using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Refunds;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class RefundService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogService _auditLogService;

        public RefundService(
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

        public async Task<RefundableSaleViewModel?> GetRefundableSaleAsync(int saleId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var sale = await _db.Sales
                .AsNoTracking()
                .Include(x => x.SaleItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == saleId &&
                    x.TenantId == tenantId.Value);

            if (sale == null)
            {
                return null;
            }

            var returnedQuantities = await GetReturnedQuantitiesAsync(sale.Id);

            var productIds = sale.SaleItems
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            return new RefundableSaleViewModel
            {
                SaleId = sale.Id,
                SaleNumber = sale.SaleNumber,
                CreatedAt = sale.CreatedAt,
                CashierName = sale.CashierName ?? "-",
                PaymentMethod = sale.PaymentMethod,
                Status = sale.Status,
                CustomerName = sale.CustomerName,
                TotalAmount = sale.TotalAmount,
                Items = sale.SaleItems
                    .OrderBy(x => x.Id)
                    .Select(x =>
                    {
                        var alreadyReturned = returnedQuantities.TryGetValue(x.Id, out var qty)
                            ? qty
                            : 0;

                        var remaining = Math.Max(0, x.Quantity - alreadyReturned);

                        products.TryGetValue(x.ProductId, out var product);

                        var isVariant = x.ProductVariantId.HasValue;

                        return new RefundableSaleItemViewModel
                        {
                            SaleItemId = x.Id,

                            ProductId = x.ProductId,
                            ProductVariantId = x.ProductVariantId,

                            ProductName = x.ProductName,
                            SKU = string.IsNullOrWhiteSpace(x.VariantSKU) ? x.SKU : x.VariantSKU,

                            VariantName = x.VariantName,
                            VariantSize = x.VariantSize,
                            VariantColor = x.VariantColor,
                            VariantSKU = x.VariantSKU,
                            VariantBarcode = x.VariantBarcode,

                            ProductType = product?.ProductType ?? "Physical Product",
                            TrackStock = isVariant || (product?.TrackStock ?? false),
                            UnitOfMeasure = product?.UnitOfMeasure,

                            QuantitySold = x.Quantity,
                            QuantityAlreadyReturned = alreadyReturned,
                            QuantityRemaining = remaining,
                            UnitPrice = x.UnitPrice
                        };
                    })
                    .Where(x => x.QuantityRemaining > 0)
                    .ToList()
            };
        }

        public async Task<List<SaleReturnHistoryRowViewModel>> GetReturnHistoryAsync(DateTime? fromDate, DateTime? toDate, int? branchId = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<SaleReturnHistoryRowViewModel>();
            }

            var query = _db.SaleReturns
                .AsNoTracking()
                .Include(x => x.Sale)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    (!branchId.HasValue || x.BranchId == branchId.Value));

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < to);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .Select(x => new SaleReturnHistoryRowViewModel
                {
                    Id = x.Id,
                    ReturnNumber = x.ReturnNumber,
                    SaleNumber = x.Sale != null ? x.Sale.SaleNumber : "-",
                    CreatedAt = x.CreatedAt,
                    ReturnType = x.ReturnType,
                    RefundMethod = x.RefundMethod,
                    ReturnedByName = x.ReturnedByName,
                    TotalRefundAmount = x.TotalRefundAmount,
                    Status = x.Status,
                    Reason = x.Reason
                })
                .ToListAsync();
        }

        public async Task<RefundResult> CreateRefundAsync(CreateRefundRequest request)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserDisplayName();

            if (tenantId == null)
            {
                return Fail("Tenant not found.");
            }

            if (userId == null)
            {
                return Fail("Logged-in user could not be identified.");
            }

            if (request.Items.Count == 0)
            {
                return Fail("Select at least one item to refund or return.");
            }

            if (request.Items.Any(x => x.Quantity <= 0))
            {
                return Fail("Return quantity must be greater than zero.");
            }

            var returnType = NormalizeReturnType(request.ReturnType);
            var refundMethod = NormalizeRefundMethod(request.RefundMethod);

            var sale = await _db.Sales
                .Include(x => x.SaleItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SaleId &&
                    x.TenantId == tenantId.Value);

            if (sale == null)
            {
                return Fail("Sale not found.");
            }

            if (sale.Status is "Refunded" or "Voided")
            {
                return Fail("This sale has already been fully refunded or voided.");
            }

            var oldSaleValues = new
            {
                sale.Id,
                sale.SaleNumber,
                sale.Status,
                sale.TotalAmount
            };

            var returnedQuantities = await GetReturnedQuantitiesAsync(sale.Id);

            var requestedItems = request.Items
                .GroupBy(x => x.SaleItemId)
                .Select(x => new CreateRefundItemRequest
                {
                    SaleItemId = x.Key,
                    Quantity = x.Sum(i => i.Quantity)
                })
                .ToList();

            var saleItemIds = requestedItems.Select(x => x.SaleItemId).ToList();

            var selectedSaleItems = sale.SaleItems
                .Where(x => saleItemIds.Contains(x.Id))
                .ToList();

            if (selectedSaleItems.Count != requestedItems.Count)
            {
                return Fail("One or more selected sale items could not be found.");
            }

            foreach (var requestItem in requestedItems)
            {
                var saleItem = selectedSaleItems.First(x => x.Id == requestItem.SaleItemId);

                var alreadyReturned = returnedQuantities.TryGetValue(saleItem.Id, out var qty)
                    ? qty
                    : 0;

                var remaining = saleItem.Quantity - alreadyReturned;

                if (requestItem.Quantity > remaining)
                {
                    return Fail($"Return quantity for {saleItem.ProductName} is more than remaining quantity. Remaining: {remaining:0.##}");
                }
            }

            if (returnType == "Void")
            {
                var allRemainingItemIds = sale.SaleItems
                    .Where(x =>
                    {
                        var alreadyReturned = returnedQuantities.TryGetValue(x.Id, out var qty) ? qty : 0;
                        return x.Quantity - alreadyReturned > 0;
                    })
                    .Select(x => x.Id)
                    .OrderBy(x => x)
                    .ToList();

                var requestedIds = requestedItems
                    .Select(x => x.SaleItemId)
                    .OrderBy(x => x)
                    .ToList();

                if (!allRemainingItemIds.SequenceEqual(requestedIds))
                {
                    return Fail("A void must include all remaining items on the sale.");
                }
            }

            SaleReturn? completedReturn = null;
            decimal completedRefundTotal = 0;
            var auditItems = new List<object>();
            var stockAudit = new List<object>();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var saleReturn = new SaleReturn
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    SaleId = sale.Id,
                    ReturnedByUserId = userId.Value,
                    ReturnedByName = userName,
                    ReturnNumber = $"RET-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    ReturnType = returnType,
                    RefundMethod = refundMethod,
                    Status = "Completed",
                    Reason = Clean(request.Reason),
                    RestockItems = request.RestockItems,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.SaleReturns.Add(saleReturn);
                await _db.SaveChangesAsync();

                decimal refundTotal = 0;

                foreach (var requestItem in requestedItems)
                {
                    var saleItem = selectedSaleItems.First(x => x.Id == requestItem.SaleItemId);

                    var product = await _db.Products.FirstOrDefaultAsync(x =>
                        x.Id == saleItem.ProductId &&
                        x.TenantId == tenantId.Value);

                    if (product == null)
                    {
                        await tx.RollbackAsync();
                        return Fail("One of the products linked to this refund could not be found.");
                    }

                    ProductVariant? variant = null;

                    if (saleItem.ProductVariantId.HasValue)
                    {
                        variant = await _db.ProductVariants.FirstOrDefaultAsync(x =>
                            x.Id == saleItem.ProductVariantId.Value &&
                            x.TenantId == tenantId.Value &&
                            x.ProductId == product.Id);
                    }

                    var isVariant = variant != null;
                    var trackStock = isVariant || product.TrackStock;
                    var productType = product.ProductType;
                    var unitOfMeasure = product.UnitOfMeasure;

                    var lineTotal = requestItem.Quantity * saleItem.UnitPrice;
                    var unitCost = saleItem.UnitCost;
                    var costTotal = requestItem.Quantity * unitCost;

                    refundTotal += lineTotal;

                    _db.SaleReturnItems.Add(new SaleReturnItem
                    {
                        SaleReturnId = saleReturn.Id,
                        SaleItemId = saleItem.Id,
                        ProductId = saleItem.ProductId,
                        ProductName = saleItem.ProductName,
                        SKU = string.IsNullOrWhiteSpace(saleItem.VariantSKU) ? saleItem.SKU : saleItem.VariantSKU,
                        Quantity = requestItem.Quantity,
                        UnitPrice = saleItem.UnitPrice,
                        LineTotal = lineTotal,

                        UnitCost = unitCost,
                        CostTotal = costTotal,

                        TrackStock = trackStock,
                        ProductType = productType,
                        UnitOfMeasure = unitOfMeasure,
                        CreatedAt = DateTime.UtcNow
                    });

                    var wasRestocked = false;

                    if (request.RestockItems && variant != null)
                    {
                        var before = variant.QuantityInStock;
                        var after = before + requestItem.Quantity;

                        variant.QuantityInStock = after;
                        variant.UpdatedAt = DateTime.UtcNow;

                        _db.StockMovements.Add(new StockMovement
                        {
                            TenantId = tenantId.Value,
                            BranchId = branchId,
                            ProductId = product.Id,
                            MovementType = returnType == "Void" ? "Void Return" : "Return",
                            Quantity = requestItem.Quantity,
                            QuantityBefore = before,
                            QuantityAfter = after,
                            ReferenceType = "SaleReturn",
                            ReferenceId = saleReturn.Id,
                            Notes = $"{returnType} for sale {sale.SaleNumber}: {product.ProductName} - {variant.VariantName}",
                            CreatedAt = DateTime.UtcNow
                        });

                        stockAudit.Add(new
                        {
                            product.Id,
                            product.ProductName,
                            ProductVariantId = variant.Id,
                            variant.VariantName,
                            variant.Size,
                            variant.Color,
                            variant.SKU,
                            QuantityBefore = before,
                            QuantityReturned = requestItem.Quantity,
                            QuantityAfter = after
                        });

                        wasRestocked = true;
                    }
                    else if (request.RestockItems && product.TrackStock)
                    {
                        var before = product.QuantityInStock;
                        var after = before + requestItem.Quantity;

                        product.QuantityInStock = after;
                        product.UpdatedAt = DateTime.UtcNow;

                        _db.StockMovements.Add(new StockMovement
                        {
                            TenantId = tenantId.Value,
                            BranchId = branchId,
                            ProductId = product.Id,
                            MovementType = returnType == "Void" ? "Void Return" : "Return",
                            Quantity = requestItem.Quantity,
                            QuantityBefore = before,
                            QuantityAfter = after,
                            ReferenceType = "SaleReturn",
                            ReferenceId = saleReturn.Id,
                            Notes = $"{returnType} for sale {sale.SaleNumber}",
                            CreatedAt = DateTime.UtcNow
                        });

                        stockAudit.Add(new
                        {
                            product.Id,
                            product.ProductName,
                            product.SKU,
                            QuantityBefore = before,
                            QuantityReturned = requestItem.Quantity,
                            QuantityAfter = after
                        });

                        wasRestocked = true;
                    }

                    auditItems.Add(new
                    {
                        saleItem.ProductId,
                        saleItem.ProductName,
                        saleItem.SKU,
                        saleItem.ProductVariantId,
                        saleItem.VariantName,
                        saleItem.VariantSize,
                        saleItem.VariantColor,
                        saleItem.VariantSKU,
                        QuantityReturned = requestItem.Quantity,
                        saleItem.UnitPrice,
                        LineTotal = lineTotal,
                        Restocked = wasRestocked
                    });
                }

                saleReturn.TotalRefundAmount = refundTotal;
                saleReturn.UpdatedAt = DateTime.UtcNow;

                var updatedReturnedQuantities = await BuildUpdatedReturnedQuantitiesAsync(
                    sale.Id,
                    requestedItems,
                    returnedQuantities);

                var allItemsReturned = sale.SaleItems.All(x =>
                {
                    var returned = updatedReturnedQuantities.TryGetValue(x.Id, out var qty) ? qty : 0;
                    return returned >= x.Quantity;
                });

                sale.Status = allItemsReturned
                    ? returnType == "Void" ? "Voided" : "Refunded"
                    : "Partially Refunded";

                sale.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                completedReturn = saleReturn;
                completedRefundTotal = refundTotal;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Fail($"Refund failed: {ex.Message}");
            }

            if (completedReturn != null)
            {
                await _auditLogService.LogAsync(
                    module: "Refunds",
                    action: "Refund",
                    entityName: "SaleReturn",
                    entityId: completedReturn.Id,
                    summary: $"{returnType} processed for sale {sale.SaleNumber}. Return {completedReturn.ReturnNumber}. Amount {completedRefundTotal:0.00}.",
                    oldValues: oldSaleValues,
                    newValues: new
                    {
                        completedReturn.ReturnNumber,
                        SaleId = sale.Id,
                        sale.SaleNumber,
                        SaleStatusAfter = sale.Status,
                        ReturnType = returnType,
                        RefundMethod = refundMethod,
                        completedReturn.Status,
                        completedReturn.Reason,
                        completedReturn.RestockItems,
                        TotalRefundAmount = completedRefundTotal,
                        Items = auditItems,
                        StockRestocked = stockAudit
                    });
            }

            return new RefundResult
            {
                Success = true,
                Message = $"{returnType} completed successfully.",
                SaleReturnId = completedReturn?.Id,
                ReturnNumber = completedReturn?.ReturnNumber,
                TotalRefundAmount = completedRefundTotal
            };
        }

        private async Task<Dictionary<int, decimal>> GetReturnedQuantitiesAsync(int saleId)
        {
            return await _db.SaleReturnItems
                .AsNoTracking()
                .Where(x =>
                    x.SaleReturn != null &&
                    x.SaleReturn.SaleId == saleId &&
                    x.SaleReturn.Status == "Completed")
                .GroupBy(x => x.SaleItemId)
                .Select(g => new
                {
                    SaleItemId = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToDictionaryAsync(x => x.SaleItemId, x => x.Quantity);
        }

        private static Task<Dictionary<int, decimal>> BuildUpdatedReturnedQuantitiesAsync(
            int saleId,
            List<CreateRefundItemRequest> requestedItems,
            Dictionary<int, decimal> existingReturnedQuantities)
        {
            var updated = existingReturnedQuantities.ToDictionary(x => x.Key, x => x.Value);

            foreach (var item in requestedItems)
            {
                if (!updated.ContainsKey(item.SaleItemId))
                {
                    updated[item.SaleItemId] = 0;
                }

                updated[item.SaleItemId] += item.Quantity;
            }

            return Task.FromResult(updated);
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

        private static string NormalizeReturnType(string? value)
        {
            return value switch
            {
                "Return" => "Return",
                "Void" => "Void",
                _ => "Refund"
            };
        }

        private static string NormalizeRefundMethod(string? value)
        {
            return value switch
            {
                "Cash" => "Cash",
                "Card" => "Card",
                "EFT" => "EFT",
                "Store Credit" => "Store Credit",
                _ => "Original Payment"
            };
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static RefundResult Fail(string message)
        {
            return new RefundResult
            {
                Success = false,
                Message = message
            };
        }
    }
}