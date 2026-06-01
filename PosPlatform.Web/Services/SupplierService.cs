using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Suppliers;

namespace PosPlatform.Web.Services
{
    public class SupplierService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public SupplierService(AppDbContext db, TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<List<SupplierListItemViewModel>> GetSuppliersAsync(string? search, string statusFilter)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<SupplierListItemViewModel>();
            }

            var query = _db.Suppliers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.SupplierName.Contains(term) ||
                    (x.ContactPerson != null && x.ContactPerson.Contains(term)) ||
                    (x.Phone != null && x.Phone.Contains(term)) ||
                    (x.Email != null && x.Email.Contains(term)));
            }

            if (statusFilter == "active")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (statusFilter == "inactive")
            {
                query = query.Where(x => !x.IsActive);
            }

            var suppliers = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SupplierListItemViewModel
                {
                    Id = x.Id,
                    SupplierName = x.SupplierName,
                    ContactPerson = x.ContactPerson,
                    Phone = x.Phone,
                    Email = x.Email,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            var supplierIds = suppliers.Select(x => x.Id).ToList();

            if (supplierIds.Count > 0)
            {
                var stats = await _db.StockPurchases
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId.Value &&
                        supplierIds.Contains(x.SupplierId))
                    .GroupBy(x => x.SupplierId)
                    .Select(g => new
                    {
                        SupplierId = g.Key,
                        TotalPurchases = g.Count(),
                        TotalSpent = g.Sum(x => x.TotalAmount),
                        LastPurchaseDate = g.Max(x => x.PurchaseDate)
                    })
                    .ToListAsync();

                foreach (var supplier in suppliers)
                {
                    var stat = stats.FirstOrDefault(x => x.SupplierId == supplier.Id);

                    if (stat != null)
                    {
                        supplier.TotalPurchases = stat.TotalPurchases;
                        supplier.TotalSpent = stat.TotalSpent;
                        supplier.LastPurchaseDate = stat.LastPurchaseDate;
                    }
                }
            }

            return suppliers;
        }

        public async Task<List<SupplierOptionViewModel>> GetSupplierOptionsAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<SupplierOptionViewModel>();
            }

            return await _db.Suppliers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.IsActive)
                .OrderBy(x => x.SupplierName)
                .Select(x => new SupplierOptionViewModel
                {
                    Id = x.Id,
                    SupplierName = x.SupplierName
                })
                .ToListAsync();
        }

        public async Task<SupplierFormModel?> GetByIdAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var entity = await _db.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return null;
            }

            return new SupplierFormModel
            {
                Id = entity.Id,
                SupplierName = entity.SupplierName,
                ContactPerson = entity.ContactPerson,
                Phone = entity.Phone,
                Email = entity.Email,
                Address = entity.Address,
                TaxNumber = entity.TaxNumber,
                Notes = entity.Notes,
                IsActive = entity.IsActive
            };
        }

        public async Task<(bool Success, string Message)> SaveAsync(SupplierFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            if (string.IsNullOrWhiteSpace(model.SupplierName))
            {
                return (false, "Supplier name is required.");
            }

            Supplier entity;

            if (model.Id.HasValue && model.Id.Value > 0)
            {
                entity = await _db.Suppliers.FirstOrDefaultAsync(x =>
                    x.Id == model.Id.Value &&
                    x.TenantId == tenantId.Value)
                    ?? new Supplier();

                if (entity.Id == 0)
                {
                    return (false, "Supplier not found.");
                }
            }
            else
            {
                entity = new Supplier
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Suppliers.Add(entity);
            }

            entity.SupplierName = model.SupplierName.Trim();
            entity.ContactPerson = Clean(model.ContactPerson);
            entity.Phone = Clean(model.Phone);
            entity.Email = Clean(model.Email);
            entity.Address = Clean(model.Address);
            entity.TaxNumber = Clean(model.TaxNumber);
            entity.Notes = Clean(model.Notes);
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, model.Id.HasValue ? "Supplier updated successfully." : "Supplier added successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleStatusAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Suppliers.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Supplier not found.");
            }

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, entity.IsActive ? "Supplier activated." : "Supplier deactivated.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Suppliers.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Supplier not found.");
            }

            var hasPurchases = await _db.StockPurchases.AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.SupplierId == entity.Id);

            if (hasPurchases)
            {
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return (true, "Supplier has purchase history, so they were deactivated instead of deleted.");
            }

            _db.Suppliers.Remove(entity);
            await _db.SaveChangesAsync();

            return (true, "Supplier deleted successfully.");
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}