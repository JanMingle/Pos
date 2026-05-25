using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Stock;

namespace PosPlatform.Web.Services
{
    public class StockService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public StockService(AppDbContext db, TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<bool> IsStockTrackingEnabledAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return false;
            }

            var settings = await _db.BusinessSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            return settings?.StockTrackingEnabled ?? true;
        }

        public async Task<List<StockProductRowViewModel>> GetStockProductsAsync(string? search, string statusFilter)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<StockProductRowViewModel>();
            }

            var stockEnabled = await IsStockTrackingEnabledAsync();

            if (!stockEnabled)
            {
                return new List<StockProductRowViewModel>();
            }

            var query = _db.Products
                .AsNoTracking()
                .Include(x => x.ProductCategory)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.TrackStock);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.ProductName.Contains(term) ||
                    x.SKU.Contains(term) ||
                    x.ProductType.Contains(term) ||
                    (x.Barcode != null && x.Barcode.Contains(term)));
            }

            if (statusFilter == "low")
            {
                query = query.Where(x => x.QuantityInStock > 0 && x.QuantityInStock <= x.ReorderLevel);
            }
            else if (statusFilter == "out")
            {
                query = query.Where(x => x.QuantityInStock <= 0);
            }
            else if (statusFilter == "healthy")
            {
                query = query.Where(x => x.QuantityInStock > x.ReorderLevel);
            }
            else if (statusFilter == "active")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (statusFilter == "inactive")
            {
                query = query.Where(x => !x.IsActive);
            }

            return await query
                .OrderBy(x => x.QuantityInStock <= x.ReorderLevel ? 0 : 1)
                .ThenBy(x => x.ProductName)
                .Select(x => new StockProductRowViewModel
                {
                    ProductId = x.Id,
                    ProductName = x.ProductName,
                    SKU = x.SKU,
                    Barcode = x.Barcode,
                    CategoryName = x.ProductCategory != null ? x.ProductCategory.Name : "-",

                    ProductType = x.ProductType,
                    TrackStock = x.TrackStock,
                    AgeRestricted = x.AgeRestricted,
                    UnitOfMeasure = x.UnitOfMeasure,

                    QuantityInStock = x.QuantityInStock,
                    ReorderLevel = x.ReorderLevel,
                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<StockSummaryViewModel> GetStockSummaryAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new StockSummaryViewModel();
            }

            var stockEnabled = await IsStockTrackingEnabledAsync();

            if (!stockEnabled)
            {
                return new StockSummaryViewModel();
            }

            var products = await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.TrackStock)
                .Select(x => new
                {
                    x.QuantityInStock,
                    x.ReorderLevel,
                    x.CostPrice
                })
                .ToListAsync();

            return new StockSummaryViewModel
            {
                TotalStockItems = products.Count,
                LowStockItems = products.Count(x => x.QuantityInStock > 0 && x.QuantityInStock <= x.ReorderLevel),
                OutOfStockItems = products.Count(x => x.QuantityInStock <= 0),
                TotalUnits = products.Sum(x => x.QuantityInStock),
                TotalStockValue = products.Sum(x => x.QuantityInStock * x.CostPrice)
            };
        }

        public async Task<List<StockMovementRowViewModel>> GetMovementsAsync(DateTime? fromDate, DateTime? toDate, string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<StockMovementRowViewModel>();
            }

            var query = _db.StockMovements
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.TenantId == tenantId.Value);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.CreatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < to);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.Product != null &&
                    (
                        x.Product.ProductName.Contains(term) ||
                        x.Product.SKU.Contains(term) ||
                        x.Product.ProductType.Contains(term) ||
                        (x.Product.Barcode != null && x.Product.Barcode.Contains(term))
                    ));
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(200)
                .Select(x => new StockMovementRowViewModel
                {
                    Id = x.Id,
                    CreatedAt = x.CreatedAt,
                    ProductName = x.Product != null ? x.Product.ProductName : "-",
                    SKU = x.Product != null ? x.Product.SKU : "-",
                    ProductType = x.Product != null ? x.Product.ProductType : "-",
                    UnitOfMeasure = x.Product != null ? x.Product.UnitOfMeasure : null,
                    MovementType = x.MovementType,
                    Quantity = x.Quantity,
                    QuantityBefore = x.QuantityBefore,
                    QuantityAfter = x.QuantityAfter,
                    ReferenceType = x.ReferenceType,
                    ReferenceId = x.ReferenceId,
                    Notes = x.Notes
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> AdjustStockAsync(StockAdjustmentModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var stockEnabled = await IsStockTrackingEnabledAsync();

            if (!stockEnabled)
            {
                return (false, "Stock tracking is disabled in Business Settings.");
            }

            var product = await _db.Products.FirstOrDefaultAsync(x =>
                x.Id == model.ProductId &&
                x.TenantId == tenantId.Value);

            if (product == null)
            {
                return (false, "Item not found.");
            }

            if (!product.TrackStock)
            {
                return (false, "This item is not stock-tracked and cannot be adjusted here.");
            }

            if (product.ProductType is "Service" or "Digital Product")
            {
                return (false, "Services and digital products cannot be adjusted in stock.");
            }

            if (model.AdjustmentType != "Correction" && model.Quantity <= 0)
            {
                return (false, "Quantity must be greater than zero.");
            }

            if (model.AdjustmentType == "Correction" && model.NewQuantity < 0)
            {
                return (false, "New quantity cannot be negative.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var before = product.QuantityInStock;
                decimal after;
                decimal movementQuantity;

                if (model.AdjustmentType == "Stock In")
                {
                    after = before + model.Quantity;
                    movementQuantity = model.Quantity;
                }
                else if (model.AdjustmentType == "Stock Out")
                {
                    if (model.Quantity > before)
                    {
                        return (false, $"Cannot remove more stock than available. Available: {before:0.##}");
                    }

                    after = before - model.Quantity;
                    movementQuantity = -model.Quantity;
                }
                else
                {
                    after = model.NewQuantity;
                    movementQuantity = after - before;
                }

                product.QuantityInStock = after;
                product.UpdatedAt = DateTime.UtcNow;

                _db.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    ProductId = product.Id,
                    MovementType = model.AdjustmentType,
                    Quantity = movementQuantity,
                    QuantityBefore = before,
                    QuantityAfter = after,
                    ReferenceType = "Manual",
                    ReferenceId = null,
                    Notes = string.IsNullOrWhiteSpace(model.Notes)
                        ? $"Manual {model.AdjustmentType.ToLower()} adjustment."
                        : model.Notes.Trim(),
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, "Stock updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Stock update failed: {ex.Message}");
            }
        }
    }
}