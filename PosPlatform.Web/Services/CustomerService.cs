using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Customers;

namespace PosPlatform.Web.Services
{
    public class CustomerService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public CustomerService(AppDbContext db, TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<List<CustomerListItemViewModel>> GetCustomersAsync(string? search, string statusFilter)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<CustomerListItemViewModel>();
            }

            var query = _db.Customers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    (x.FirstName != null && x.FirstName.Contains(term)) ||
                    (x.LastName != null && x.LastName.Contains(term)) ||
                    (x.BusinessName != null && x.BusinessName.Contains(term)) ||
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
            else if (statusFilter == "individual")
            {
                query = query.Where(x => x.CustomerType == "Individual");
            }
            else if (statusFilter == "business")
            {
                query = query.Where(x => x.CustomerType == "Business");
            }

            var customers = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new CustomerListItemViewModel
                {
                    Id = x.Id,
                    CustomerType = x.CustomerType,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    BusinessName = x.BusinessName,
                    Phone = x.Phone,
                    Email = x.Email,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    DisplayName = x.CustomerType == "Business"
                        ? (x.BusinessName ?? "-")
                        : ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim()
                })
                .ToListAsync();

            var customerIds = customers.Select(x => x.Id).ToList();

            if (customerIds.Count > 0)
            {
                var stats = await _db.Sales
                    .AsNoTracking()
                    .Where(x =>
                        x.TenantId == tenantId.Value &&
                        x.CustomerId.HasValue &&
                        customerIds.Contains(x.CustomerId.Value) &&
                        x.Status == "Completed")
                    .GroupBy(x => x.CustomerId!.Value)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        TotalPurchases = g.Count(),
                        TotalSpent = g.Sum(x => x.TotalAmount),
                        LastPurchaseDate = g.Max(x => x.CreatedAt)
                    })
                    .ToListAsync();

                foreach (var customer in customers)
                {
                    var stat = stats.FirstOrDefault(x => x.CustomerId == customer.Id);

                    if (stat != null)
                    {
                        customer.TotalPurchases = stat.TotalPurchases;
                        customer.TotalSpent = stat.TotalSpent;
                        customer.LastPurchaseDate = stat.LastPurchaseDate;
                    }

                    if (string.IsNullOrWhiteSpace(customer.DisplayName))
                    {
                        customer.DisplayName = "-";
                    }
                }
            }

            return customers;
        }

        public async Task<List<CustomerOptionViewModel>> GetCustomerOptionsAsync(string? search = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<CustomerOptionViewModel>();
            }

            var query = _db.Customers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    (x.FirstName != null && x.FirstName.Contains(term)) ||
                    (x.LastName != null && x.LastName.Contains(term)) ||
                    (x.BusinessName != null && x.BusinessName.Contains(term)) ||
                    (x.Phone != null && x.Phone.Contains(term)) ||
                    (x.Email != null && x.Email.Contains(term)));
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .Select(x => new CustomerOptionViewModel
                {
                    Id = x.Id,
                    DisplayName = x.CustomerType == "Business"
                        ? (x.BusinessName ?? "-")
                        : ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim(),
                    Phone = x.Phone,
                    Email = x.Email
                })
                .ToListAsync();
        }

        public async Task<CustomerFormModel?> GetByIdAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var entity = await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return null;
            }

            return new CustomerFormModel
            {
                Id = entity.Id,
                CustomerType = entity.CustomerType,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                BusinessName = entity.BusinessName,
                Phone = entity.Phone,
                Email = entity.Email,
                Notes = entity.Notes,
                IsActive = entity.IsActive
            };
        }

        public async Task<(bool Success, string Message)> SaveAsync(CustomerFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var customerType = NormalizeCustomerType(model.CustomerType);

            if (customerType == "Business" && string.IsNullOrWhiteSpace(model.BusinessName))
            {
                return (false, "Business name is required for business customers.");
            }

            if (customerType == "Individual" && string.IsNullOrWhiteSpace(model.FirstName))
            {
                return (false, "First name is required for individual customers.");
            }

            Customer entity;

            if (model.Id.HasValue && model.Id.Value > 0)
            {
                entity = await _db.Customers.FirstOrDefaultAsync(x =>
                    x.Id == model.Id.Value &&
                    x.TenantId == tenantId.Value)
                    ?? new Customer();

                if (entity.Id == 0)
                {
                    return (false, "Customer not found.");
                }
            }
            else
            {
                entity = new Customer
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Customers.Add(entity);
            }

            entity.CustomerType = customerType;
            entity.FirstName = Clean(model.FirstName);
            entity.LastName = Clean(model.LastName);
            entity.BusinessName = Clean(model.BusinessName);
            entity.Phone = Clean(model.Phone);
            entity.Email = Clean(model.Email);
            entity.Notes = Clean(model.Notes);
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, model.Id.HasValue ? "Customer updated successfully." : "Customer added successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleStatusAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Customers.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Customer not found.");
            }

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, entity.IsActive ? "Customer activated." : "Customer deactivated.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Customers.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Customer not found.");
            }

            var hasSales = await _db.Sales.AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.CustomerId == entity.Id);

            if (hasSales)
            {
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return (true, "Customer has sales history, so they were deactivated instead of deleted.");
            }

            _db.Customers.Remove(entity);
            await _db.SaveChangesAsync();

            return (true, "Customer deleted successfully.");
        }

        private static string NormalizeCustomerType(string? value)
        {
            return value == "Business" ? "Business" : "Individual";
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}