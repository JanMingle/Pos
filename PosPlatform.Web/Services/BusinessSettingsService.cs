using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Settings;

namespace PosPlatform.Web.Services
{
    public class BusinessSettingsService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public BusinessSettingsService(AppDbContext db, TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public List<BusinessTypeOptionViewModel> GetBusinessTypes()
        {
            return new List<BusinessTypeOptionViewModel>
            {
                new()
                {
                    Name = "General Business",
                    Description = "Flexible setup for any small business."
                },
                new()
                {
                    Name = "Retail Store",
                    Description = "Products, stock, barcode sales and receipts."
                },
                new()
                {
                    Name = "Salon / Beauty",
                    Description = "Services, products, appointments and customer history."
                },
                new()
                {
                    Name = "Liquor Store",
                    Description = "Stock, age-restricted products and cashier sales."
                },
                new()
                {
                    Name = "Clothing Store",
                    Description = "Products, stock, variants and category-based sales."
                },
                new()
                {
                    Name = "Service Business",
                    Description = "Services, bookings, customers and receipts."
                },
                new()
                {
                    Name = "Mini Market / Grocery",
                    Description = "Fast product sales, stock tracking and low-stock alerts."
                }
            };
        }

        public async Task<BusinessSettingsFormModel> GetSettingsAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new BusinessSettingsFormModel
                {
                    BusinessName = "My Business"
                };
            }

            var tenant = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tenantId.Value);

            var settings = await _db.BusinessSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            if (settings == null)
            {
                return new BusinessSettingsFormModel
                {
                    BusinessName = tenant?.Name ?? "My Business",
                    BusinessType = string.IsNullOrWhiteSpace(tenant?.BusinessType)
                        ? "General Business"
                        : tenant.BusinessType!,
                    Email = tenant?.Email,
                    Phone = tenant?.Phone,
                    CurrencyCode = "ZAR",
                    CurrencySymbol = "R",
                    TaxName = "VAT",
                    ReceiptTitle = "Sales Receipt",
                    ReceiptFooterMessage = "Thank you for your purchase.",
                    ProductsEnabled = true,
                    StockTrackingEnabled = true,
                    CustomersEnabled = true,
                    AllowDiscounts = true
                };
            }

            return new BusinessSettingsFormModel
            {
                Id = settings.Id,
                BusinessName = settings.BusinessName,
                BusinessType = settings.BusinessType,
                Address = settings.Address,
                Phone = settings.Phone,
                Email = settings.Email,
                CurrencyCode = settings.CurrencyCode,
                CurrencySymbol = settings.CurrencySymbol,
                TaxEnabled = settings.TaxEnabled,
                TaxName = settings.TaxName,
                TaxRate = settings.TaxRate,
                ProductsEnabled = settings.ProductsEnabled,
                StockTrackingEnabled = settings.StockTrackingEnabled,
                ServicesEnabled = settings.ServicesEnabled,
                AppointmentsEnabled = settings.AppointmentsEnabled,
                CustomersEnabled = settings.CustomersEnabled,
                AgeRestrictedProductsEnabled = settings.AgeRestrictedProductsEnabled,
                AllowNegativeStock = settings.AllowNegativeStock,
                RequireCustomerForSale = settings.RequireCustomerForSale,
                AllowDiscounts = settings.AllowDiscounts,
                ReceiptTitle = settings.ReceiptTitle,
                ReceiptFooterMessage = settings.ReceiptFooterMessage,
                ReturnPolicyText = settings.ReturnPolicyText
            };
        }

        public async Task<(bool Success, string Message)> SaveSettingsAsync(BusinessSettingsFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            if (model.TaxEnabled && model.TaxRate <= 0)
            {
                return (false, "Tax rate must be greater than zero when tax is enabled.");
            }

            var settings = await _db.BusinessSettings
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            if (settings == null)
            {
                settings = new BusinessSettings
                {
                    TenantId = tenantId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                _db.BusinessSettings.Add(settings);
            }

            settings.BusinessName = model.BusinessName.Trim();
            settings.BusinessType = model.BusinessType.Trim();
            settings.Address = Clean(model.Address);
            settings.Phone = Clean(model.Phone);
            settings.Email = Clean(model.Email);
            settings.CurrencyCode = model.CurrencyCode.Trim().ToUpperInvariant();
            settings.CurrencySymbol = model.CurrencySymbol.Trim();

            settings.TaxEnabled = model.TaxEnabled;
            settings.TaxName = string.IsNullOrWhiteSpace(model.TaxName) ? "VAT" : model.TaxName.Trim();
            settings.TaxRate = model.TaxEnabled ? model.TaxRate : 0;

            settings.ProductsEnabled = model.ProductsEnabled;
            settings.StockTrackingEnabled = model.StockTrackingEnabled;
            settings.ServicesEnabled = model.ServicesEnabled;
            settings.AppointmentsEnabled = model.AppointmentsEnabled;
            settings.CustomersEnabled = model.CustomersEnabled;
            settings.AgeRestrictedProductsEnabled = model.AgeRestrictedProductsEnabled;

            settings.AllowNegativeStock = model.AllowNegativeStock;
            settings.RequireCustomerForSale = model.RequireCustomerForSale;
            settings.AllowDiscounts = model.AllowDiscounts;

            settings.ReceiptTitle = string.IsNullOrWhiteSpace(model.ReceiptTitle)
                ? "Sales Receipt"
                : model.ReceiptTitle.Trim();

            settings.ReceiptFooterMessage = Clean(model.ReceiptFooterMessage);
            settings.ReturnPolicyText = Clean(model.ReturnPolicyText);
            settings.UpdatedAt = DateTime.UtcNow;

            var tenant = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId.Value);

            if (tenant != null)
            {
                tenant.Name = settings.BusinessName;
                tenant.BusinessType = settings.BusinessType;
                tenant.Email = settings.Email;
                tenant.Phone = settings.Phone;
            }

            await _db.SaveChangesAsync();

            return (true, "Business settings saved successfully.");
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}