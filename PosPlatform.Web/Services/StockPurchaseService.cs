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

        public StockPurchaseService(
            AppDbContext db,
            TenantContextService tenantContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<PurchaseProductOptionViewModel>> GetStockProductOptionsAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<PurchaseProductOptionViewModel>();
            }

            return await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    x.TrackStock)
                .OrderBy(x => x.ProductName)
                .Select(x => new PurchaseProductOptionViewModel
                {
                    Id = x.Id,
                    ProductName = x.ProductName,
                    SKU = x.SKU,
                    CurrentStock = x.QuantityInStock,
                    CostPrice = x.CostPrice,
                    UnitOfMeasure = x.UnitOfMeasure
                })
                .ToListAsync();
        }

        public async Task<List<StockPurchaseListItemViewModel>> GetPurchasesAsync(DateTime? fromDate, DateTime? toDate, string? search)
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
                .Where(x => x.TenantId == tenantId.Value);

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
                return (false, "Select product for every line.");
            }

            if (model.Items.Any(x => x.Quantity <= 0))
            {
                return (false, "Quantity must be greater than zero.");
            }

            if (model.Items.Any(x => x.UnitCost < 0))
            {
                return (false, "Unit cost cannot be negative.");
            }

            var supplierExists = await _db.Suppliers.AnyAsync(x =>
                x.Id == model.SupplierId &&
                x.TenantId == tenantId.Value &&
                x.IsActive);

            if (!supplierExists)
            {
                return (false, "Supplier not found or inactive.");
            }

            var groupedItems = model.Items
                .GroupBy(x => x.ProductId)
                .Select(x => new CreateStockPurchaseItemModel
                {
                    ProductId = x.Key,
                    Quantity = x.Sum(i => i.Quantity),
                    UnitCost = x.Last().UnitCost
                })
                .ToList();

            var productIds = groupedItems.Select(x => x.ProductId).ToList();

            var products = await _db.Products
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    productIds.Contains(x.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return (false, "One or more products could not be found.");
            }

            if (products.Any(x => !x.TrackStock))
            {
                return (false, "Only stock-tracked products can be received.");
            }

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

                    var before = product.QuantityInStock;
                    var after = before + item.Quantity;
                    var lineTotal = item.Quantity * item.UnitCost;

                    _db.StockPurchaseItems.Add(new StockPurchaseItem
                    {
                        StockPurchaseId = purchase.Id,
                        ProductId = product.Id,
                        ProductName = product.ProductName,
                        SKU = product.SKU,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        LineTotal = lineTotal,
                        QuantityBefore = before,
                        QuantityAfter = after,
                        UnitOfMeasure = product.UnitOfMeasure,
                        CreatedAt = DateTime.UtcNow
                    });

                    product.QuantityInStock = after;

                    if (model.UpdateProductCostPrice)
                    {
                        product.CostPrice = item.UnitCost;
                    }

                    product.UpdatedAt = DateTime.UtcNow;

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
                        Notes = $"Stock received from purchase {purchase.PurchaseNumber}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, $"Stock purchase received successfully. Purchase: {purchase.PurchaseNumber}");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();

                return (false, $"Stock purchase failed: {ex.Message}");
            }
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