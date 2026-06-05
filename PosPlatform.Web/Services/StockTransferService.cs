using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.StockTransfers;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class StockTransferService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogService _auditLogService;

        public StockTransferService(
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

        public async Task<List<StockTransferProductOptionViewModel>> GetSourceProductsAsync(int sourceBranchId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null || sourceBranchId <= 0)
            {
                return new List<StockTransferProductOptionViewModel>();
            }

            return await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    x.TrackStock &&
                    x.QuantityInStock > 0 &&
                    (x.BranchId == sourceBranchId || x.BranchId == null))
                .OrderBy(x => x.ProductName)
                .Select(x => new StockTransferProductOptionViewModel
                {
                    Id = x.Id,
                    ProductName = x.ProductName,
                    SKU = x.SKU,
                    QuantityInStock = x.QuantityInStock,
                    UnitOfMeasure = x.UnitOfMeasure,
                    CostPrice = x.CostPrice
                })
                .ToListAsync();
        }

        public async Task<List<StockTransferHistoryRowViewModel>> GetTransfersAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int? branchId,
            string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<StockTransferHistoryRowViewModel>();
            }

            var query = _db.StockTransfers
                .AsNoTracking()
                .Include(x => x.SourceBranch)
                .Include(x => x.DestinationBranch)
                .Include(x => x.Items)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    (!branchId.HasValue ||
                        x.SourceBranchId == branchId.Value ||
                        x.DestinationBranchId == branchId.Value));

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.TransferDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.TransferDate < to);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.TransferNumber.Contains(term) ||
                    (x.SourceBranch != null && x.SourceBranch.Name.Contains(term)) ||
                    (x.DestinationBranch != null && x.DestinationBranch.Name.Contains(term)) ||
                    (x.Notes != null && x.Notes.Contains(term)));
            }

            return await query
                .OrderByDescending(x => x.TransferDate)
                .ThenByDescending(x => x.Id)
                .Take(150)
                .Select(x => new StockTransferHistoryRowViewModel
                {
                    Id = x.Id,
                    TransferNumber = x.TransferNumber,
                    TransferDate = x.TransferDate,
                    SourceBranchName = x.SourceBranch != null ? x.SourceBranch.Name : "-",
                    DestinationBranchName = x.DestinationBranch != null ? x.DestinationBranch.Name : "-",
                    ItemCount = x.Items.Count,
                    TotalQuantity = x.Items.Sum(i => i.Quantity),
                    Status = x.Status,
                    CreatedByName = x.CreatedByName,
                    Notes = x.Notes
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateTransferAsync(CreateStockTransferModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
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

            if (model.SourceBranchId <= 0)
            {
                return (false, "Select source branch.");
            }

            if (model.DestinationBranchId <= 0)
            {
                return (false, "Select destination branch.");
            }

            if (model.SourceBranchId == model.DestinationBranchId)
            {
                return (false, "Source and destination branches cannot be the same.");
            }

            var sourceBranch = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == model.SourceBranchId &&
                    x.TenantId == tenantId.Value &&
                    x.IsActive);

            if (sourceBranch == null)
            {
                return (false, "Source branch was not found or is inactive.");
            }

            var destinationBranch = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == model.DestinationBranchId &&
                    x.TenantId == tenantId.Value &&
                    x.IsActive);

            if (destinationBranch == null)
            {
                return (false, "Destination branch was not found or is inactive.");
            }

            model.Items = model.Items
                .Where(x => x.ProductId > 0 && x.Quantity > 0)
                .ToList();

            if (model.Items.Count == 0)
            {
                return (false, "Add at least one item to transfer.");
            }

            var groupedItems = model.Items
                .GroupBy(x => x.ProductId)
                .Select(x => new CreateStockTransferItemModel
                {
                    ProductId = x.Key,
                    Quantity = x.Sum(i => i.Quantity)
                })
                .ToList();

            var productIds = groupedItems.Select(x => x.ProductId).ToList();

            var sourceProducts = await _db.Products
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    productIds.Contains(x.Id) &&
                    x.TrackStock &&
                    (x.BranchId == model.SourceBranchId || x.BranchId == null))
                .ToListAsync();

            if (sourceProducts.Count != productIds.Count)
            {
                return (false, "One or more products could not be found for the selected source branch.");
            }

            foreach (var item in groupedItems)
            {
                var product = sourceProducts.First(x => x.Id == item.ProductId);

                if (!product.IsActive)
                {
                    return (false, $"{product.ProductName} is inactive.");
                }

                if (product.QuantityInStock < item.Quantity)
                {
                    return (false, $"Not enough stock for {product.ProductName}. Available: {product.QuantityInStock:0.##}");
                }
            }

            StockTransfer? completedTransfer = null;
            var transferItemsAudit = new List<object>();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var transfer = new StockTransfer
                {
                    TenantId = tenantId.Value,
                    TransferNumber = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    SourceBranchId = model.SourceBranchId,
                    DestinationBranchId = model.DestinationBranchId,
                    TransferDate = model.TransferDate.Date,
                    Status = "Completed",
                    Notes = Clean(model.Notes),
                    CreatedByUserId = userId.Value,
                    CreatedByName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.StockTransfers.Add(transfer);
                await _db.SaveChangesAsync();

                foreach (var item in groupedItems)
                {
                    var sourceProduct = sourceProducts.First(x => x.Id == item.ProductId);

                    var targetProduct = await GetOrCreateDestinationProductAsync(
                        sourceProduct,
                        tenantId.Value,
                        model.DestinationBranchId);

                    var sourceBefore = sourceProduct.QuantityInStock;
                    var sourceAfter = sourceBefore - item.Quantity;

                    var destinationBefore = targetProduct.QuantityInStock;
                    var destinationAfter = destinationBefore + item.Quantity;

                    sourceProduct.QuantityInStock = sourceAfter;
                    sourceProduct.UpdatedAt = DateTime.UtcNow;

                    targetProduct.QuantityInStock = destinationAfter;
                    targetProduct.UpdatedAt = DateTime.UtcNow;

                    _db.StockTransferItems.Add(new StockTransferItem
                    {
                        StockTransferId = transfer.Id,
                        SourceProductId = sourceProduct.Id,
                        TargetProductId = targetProduct.Id,
                        ProductName = sourceProduct.ProductName,
                        SKU = sourceProduct.SKU,
                        Quantity = item.Quantity,
                        SourceQuantityBefore = sourceBefore,
                        SourceQuantityAfter = sourceAfter,
                        DestinationQuantityBefore = destinationBefore,
                        DestinationQuantityAfter = destinationAfter,
                        UnitOfMeasure = sourceProduct.UnitOfMeasure,
                        CreatedAt = DateTime.UtcNow
                    });

                    _db.StockMovements.Add(new StockMovement
                    {
                        TenantId = tenantId.Value,
                        BranchId = model.SourceBranchId,
                        ProductId = sourceProduct.Id,
                        MovementType = "Transfer Out",
                        Quantity = -item.Quantity,
                        QuantityBefore = sourceBefore,
                        QuantityAfter = sourceAfter,
                        ReferenceType = "StockTransfer",
                        ReferenceId = transfer.Id,
                        Notes = $"Transferred out via {transfer.TransferNumber}",
                        CreatedAt = DateTime.UtcNow
                    });

                    _db.StockMovements.Add(new StockMovement
                    {
                        TenantId = tenantId.Value,
                        BranchId = model.DestinationBranchId,
                        ProductId = targetProduct.Id,
                        MovementType = "Transfer In",
                        Quantity = item.Quantity,
                        QuantityBefore = destinationBefore,
                        QuantityAfter = destinationAfter,
                        ReferenceType = "StockTransfer",
                        ReferenceId = transfer.Id,
                        Notes = $"Transferred in via {transfer.TransferNumber}",
                        CreatedAt = DateTime.UtcNow
                    });

                    transferItemsAudit.Add(new
                    {
                        ProductName = sourceProduct.ProductName,
                        SKU = sourceProduct.SKU,
                        Quantity = item.Quantity,
                        UnitOfMeasure = sourceProduct.UnitOfMeasure,
                        SourceProductId = sourceProduct.Id,
                        TargetProductId = targetProduct.Id,
                        SourceBranchId = model.SourceBranchId,
                        DestinationBranchId = model.DestinationBranchId,
                        SourceQuantityBefore = sourceBefore,
                        SourceQuantityAfter = sourceAfter,
                        DestinationQuantityBefore = destinationBefore,
                        DestinationQuantityAfter = destinationAfter
                    });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                completedTransfer = transfer;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Stock transfer failed: {ex.Message}");
            }

            if (completedTransfer != null)
            {
                var totalQuantity = groupedItems.Sum(x => x.Quantity);

                await _auditLogService.LogAsync(
                    module: "Stock Transfers",
                    action: "Transfer",
                    entityName: "StockTransfer",
                    entityId: completedTransfer.Id,
                    summary: $"Transferred {totalQuantity:0.##} item(s) from {sourceBranch.Name} to {destinationBranch.Name}. Transfer {completedTransfer.TransferNumber}.",
                    oldValues: null,
                    newValues: new
                    {
                        completedTransfer.TransferNumber,
                        SourceBranch = sourceBranch.Name,
                        DestinationBranch = destinationBranch.Name,
                        completedTransfer.TransferDate,
                        completedTransfer.Status,
                        completedTransfer.Notes,
                        Items = transferItemsAudit
                    });
            }

            return (true, $"Stock transfer completed successfully. Transfer: {completedTransfer?.TransferNumber}");
        }

        private async Task<Product> GetOrCreateDestinationProductAsync(Product sourceProduct, int tenantId, int destinationBranchId)
        {
            var targetProduct = await _db.Products.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.BranchId == destinationBranchId &&
                x.SKU == sourceProduct.SKU);

            if (targetProduct != null)
            {
                return targetProduct;
            }

            targetProduct = new Product
            {
                TenantId = tenantId,
                BranchId = destinationBranchId,

                ProductName = sourceProduct.ProductName,
                SKU = sourceProduct.SKU,
                Barcode = sourceProduct.Barcode,
                Description = sourceProduct.Description,

                ProductCategoryId = sourceProduct.ProductCategoryId,

                ProductType = sourceProduct.ProductType,
                TrackStock = true,
                AgeRestricted = sourceProduct.AgeRestricted,
                UnitOfMeasure = sourceProduct.UnitOfMeasure,
                DurationMinutes = sourceProduct.DurationMinutes,

                CostPrice = sourceProduct.CostPrice,
                SellingPrice = sourceProduct.SellingPrice,
                QuantityInStock = 0,
                ReorderLevel = sourceProduct.ReorderLevel,

                IsActive = true,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Products.Add(targetProduct);
            await _db.SaveChangesAsync();

            return targetProduct;
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