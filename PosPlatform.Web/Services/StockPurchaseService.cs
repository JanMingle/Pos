using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Purchases;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class StockPurchaseService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogService _auditLogService;

        public StockPurchaseService(
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

        public async Task<List<PurchaseProductOptionViewModel>> GetStockProductOptionsAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return new List<PurchaseProductOptionViewModel>();
            }

            var products = await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    x.TrackStock &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value))
                .OrderBy(x => x.ProductName)
                .Select(x => new PurchaseProductOptionViewModel
                {
                    Id = x.Id,
                    ProductId = x.Id,
                    ProductVariantId = null,

                    ProductName = x.ProductName,
                    DisplayName = x.ProductName,

                    SKU = x.SKU,
                    CurrentStock = x.QuantityInStock,
                    CostPrice = x.CostPrice,
                    UnitOfMeasure = x.UnitOfMeasure
                })
                .ToListAsync();

            var variants = await _db.ProductVariants
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    x.Product != null &&
                    x.Product.IsActive &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value))
                .OrderBy(x => x.Product!.ProductName)
                .ThenBy(x => x.VariantName)
                .Select(x => new PurchaseProductOptionViewModel
                {
                    Id = x.ProductId,
                    ProductId = x.ProductId,
                    ProductVariantId = x.Id,

                    ProductName = x.Product != null ? x.Product.ProductName : "Product",
                    DisplayName =
                        (x.Product != null ? x.Product.ProductName : "Product") +
                        " - " +
                        x.VariantName,

                    SKU = x.SKU,

                    VariantName = x.VariantName,
                    VariantSize = x.Size,
                    VariantColor = x.Color,
                    VariantSKU = x.SKU,
                    VariantBarcode = x.Barcode,

                    CurrentStock = x.QuantityInStock,
                    CostPrice = x.CostPrice,
                    UnitOfMeasure = x.Product != null ? x.Product.UnitOfMeasure : null
                })
                .ToListAsync();

            return variants
                .Concat(products)
                .OrderBy(x => x.DisplayName)
                .ToList();
        }

        public async Task<List<StockPurchaseListItemViewModel>> GetPurchasesAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string? search,
            int? branchId = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<StockPurchaseListItemViewModel>();
            }

            var query = _db.StockPurchases
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.StockPurchaseItems)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    (!branchId.HasValue || x.BranchId == branchId.Value));

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.PurchaseDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.PurchaseDate < to);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.PurchaseNumber.Contains(term) ||
                    (x.SupplierInvoiceNumber != null && x.SupplierInvoiceNumber.Contains(term)) ||
                    (x.Supplier != null && x.Supplier.SupplierName.Contains(term)));
            }

            return await query
                .OrderByDescending(x => x.PurchaseDate)
                .ThenByDescending(x => x.Id)
                .Take(100)
                .Select(x => new StockPurchaseListItemViewModel
                {
                    Id = x.Id,
                    PurchaseNumber = x.PurchaseNumber,
                    SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "-",
                    SupplierInvoiceNumber = x.SupplierInvoiceNumber,
                    PurchaseDate = x.PurchaseDate,
                    ItemCount = x.StockPurchaseItems.Count,
                    TotalQuantity = x.StockPurchaseItems.Sum(i => i.Quantity),
                    Subtotal = x.Subtotal,
                    TaxAmount = x.TaxAmount,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status,
                    CreatedByName = x.CreatedByName
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateAndReceiveAsync(CreateStockPurchaseModel model)
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

            if (model.SupplierId <= 0)
            {
                return (false, "Select supplier.");
            }

            if (model.Items.Count == 0)
            {
                return (false, "Add at least one item to receive.");
            }

            if (model.Items.Any(x => x.ProductId <= 0))
            {
                return (false, "Select product or variant for every line.");
            }

            if (model.Items.Any(x => x.Quantity <= 0))
            {
                return (false, "Quantity must be greater than zero.");
            }

            if (model.Items.Any(x => x.UnitCost < 0))
            {
                return (false, "Unit cost cannot be negative.");
            }

            if (model.TaxAmount < 0)
            {
                return (false, "Tax amount cannot be negative.");
            }

            var supplier = await _db.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == model.SupplierId &&
                    x.TenantId == tenantId.Value &&
                    x.IsActive);

            if (supplier == null)
            {
                return (false, "Supplier not found or inactive.");
            }

            var groupedItems = model.Items
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.ProductVariantId
                })
                .Select(x => new CreateStockPurchaseItemModel
                {
                    ProductId = x.Key.ProductId,
                    ProductVariantId = x.Key.ProductVariantId,
                    Quantity = x.Sum(i => i.Quantity),
                    UnitCost = x.Last().UnitCost
                })
                .ToList();

            var productIds = groupedItems
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var variantIds = groupedItems
                .Where(x => x.ProductVariantId.HasValue)
                .Select(x => x.ProductVariantId!.Value)
                .Distinct()
                .ToList();

            var products = await _db.Products
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    productIds.Contains(x.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return (false, "One or more products could not be found.");
            }

            var variants = await _db.ProductVariants
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    variantIds.Contains(x.Id))
                .ToListAsync();

            if (variants.Count != variantIds.Count)
            {
                return (false, "One or more product variants could not be found.");
            }

            foreach (var item in groupedItems)
            {
                var product = products.FirstOrDefault(x => x.Id == item.ProductId);

                if (product == null)
                {
                    return (false, "One or more products could not be found.");
                }

                if (item.ProductVariantId.HasValue)
                {
                    var variant = variants.FirstOrDefault(x => x.Id == item.ProductVariantId.Value);

                    if (variant == null)
                    {
                        return (false, "One or more product variants could not be found.");
                    }

                    if (variant.ProductId != product.Id)
                    {
                        return (false, "A selected variant does not belong to the selected product.");
                    }

                    if (!variant.IsActive)
                    {
                        return (false, $"{product.ProductName} - {variant.VariantName} is inactive.");
                    }
                }
                else if (!product.TrackStock)
                {
                    return (false, "Only stock-tracked products can be received.");
                }
            }

            StockPurchase? completedPurchase = null;
            var auditItems = new List<object>();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var subtotal = groupedItems.Sum(x => x.Quantity * x.UnitCost);
                var total = subtotal + model.TaxAmount;

                var purchase = new StockPurchase
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    SupplierId = model.SupplierId,
                    PurchaseNumber = $"PO-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    SupplierInvoiceNumber = Clean(model.SupplierInvoiceNumber),
                    PurchaseDate = model.PurchaseDate.Date,
                    Subtotal = subtotal,
                    TaxAmount = model.TaxAmount,
                    TotalAmount = total,
                    Status = "Received",
                    Notes = Clean(model.Notes),
                    CreatedByUserId = userId.Value,
                    CreatedByName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.StockPurchases.Add(purchase);
                await _db.SaveChangesAsync();

                foreach (var item in groupedItems)
                {
                    var product = products.First(x => x.Id == item.ProductId);
                    var variant = item.ProductVariantId.HasValue
                        ? variants.First(x => x.Id == item.ProductVariantId.Value)
                        : null;

                    var before = variant != null
                        ? variant.QuantityInStock
                        : product.QuantityInStock;

                    var after = before + item.Quantity;
                    var oldCostPrice = variant != null ? variant.CostPrice : product.CostPrice;
                    var lineTotal = item.Quantity * item.UnitCost;

                    var itemName = variant == null
                        ? product.ProductName
                        : $"{product.ProductName} - {variant.VariantName}";

                    var sku = variant?.SKU ?? product.SKU;

                    _db.StockPurchaseItems.Add(new StockPurchaseItem
                    {
                        StockPurchaseId = purchase.Id,
                        ProductId = product.Id,
                        ProductVariantId = variant?.Id,

                        ProductName = itemName,
                        SKU = sku,

                        VariantName = variant?.VariantName,
                        VariantSize = variant?.Size,
                        VariantColor = variant?.Color,
                        VariantSKU = variant?.SKU,
                        VariantBarcode = variant?.Barcode,

                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        LineTotal = lineTotal,
                        QuantityBefore = before,
                        QuantityAfter = after,
                        UnitOfMeasure = product.UnitOfMeasure,
                        CreatedAt = DateTime.UtcNow
                    });

                    if (variant != null)
                    {
                        variant.QuantityInStock = after;

                        if (model.UpdateProductCostPrice)
                        {
                            variant.CostPrice = item.UnitCost;
                        }

                        variant.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        product.QuantityInStock = after;

                        if (model.UpdateProductCostPrice)
                        {
                            product.CostPrice = item.UnitCost;
                        }

                        product.UpdatedAt = DateTime.UtcNow;
                    }

                    _db.StockMovements.Add(new StockMovement
                    {
                        TenantId = tenantId.Value,
                        BranchId = branchId,
                        ProductId = product.Id,
                        MovementType = "Purchase",
                        Quantity = item.Quantity,
                        QuantityBefore = before,
                        QuantityAfter = after,
                        ReferenceType = "StockPurchase",
                        ReferenceId = purchase.Id,
                        Notes = variant == null
                            ? $"Stock received from purchase {purchase.PurchaseNumber}"
                            : $"Variant stock received from purchase {purchase.PurchaseNumber}: {product.ProductName} - {variant.VariantName}",
                        CreatedAt = DateTime.UtcNow
                    });

                    auditItems.Add(new
                    {
                        ProductId = product.Id,
                        product.ProductName,
                        product.SKU,

                        ProductVariantId = variant?.Id,
                        VariantName = variant?.VariantName,
                        VariantSize = variant?.Size,
                        VariantColor = variant?.Color,
                        VariantSKU = variant?.SKU,

                        QuantityReceived = item.Quantity,
                        UnitCost = item.UnitCost,
                        LineTotal = lineTotal,
                        UnitOfMeasure = product.UnitOfMeasure,
                        QuantityBefore = before,
                        QuantityAfter = after,
                        CostPriceBefore = oldCostPrice,
                        CostPriceAfter = variant != null ? variant.CostPrice : product.CostPrice,
                        CostPriceUpdated = model.UpdateProductCostPrice
                    });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                completedPurchase = purchase;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Stock purchase failed: {ex.Message}");
            }

            if (completedPurchase != null)
            {
                await _auditLogService.LogAsync(
                    module: "Stock Purchases",
                    action: "Create",
                    entityName: "StockPurchase",
                    entityId: completedPurchase.Id,
                    summary: $"Received stock purchase {completedPurchase.PurchaseNumber} from {supplier.SupplierName}. Total {completedPurchase.TotalAmount:0.00}.",
                    oldValues: null,
                    newValues: new
                    {
                        completedPurchase.Id,
                        completedPurchase.PurchaseNumber,
                        completedPurchase.BranchId,
                        SupplierId = supplier.Id,
                        SupplierName = supplier.SupplierName,
                        completedPurchase.SupplierInvoiceNumber,
                        completedPurchase.PurchaseDate,
                        completedPurchase.Subtotal,
                        completedPurchase.TaxAmount,
                        completedPurchase.TotalAmount,
                        completedPurchase.Status,
                        completedPurchase.Notes,
                        completedPurchase.CreatedByUserId,
                        completedPurchase.CreatedByName,
                        UpdateProductCostPrice = model.UpdateProductCostPrice,
                        Items = auditItems
                    });
            }

            return (true, $"Stock purchase received successfully. Purchase: {completedPurchase?.PurchaseNumber}");
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