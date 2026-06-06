using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Products;

namespace PosPlatform.Web.Services
{
    public class ProductVariantService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly AuditLogService _auditLogService;

        public ProductVariantService(
            AppDbContext db,
            TenantContextService tenantContext,
            AuditLogService auditLogService)
        {
            _db = db;
            _tenantContext = tenantContext;
            _auditLogService = auditLogService;
        }

        public async Task<(bool Success, string Message, string ProductName)> GetProductHeaderAsync(int productId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.", string.Empty);
            }

            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == productId &&
                    x.TenantId == tenantId.Value);

            if (product == null)
            {
                return (false, "Product not found.", string.Empty);
            }

            return (true, "Product found.", product.ProductName);
        }

        public async Task<List<ProductVariantViewModel>> GetVariantsAsync(int productId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<ProductVariantViewModel>();
            }

            return await _db.ProductVariants
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.ProductId == productId)
                .OrderBy(x => x.VariantName)
                .Select(x => new ProductVariantViewModel
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    ProductName = x.Product != null ? x.Product.ProductName : "-",
                    VariantName = x.VariantName,
                    Size = x.Size,
                    Color = x.Color,
                    SKU = x.SKU,
                    Barcode = x.Barcode,
                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    ReorderLevel = x.ReorderLevel,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ProductVariantFormModel?> GetVariantForEditAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            return await _db.ProductVariants
                .AsNoTracking()
                .Where(x =>
                    x.Id == id &&
                    x.TenantId == tenantId.Value)
                .Select(x => new ProductVariantFormModel
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    VariantName = x.VariantName,
                    Size = x.Size,
                    Color = x.Color,
                    SKU = x.SKU,
                    Barcode = x.Barcode,
                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    ReorderLevel = x.ReorderLevel,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message)> SaveVariantAsync(ProductVariantFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            if (model.ProductId <= 0)
            {
                return (false, "Product is required.");
            }

            if (string.IsNullOrWhiteSpace(model.VariantName))
            {
                return (false, "Variant name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.SKU))
            {
                return (false, "Variant SKU is required.");
            }

            if (model.SellingPrice < 0 || model.CostPrice < 0 || model.QuantityInStock < 0)
            {
                return (false, "Prices and quantity cannot be negative.");
            }

            var product = await _db.Products
                .FirstOrDefaultAsync(x =>
                    x.Id == model.ProductId &&
                    x.TenantId == tenantId.Value);

            if (product == null)
            {
                return (false, "Product not found.");
            }

            var cleanSku = model.SKU.Trim();

            var duplicateSku = await _db.ProductVariants
                .AnyAsync(x =>
                    x.TenantId == tenantId.Value &&
                    x.BranchId == branchId &&
                    x.SKU == cleanSku &&
                    x.Id != model.Id);

            if (duplicateSku)
            {
                return (false, "Another product variant already uses this SKU.");
            }

            if (model.Id == 0)
            {
                var variant = new ProductVariant
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    ProductId = model.ProductId,
                    VariantName = model.VariantName.Trim(),
                    Size = Clean(model.Size),
                    Color = Clean(model.Color),
                    SKU = cleanSku,
                    Barcode = Clean(model.Barcode),
                    CostPrice = model.CostPrice,
                    SellingPrice = model.SellingPrice,
                    QuantityInStock = model.QuantityInStock,
                    ReorderLevel = model.ReorderLevel,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.ProductVariants.Add(variant);
                await _db.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    module: "Products",
                    action: "Create Variant",
                    entityName: "ProductVariant",
                    entityId: variant.Id,
                    summary: $"Created variant {variant.VariantName} for product {product.ProductName}.",
                    oldValues: null,
                    newValues: new
                    {
                        variant.Id,
                        variant.ProductId,
                        ProductName = product.ProductName,
                        variant.VariantName,
                        variant.Size,
                        variant.Color,
                        variant.SKU,
                        variant.Barcode,
                        variant.CostPrice,
                        variant.SellingPrice,
                        variant.QuantityInStock,
                        variant.ReorderLevel,
                        variant.IsActive
                    });

                return (true, "Product variant created successfully.");
            }
            else
            {
                var variant = await _db.ProductVariants
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.Id &&
                        x.TenantId == tenantId.Value);

                if (variant == null)
                {
                    return (false, "Variant not found.");
                }

                var oldValues = new
                {
                    variant.VariantName,
                    variant.Size,
                    variant.Color,
                    variant.SKU,
                    variant.Barcode,
                    variant.CostPrice,
                    variant.SellingPrice,
                    variant.QuantityInStock,
                    variant.ReorderLevel,
                    variant.IsActive
                };

                variant.VariantName = model.VariantName.Trim();
                variant.Size = Clean(model.Size);
                variant.Color = Clean(model.Color);
                variant.SKU = cleanSku;
                variant.Barcode = Clean(model.Barcode);
                variant.CostPrice = model.CostPrice;
                variant.SellingPrice = model.SellingPrice;
                variant.QuantityInStock = model.QuantityInStock;
                variant.ReorderLevel = model.ReorderLevel;
                variant.IsActive = model.IsActive;
                variant.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    module: "Products",
                    action: "Update Variant",
                    entityName: "ProductVariant",
                    entityId: variant.Id,
                    summary: $"Updated variant {variant.VariantName} for product {product.ProductName}.",
                    oldValues: oldValues,
                    newValues: new
                    {
                        variant.VariantName,
                        variant.Size,
                        variant.Color,
                        variant.SKU,
                        variant.Barcode,
                        variant.CostPrice,
                        variant.SellingPrice,
                        variant.QuantityInStock,
                        variant.ReorderLevel,
                        variant.IsActive
                    });

                return (true, "Product variant updated successfully.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteVariantAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var variant = await _db.ProductVariants
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.TenantId == tenantId.Value);

            if (variant == null)
            {
                return (false, "Variant not found.");
            }

            if (variant.QuantityInStock > 0)
            {
                return (false, "Cannot delete a variant that still has stock. Set it inactive instead.");
            }

            var oldValues = new
            {
                variant.Id,
                variant.ProductId,
                ProductName = variant.Product?.ProductName,
                variant.VariantName,
                variant.Size,
                variant.Color,
                variant.SKU,
                variant.Barcode,
                variant.CostPrice,
                variant.SellingPrice,
                variant.QuantityInStock
            };

            _db.ProductVariants.Remove(variant);
            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Products",
                action: "Delete Variant",
                entityName: "ProductVariant",
                entityId: id,
                summary: $"Deleted variant {variant.VariantName}.",
                oldValues: oldValues,
                newValues: null);

            return (true, "Product variant deleted successfully.");
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}