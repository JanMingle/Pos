using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Expenses;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class ExpenseService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogService _auditLogService;

        public ExpenseService(
            AppDbContext db,
            TenantContextService tenantContext,
            IHttpContextAccessor httpContextAccessor,
            AuditLogService auditLogService)
        {
            _db = db;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
            _auditLogService = auditLogService;
        }

        public async Task<List<ExpenseCategoryListItemViewModel>> GetCategoriesAsync(string? search, string statusFilter, int? branchId = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<ExpenseCategoryListItemViewModel>();
            }

            var query = _db.ExpenseCategories
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.CategoryName.Contains(term) ||
                    (x.Description != null && x.Description.Contains(term)));
            }

            if (statusFilter == "active")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (statusFilter == "inactive")
            {
                query = query.Where(x => !x.IsActive);
            }

            var categories = await query
                .OrderBy(x => x.CategoryName)
                .Select(x => new ExpenseCategoryListItemViewModel
                {
                    Id = x.Id,
                    CategoryName = x.CategoryName,
                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            var categoryIds = categories.Select(x => x.Id).ToList();

            if (categoryIds.Count > 0)
            {
                var stats = await _db.Expenses
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId.Value &&
                        categoryIds.Contains(x.ExpenseCategoryId) &&
                        x.Status == "Recorded" &&
                        (!branchId.HasValue || x.BranchId == branchId.Value))
                    .GroupBy(x => x.ExpenseCategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        ExpenseCount = g.Count(),
                        TotalSpent = g.Sum(x => x.TotalAmount)
                    })
                    .ToListAsync();

                foreach (var category in categories)
                {
                    var stat = stats.FirstOrDefault(x => x.CategoryId == category.Id);

                    if (stat != null)
                    {
                        category.ExpenseCount = stat.ExpenseCount;
                        category.TotalSpent = stat.TotalSpent;
                    }
                }
            }

            return categories;
        }

        public async Task<List<ExpenseCategoryOptionViewModel>> GetCategoryOptionsAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<ExpenseCategoryOptionViewModel>();
            }

            return await _db.ExpenseCategories
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.IsActive)
                .OrderBy(x => x.CategoryName)
                .Select(x => new ExpenseCategoryOptionViewModel
                {
                    Id = x.Id,
                    CategoryName = x.CategoryName
                })
                .ToListAsync();
        }

        public async Task<ExpenseCategoryFormModel?> GetCategoryByIdAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var entity = await _db.ExpenseCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return null;
            }

            return new ExpenseCategoryFormModel
            {
                Id = entity.Id,
                CategoryName = entity.CategoryName,
                Description = entity.Description,
                IsActive = entity.IsActive
            };
        }

        public async Task<(bool Success, string Message)> SaveCategoryAsync(ExpenseCategoryFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                return (false, "Category name is required.");
            }

            ExpenseCategory entity;
            object? oldValues = null;
            var isNew = !model.Id.HasValue || model.Id.Value <= 0;

            if (!isNew)
            {
                entity = await _db.ExpenseCategories.FirstOrDefaultAsync(x =>
                    x.Id == model.Id!.Value &&
                    x.TenantId == tenantId.Value)
                    ?? new ExpenseCategory();

                if (entity.Id == 0)
                {
                    return (false, "Expense category not found.");
                }

                oldValues = new
                {
                    entity.CategoryName,
                    entity.Description,
                    entity.IsActive,
                    entity.BranchId
                };
            }
            else
            {
                entity = new ExpenseCategory
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.ExpenseCategories.Add(entity);
            }

            entity.CategoryName = model.CategoryName.Trim();
            entity.Description = Clean(model.Description);
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Expenses",
                action: isNew ? "Create" : "Update",
                entityName: "ExpenseCategory",
                entityId: entity.Id,
                summary: isNew
                    ? $"Created expense category {entity.CategoryName}."
                    : $"Updated expense category {entity.CategoryName}.",
                oldValues: oldValues,
                newValues: new
                {
                    entity.CategoryName,
                    entity.Description,
                    entity.IsActive,
                    entity.BranchId
                });

            return (true, isNew ? "Expense category added successfully." : "Expense category updated successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleCategoryStatusAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.ExpenseCategories.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Expense category not found.");
            }

            var oldValues = new
            {
                entity.CategoryName,
                entity.IsActive
            };

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var action = entity.IsActive ? "Activate" : "Deactivate";

            await _auditLogService.LogAsync(
                module: "Expenses",
                action: action,
                entityName: "ExpenseCategory",
                entityId: entity.Id,
                summary: $"{action}d expense category {entity.CategoryName}.",
                oldValues: oldValues,
                newValues: new
                {
                    entity.CategoryName,
                    entity.IsActive
                });

            return (true, entity.IsActive ? "Expense category activated." : "Expense category deactivated.");
        }

        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.ExpenseCategories.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Expense category not found.");
            }

            var oldValues = new
            {
                entity.CategoryName,
                entity.Description,
                entity.IsActive,
                entity.BranchId
            };

            var hasExpenses = await _db.Expenses.AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.ExpenseCategoryId == entity.Id);

            if (hasExpenses)
            {
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    module: "Expenses",
                    action: "Deactivate",
                    entityName: "ExpenseCategory",
                    entityId: entity.Id,
                    summary: $"Deactivated expense category {entity.CategoryName} because it has expense history.",
                    oldValues: oldValues,
                    newValues: new
                    {
                        entity.CategoryName,
                        entity.IsActive
                    });

                return (true, "Category has expense history, so it was deactivated instead of deleted.");
            }

            var categoryName = entity.CategoryName;
            var categoryId = entity.Id;

            _db.ExpenseCategories.Remove(entity);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Expenses",
                action: "Delete",
                entityName: "ExpenseCategory",
                entityId: categoryId,
                summary: $"Deleted expense category {categoryName}.",
                oldValues: oldValues,
                newValues: null);

            return (true, "Expense category deleted successfully.");
        }

        public async Task<List<ExpenseListItemViewModel>> GetExpensesAsync(DateTime? fromDate, DateTime? toDate, string? search, int? branchId = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<ExpenseListItemViewModel>();
            }

            var query = _db.Expenses
                .AsNoTracking()
                .Include(x => x.ExpenseCategory)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    (!branchId.HasValue || x.BranchId == branchId.Value));

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.ExpenseDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.ExpenseDate < to);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.ExpenseNumber.Contains(term) ||
                    (x.VendorName != null && x.VendorName.Contains(term)) ||
                    (x.ReferenceNumber != null && x.ReferenceNumber.Contains(term)) ||
                    (x.ExpenseCategory != null && x.ExpenseCategory.CategoryName.Contains(term)));
            }

            return await query
                .OrderByDescending(x => x.ExpenseDate)
                .ThenByDescending(x => x.Id)
                .Take(150)
                .Select(x => new ExpenseListItemViewModel
                {
                    Id = x.Id,
                    ExpenseNumber = x.ExpenseNumber,
                    ExpenseDate = x.ExpenseDate,
                    CategoryName = x.ExpenseCategory != null ? x.ExpenseCategory.CategoryName : "-",
                    VendorName = x.VendorName,
                    ReferenceNumber = x.ReferenceNumber,
                    PaymentMethod = x.PaymentMethod,
                    Subtotal = x.Subtotal,
                    TaxAmount = x.TaxAmount,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status,
                    CreatedByName = x.CreatedByName,
                    Notes = x.Notes
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> SaveExpenseAsync(ExpenseFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserDisplayName();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            if (userId == null)
            {
                return (false, "Logged-in user could not be identified.");
            }

            if (model.ExpenseCategoryId <= 0)
            {
                return (false, "Select expense category.");
            }

            if (model.Subtotal <= 0)
            {
                return (false, "Subtotal must be greater than zero.");
            }

            if (model.TaxAmount < 0)
            {
                return (false, "Tax amount cannot be negative.");
            }

            var category = await _db.ExpenseCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == model.ExpenseCategoryId &&
                    x.TenantId == tenantId.Value &&
                    x.IsActive);

            if (category == null)
            {
                return (false, "Expense category not found or inactive.");
            }

            var total = model.Subtotal + model.TaxAmount;

            Expense entity;
            object? oldValues = null;
            var isNew = !model.Id.HasValue || model.Id.Value <= 0;

            if (!isNew)
            {
                entity = await _db.Expenses.FirstOrDefaultAsync(x =>
                    x.Id == model.Id!.Value &&
                    x.TenantId == tenantId.Value)
                    ?? new Expense();

                if (entity.Id == 0)
                {
                    return (false, "Expense not found.");
                }

                oldValues = new
                {
                    entity.ExpenseNumber,
                    entity.ExpenseCategoryId,
                    entity.ExpenseDate,
                    entity.VendorName,
                    entity.ReferenceNumber,
                    entity.PaymentMethod,
                    entity.Subtotal,
                    entity.TaxAmount,
                    entity.TotalAmount,
                    entity.Status,
                    entity.Notes,
                    entity.BranchId
                };
            }
            else
            {
                entity = new Expense
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    ExpenseNumber = $"EXP-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    CreatedByUserId = userId.Value,
                    CreatedByName = userName,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Expenses.Add(entity);
            }

            entity.ExpenseCategoryId = model.ExpenseCategoryId;
            entity.ExpenseDate = model.ExpenseDate.Date;
            entity.VendorName = Clean(model.VendorName);
            entity.ReferenceNumber = Clean(model.ReferenceNumber);
            entity.PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? "Cash" : model.PaymentMethod.Trim();
            entity.Subtotal = model.Subtotal;
            entity.TaxAmount = model.TaxAmount;
            entity.TotalAmount = total;
            entity.Status = "Recorded";
            entity.Notes = Clean(model.Notes);
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Expenses",
                action: isNew ? "Create" : "Update",
                entityName: "Expense",
                entityId: entity.Id,
                summary: isNew
                    ? $"Recorded expense {entity.ExpenseNumber} for {entity.TotalAmount:0.00}."
                    : $"Updated expense {entity.ExpenseNumber} for {entity.TotalAmount:0.00}.",
                oldValues: oldValues,
                newValues: new
                {
                    entity.ExpenseNumber,
                    CategoryName = category.CategoryName,
                    entity.ExpenseCategoryId,
                    entity.ExpenseDate,
                    entity.VendorName,
                    entity.ReferenceNumber,
                    entity.PaymentMethod,
                    entity.Subtotal,
                    entity.TaxAmount,
                    entity.TotalAmount,
                    entity.Status,
                    entity.Notes,
                    entity.BranchId
                });

            return (true, isNew ? "Expense recorded successfully." : "Expense updated successfully.");
        }

        public async Task<ExpenseFormModel?> GetExpenseByIdAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var entity = await _db.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return null;
            }

            return new ExpenseFormModel
            {
                Id = entity.Id,
                ExpenseCategoryId = entity.ExpenseCategoryId,
                ExpenseDate = entity.ExpenseDate,
                VendorName = entity.VendorName,
                ReferenceNumber = entity.ReferenceNumber,
                PaymentMethod = entity.PaymentMethod,
                Subtotal = entity.Subtotal,
                TaxAmount = entity.TaxAmount,
                Notes = entity.Notes
            };
        }

        public async Task<(bool Success, string Message)> DeleteExpenseAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Expenses
                .Include(x => x.ExpenseCategory)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Expense not found.");
            }

            var oldValues = new
            {
                entity.ExpenseNumber,
                CategoryName = entity.ExpenseCategory?.CategoryName,
                entity.ExpenseCategoryId,
                entity.ExpenseDate,
                entity.VendorName,
                entity.ReferenceNumber,
                entity.PaymentMethod,
                entity.Subtotal,
                entity.TaxAmount,
                entity.TotalAmount,
                entity.Status,
                entity.Notes,
                entity.BranchId
            };

            var expenseNumber = entity.ExpenseNumber;
            var expenseId = entity.Id;
            var amount = entity.TotalAmount;

            _db.Expenses.Remove(entity);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Expenses",
                action: "Delete",
                entityName: "Expense",
                entityId: expenseId,
                summary: $"Deleted expense {expenseNumber} for {amount:0.00}.",
                oldValues: oldValues,
                newValues: null);

            return (true, "Expense deleted successfully.");
        }

        private int? GetCurrentUserId()
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        private string GetCurrentUserDisplayName()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            return user?.FindFirstValue(ClaimTypes.Name)
                ?? user?.Identity?.Name
                ?? user?.FindFirstValue(ClaimTypes.Email)
                ?? "User";
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}