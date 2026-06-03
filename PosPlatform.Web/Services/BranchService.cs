using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Branches;

namespace PosPlatform.Web.Services
{
    public class BranchService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public BranchService(AppDbContext db, TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<List<BranchListItemViewModel>> GetBranchesAsync(string? search, string statusFilter)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<BranchListItemViewModel>();
            }

            var query = _db.Branches
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.Name.Contains(term) ||
                    (x.BranchCode != null && x.BranchCode.Contains(term)) ||
                    (x.Phone != null && x.Phone.Contains(term)) ||
                    (x.Email != null && x.Email.Contains(term)) ||
                    (x.Address != null && x.Address.Contains(term)));
            }

            if (statusFilter == "active")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (statusFilter == "inactive")
            {
                query = query.Where(x => !x.IsActive);
            }
            else if (statusFilter == "main")
            {
                query = query.Where(x => x.IsMainBranch);
            }

            var branches = await query
                .OrderByDescending(x => x.IsMainBranch)
                .ThenBy(x => x.Name)
                .Select(x => new BranchListItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    BranchCode = x.BranchCode,
                    Phone = x.Phone,
                    Email = x.Email,
                    Address = x.Address,
                    IsMainBranch = x.IsMainBranch,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            var branchIds = branches.Select(x => x.Id).ToList();

            if (branchIds.Count > 0)
            {
                var saleStats = await _db.Sales
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId.Value &&
                        x.BranchId.HasValue &&
                        branchIds.Contains(x.BranchId.Value) &&
                        x.Status != "Voided")
                    .GroupBy(x => x.BranchId!.Value)
                    .Select(g => new
                    {
                        BranchId = g.Key,
                        SalesCount = g.Count(),
                        SalesTotal = g.Sum(x => x.TotalAmount)
                    })
                    .ToListAsync();

                var purchaseStats = await _db.StockPurchases
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId.Value &&
                        x.BranchId.HasValue &&
                        branchIds.Contains(x.BranchId.Value))
                    .GroupBy(x => x.BranchId!.Value)
                    .Select(g => new
                    {
                        BranchId = g.Key,
                        PurchaseCount = g.Count(),
                        PurchaseTotal = g.Sum(x => x.TotalAmount)
                    })
                    .ToListAsync();

                var expenseStats = await _db.Expenses
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId.Value &&
                        x.BranchId.HasValue &&
                        branchIds.Contains(x.BranchId.Value) &&
                        x.Status == "Recorded")
                    .GroupBy(x => x.BranchId!.Value)
                    .Select(g => new
                    {
                        BranchId = g.Key,
                        ExpenseCount = g.Count(),
                        ExpenseTotal = g.Sum(x => x.TotalAmount)
                    })
                    .ToListAsync();

                foreach (var branch in branches)
                {
                    var sales = saleStats.FirstOrDefault(x => x.BranchId == branch.Id);
                    var purchases = purchaseStats.FirstOrDefault(x => x.BranchId == branch.Id);
                    var expenses = expenseStats.FirstOrDefault(x => x.BranchId == branch.Id);

                    if (sales != null)
                    {
                        branch.SalesCount = sales.SalesCount;
                        branch.SalesTotal = sales.SalesTotal;
                    }

                    if (purchases != null)
                    {
                        branch.PurchaseCount = purchases.PurchaseCount;
                        branch.PurchaseTotal = purchases.PurchaseTotal;
                    }

                    if (expenses != null)
                    {
                        branch.ExpenseCount = expenses.ExpenseCount;
                        branch.ExpenseTotal = expenses.ExpenseTotal;
                    }
                }
            }

            return branches;
        }

        public async Task<List<BranchOptionViewModel>> GetBranchOptionsAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<BranchOptionViewModel>();
            }

            return await _db.Branches
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.IsActive)
                .OrderByDescending(x => x.IsMainBranch)
                .ThenBy(x => x.Name)
            .Select(x => new BranchOptionViewModel
            {
                Id = x.Id,
                Name = x.Name,
                IsMainBranch = x.IsMainBranch
            })
                .ToListAsync();
        }

        public async Task<BranchFormModel?> GetByIdAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var entity = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return null;
            }

            return new BranchFormModel
            {
                Id = entity.Id,
                Name = entity.Name,
                BranchCode = entity.BranchCode,
                Phone = entity.Phone,
                Email = entity.Email,
                Address = entity.Address,
                Notes = entity.Notes,
                IsMainBranch = entity.IsMainBranch,
                IsActive = entity.IsActive
            };
        }

        public async Task<(bool Success, string Message)> SaveAsync(BranchFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return (false, "Branch name is required.");
            }

            Branch entity;

            if (model.Id.HasValue && model.Id.Value > 0)
            {
                entity = await _db.Branches.FirstOrDefaultAsync(x =>
                    x.Id == model.Id.Value &&
                    x.TenantId == tenantId.Value)
                    ?? new Branch();

                if (entity.Id == 0)
                {
                    return (false, "Branch not found.");
                }
            }
            else
            {
                entity = new Branch
                {
                    TenantId = tenantId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Branches.Add(entity);
            }

            if (model.IsMainBranch)
            {
                var otherMainBranches = await _db.Branches
                    .Where(x =>
                        x.TenantId == tenantId.Value &&
                        x.Id != entity.Id &&
                        x.IsMainBranch)
                    .ToListAsync();

                foreach (var branch in otherMainBranches)
                {
                    branch.IsMainBranch = false;
                    branch.UpdatedAt = DateTime.UtcNow;
                }
            }

            entity.Name = model.Name.Trim();
            entity.BranchCode = Clean(model.BranchCode);
            entity.Phone = Clean(model.Phone);
            entity.Email = Clean(model.Email);
            entity.Address = Clean(model.Address);
            entity.Notes = Clean(model.Notes);
            entity.IsMainBranch = model.IsMainBranch;
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, model.Id.HasValue ? "Branch updated successfully." : "Branch added successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleStatusAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Branches.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Branch not found.");
            }

            if (entity.IsMainBranch && entity.IsActive)
            {
                return (false, "Main branch cannot be deactivated. Set another branch as main first.");
            }

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, entity.IsActive ? "Branch activated." : "Branch deactivated.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Branches.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Branch not found.");
            }

            if (entity.IsMainBranch)
            {
                return (false, "Main branch cannot be deleted.");
            }

            var hasSales = await _db.Sales.AnyAsync(x => x.TenantId == tenantId.Value && x.BranchId == id);
            var hasPurchases = await _db.StockPurchases.AnyAsync(x => x.TenantId == tenantId.Value && x.BranchId == id);
            var hasExpenses = await _db.Expenses.AnyAsync(x => x.TenantId == tenantId.Value && x.BranchId == id);
            var hasUsers = await _db.Users.AnyAsync(x => x.TenantId == tenantId.Value && x.BranchId == id);

            if (hasSales || hasPurchases || hasExpenses || hasUsers)
            {
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return (true, "Branch has linked records, so it was deactivated instead of deleted.");
            }

            _db.Branches.Remove(entity);
            await _db.SaveChangesAsync();

            return (true, "Branch deleted successfully.");
        }

        public async Task EnsureMainBranchExistsAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return;
            }

            var hasBranch = await _db.Branches.AnyAsync(x => x.TenantId == tenantId.Value);

            if (hasBranch)
            {
                return;
            }

            _db.Branches.Add(new Branch
            {
                TenantId = tenantId.Value,
                Name = "Main Branch",
                BranchCode = "MAIN",
                IsMainBranch = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}