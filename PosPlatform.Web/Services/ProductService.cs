using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Products;

namespace PosPlatform.Web.Services
{
    public class ProductService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public ProductService(AppDbContext db, TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<List<ProductListItemViewModel>> GetProductsAsync(string? search = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<ProductListItemViewModel>();
            }

            var query = _db.Products
                .AsNoTracking()
                .Include(x => x.ProductCategory)
                .Where(x => x.TenantId == tenantId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.ProductName.Contains(term) ||
                    x.SKU.Contains(term) ||
                    (x.Barcode != null && x.Barcode.Contains(term)) ||
                    x.ProductType.Contains(term));
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ProductListItemViewModel
                {
                    Id = x.Id,
                    ProductName = x.ProductName,
                    SKU = x.SKU,
                    Barcode = x.Barcode,

                    ProductType = x.ProductType,
                    TrackStock = x.TrackStock,
                    AgeRestricted = x.AgeRestricted,
                    UnitOfMeasure = x.UnitOfMeasure,
                    DurationMinutes = x.DurationMinutes,

                    CategoryName = x.ProductCategory != null ? x.ProductCategory.Name : "-",
                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    ReorderLevel = x.ReorderLevel,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<ProductCategoryOptionViewModel>> GetCategoriesAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<ProductCategoryOptionViewModel>();
            }

            return await _db.ProductCategories
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new ProductCategoryOptionViewModel
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();
        }

        public async Task<ProductFormModel?> GetByIdAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var entity = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return null;
            }

            return new ProductFormModel
            {
                Id = entity.Id,
                ProductName = entity.ProductName,
                SKU = entity.SKU,
                Barcode = entity.Barcode,
                Description = entity.Description,

                ProductType = entity.ProductType,
                TrackStock = entity.TrackStock,
                AgeRestricted = entity.AgeRestricted,
                UnitOfMeasure = entity.UnitOfMeasure,
                DurationMinutes = entity.DurationMinutes,

                CostPrice = entity.CostPrice,
                SellingPrice = entity.SellingPrice,
                QuantityInStock = entity.QuantityInStock,
                ReorderLevel = entity.ReorderLevel,
                ProductCategoryId = entity.ProductCategoryId,
                BranchId = entity.BranchId,
                IsActive = entity.IsActive
            };
        }

        public async Task<(bool Success, string Message)> SaveAsync(ProductFormModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var productType = NormalizeProductType(model.ProductType);
            var trackStock = model.TrackStock;

            if (productType is "Service" or "Digital Product")
            {
                trackStock = false;
            }

            if (!trackStock)
            {
                model.QuantityInStock = 0;
                model.ReorderLevel = 0;
            }

            if (productType == "Service" && model.DurationMinutes.HasValue && model.DurationMinutes.Value < 0)
            {
                return (false, "Service duration cannot be negative.");
            }

            var sku = model.SKU.Trim();

            var skuExists = await _db.Products.AnyAsync(x =>
                x.TenantId == tenantId.Value &&
                x.SKU == sku &&
                x.Id != (model.Id ?? 0));

            if (skuExists)
            {
                return (false, "SKU already exists.");
            }

            Product entity;

            if (model.Id.HasValue && model.Id.Value > 0)
            {
                entity = await _db.Products.FirstOrDefaultAsync(x =>
                    x.Id == model.Id.Value && x.TenantId == tenantId.Value)
                    ?? new Product();

                if (entity.Id == 0)
                {
                    return (false, "Product not found.");
                }
            }
            else
            {
                entity = new Product
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Products.Add(entity);
            }

            entity.ProductName = model.ProductName.Trim();
            entity.SKU = sku;
            entity.Barcode = string.IsNullOrWhiteSpace(model.Barcode) ? null : model.Barcode.Trim();
            entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

            entity.ProductType = productType;
            entity.TrackStock = trackStock;
            entity.AgeRestricted = model.AgeRestricted;
            entity.UnitOfMeasure = string.IsNullOrWhiteSpace(model.UnitOfMeasure) ? "Each" : model.UnitOfMeasure.Trim();
            entity.DurationMinutes = productType == "Service" ? model.DurationMinutes : null;

            entity.CostPrice = model.CostPrice;
            entity.SellingPrice = model.SellingPrice;
            entity.QuantityInStock = trackStock ? model.QuantityInStock : 0;
            entity.ReorderLevel = trackStock ? model.ReorderLevel : 0;
            entity.ProductCategoryId = model.ProductCategoryId;
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, model.Id.HasValue ? "Item updated successfully." : "Item added successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Products.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Product not found.");
            }

            _db.Products.Remove(entity);
            await _db.SaveChangesAsync();

            return (true, "Item deleted successfully.");
        }

        public async Task<(bool Success, string Message)> ToggleStatusAsync(int id)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var entity = await _db.Products.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TenantId == tenantId.Value);

            if (entity == null)
            {
                return (false, "Product not found.");
            }

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, entity.IsActive ? "Item activated." : "Item deactivated.");
        }

        private static string NormalizeProductType(string? productType)
        {
            if (string.IsNullOrWhiteSpace(productType))
            {
                return "Physical Product";
            }

            return productType.Trim() switch
            {
                "Physical Product" => "Physical Product",
                "Service" => "Service",
                "Package / Bundle" => "Package / Bundle",
                "Voucher" => "Voucher",
                "Digital Product" => "Digital Product",
                _ => "Physical Product"
            };
        }
    }
}