using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Sales;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class SaleService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogService _auditLogService;

        public SaleService(
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

        public async Task<List<SaleProductOptionViewModel>> SearchProductsAsync(string? search = null)
        {
            return await SearchSaleItemsAsync(search);
        }

        public async Task<List<SaleProductOptionViewModel>> SearchSaleItemsAsync(string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return new List<SaleProductOptionViewModel>();
            }

            var term = search?.Trim();

            var productsQuery = _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value));

            if (!string.IsNullOrWhiteSpace(term))
            {
                productsQuery = productsQuery.Where(x =>
                    x.ProductName.Contains(term) ||
                    x.SKU.Contains(term) ||
                    x.ProductType.Contains(term) ||
                    (x.Barcode != null && x.Barcode.Contains(term)));
            }

            var productOptions = await productsQuery
                .OrderBy(x => x.ProductName)
                .Take(40)
                .Select(x => new SaleProductOptionViewModel
                {
                    Id = x.Id,
                    ProductId = x.Id,
                    ProductVariantId = null,

                    ProductName = x.ProductName,
                    DisplayName = x.ProductName,

                    SKU = x.SKU,
                    Barcode = x.Barcode,

                    VariantName = null,
                    VariantSize = null,
                    VariantColor = null,
                    VariantSKU = null,
                    VariantBarcode = null,

                    ProductType = x.ProductType,
                    TrackStock = x.TrackStock,
                    AgeRestricted = x.AgeRestricted,
                    UnitOfMeasure = x.UnitOfMeasure,
                    DurationMinutes = x.DurationMinutes,

                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            var variantsQuery = _db.ProductVariants
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    x.Product != null &&
                    x.Product.IsActive &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value));

            if (!string.IsNullOrWhiteSpace(term))
            {
                variantsQuery = variantsQuery.Where(x =>
                    x.VariantName.Contains(term) ||
                    x.SKU.Contains(term) ||
                    (x.Barcode != null && x.Barcode.Contains(term)) ||
                    (x.Size != null && x.Size.Contains(term)) ||
                    (x.Color != null && x.Color.Contains(term)) ||
                    (x.Product != null && x.Product.ProductName.Contains(term)));
            }

            var variantOptions = await variantsQuery
                .OrderBy(x => x.Product!.ProductName)
                .ThenBy(x => x.VariantName)
                .Take(60)
                .Select(x => new SaleProductOptionViewModel
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
                    Barcode = x.Barcode,

                    VariantName = x.VariantName,
                    VariantSize = x.Size,
                    VariantColor = x.Color,
                    VariantSKU = x.SKU,
                    VariantBarcode = x.Barcode,

                    ProductType = x.Product != null ? x.Product.ProductType : "Physical Product",
                    TrackStock = true,
                    AgeRestricted = x.Product != null && x.Product.AgeRestricted,
                    UnitOfMeasure = x.Product != null ? x.Product.UnitOfMeasure : "Each",
                    DurationMinutes = x.Product != null ? x.Product.DurationMinutes : null,

                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    IsActive = x.IsActive
                })
                .ToListAsync();

            return variantOptions
                .Concat(productOptions)
                .OrderBy(x => x.DisplayName)
                .ToList();
        }

        public async Task<SaleProductOptionViewModel?> GetProductByBarcodeAsync(string? barcodeOrSku)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null || string.IsNullOrWhiteSpace(barcodeOrSku))
            {
                return null;
            }

            var code = barcodeOrSku.Trim();

            var variant = await _db.ProductVariants
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    x.Product != null &&
                    x.Product.IsActive &&
                    (
                        x.SKU == code ||
                        (x.Barcode != null && x.Barcode == code)
                    ) &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value))
                .OrderByDescending(x => branchId.HasValue && x.BranchId == branchId.Value)
                .ThenBy(x => x.Product!.ProductName)
                .ThenBy(x => x.VariantName)
                .Select(x => new SaleProductOptionViewModel
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
                    Barcode = x.Barcode,

                    VariantName = x.VariantName,
                    VariantSize = x.Size,
                    VariantColor = x.Color,
                    VariantSKU = x.SKU,
                    VariantBarcode = x.Barcode,

                    ProductType = x.Product != null ? x.Product.ProductType : "Physical Product",
                    TrackStock = true,
                    AgeRestricted = x.Product != null && x.Product.AgeRestricted,
                    UnitOfMeasure = x.Product != null ? x.Product.UnitOfMeasure : "Each",
                    DurationMinutes = x.Product != null ? x.Product.DurationMinutes : null,

                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();

            if (variant != null)
            {
                return variant;
            }

            return await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    (
                        x.SKU == code ||
                        (x.Barcode != null && x.Barcode == code)
                    ) &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value))
                .OrderByDescending(x => branchId.HasValue && x.BranchId == branchId.Value)
                .ThenBy(x => x.ProductName)
                .Select(x => new SaleProductOptionViewModel
                {
                    Id = x.Id,
                    ProductId = x.Id,
                    ProductVariantId = null,

                    ProductName = x.ProductName,
                    DisplayName = x.ProductName,

                    SKU = x.SKU,
                    Barcode = x.Barcode,

                    VariantName = null,
                    VariantSize = null,
                    VariantColor = null,
                    VariantSKU = null,
                    VariantBarcode = null,

                    ProductType = x.ProductType,
                    TrackStock = x.TrackStock,
                    AgeRestricted = x.AgeRestricted,
                    UnitOfMeasure = x.UnitOfMeasure,
                    DurationMinutes = x.DurationMinutes,

                    CostPrice = x.CostPrice,
                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<SaleResult> CompleteSaleAsync(CreateSaleRequest request)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();
            var cashierUserId = GetCurrentUserId();
            var cashierName = GetCurrentUserDisplayName();

            if (tenantId == null)
            {
                return Fail("Tenant not found.");
            }

            if (cashierUserId == null)
            {
                return Fail("Logged-in user could not be identified.");
            }

            var hasOpenShift = await _db.CashierShifts
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TenantId == tenantId.Value &&
                    x.CashierUserId == cashierUserId.Value &&
                    x.Status == "Open");

            if (!hasOpenShift)
            {
                return new SaleResult
                {
                    Success = false,
                    Message = "You must open a cashier shift before completing sales.",
                    RequiresOpenShift = true
                };
            }

            var settings = await _db.BusinessSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            var allowDiscounts = settings?.AllowDiscounts ?? true;
            var allowNegativeStock = settings?.AllowNegativeStock ?? false;
            var requireCustomerForSale = settings?.RequireCustomerForSale ?? false;
            var taxEnabled = settings?.TaxEnabled ?? false;
            var taxRate = settings?.TaxRate ?? 0;

            Customer? selectedCustomer = null;

            if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
            {
                selectedCustomer = await _db.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.CustomerId.Value &&
                        x.TenantId == tenantId.Value &&
                        x.IsActive);

                if (selectedCustomer == null)
                {
                    return Fail("Selected customer could not be found or is inactive.");
                }
            }

            var requestCustomerName = Clean(request.CustomerName);
            var requestCustomerPhone = Clean(request.CustomerPhone);

            if (requireCustomerForSale && selectedCustomer == null && string.IsNullOrWhiteSpace(requestCustomerName))
            {
                return Fail("Customer is required for this business.");
            }

            var finalCustomerName = selectedCustomer != null
                ? GetCustomerDisplayName(selectedCustomer)
                : requestCustomerName;

            var finalCustomerPhone = selectedCustomer?.Phone ?? requestCustomerPhone;

            if (request.Items.Count == 0)
            {
                return Fail("Add at least one item before completing the sale.");
            }

            if (request.Items.Any(x => x.Quantity <= 0))
            {
                return Fail("Quantity must be greater than zero.");
            }

            if (request.DiscountAmount < 0)
            {
                return Fail("Discount cannot be negative.");
            }

            if (!allowDiscounts && request.DiscountAmount > 0)
            {
                return Fail("Discounts are disabled in Business Settings.");
            }

            var groupedItems = request.Items
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.ProductVariantId
                })
                .Select(x => new CreateSaleItemRequest
                {
                    ProductId = x.Key.ProductId,
                    ProductVariantId = x.Key.ProductVariantId,
                    Quantity = x.Sum(i => i.Quantity)
                })
                .ToList();

            var productIds = groupedItems.Select(x => x.ProductId).Distinct().ToList();
            var variantIds = groupedItems
                .Where(x => x.ProductVariantId.HasValue)
                .Select(x => x.ProductVariantId!.Value)
                .Distinct()
                .ToList();

            var productsForValidation = await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    productIds.Contains(x.Id))
                .ToListAsync();

            var variantsForValidation = await _db.ProductVariants
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    variantIds.Contains(x.Id))
                .ToListAsync();

            foreach (var item in groupedItems)
            {
                var product = productsForValidation.FirstOrDefault(x => x.Id == item.ProductId);

                if (product == null)
                {
                    return Fail("One of the selected items could not be found.");
                }

                if (!product.IsActive)
                {
                    return Fail($"{product.ProductName} is inactive and cannot be sold.");
                }

                if (product.AgeRestricted && !request.AgeRestrictionConfirmed)
                {
                    return new SaleResult
                    {
                        Success = false,
                        Message = $"{product.ProductName} is age-restricted. Confirm age verification before completing the sale.",
                        RequiresAgeConfirmation = true
                    };
                }

                if (item.ProductVariantId.HasValue)
                {
                    var variant = variantsForValidation.FirstOrDefault(x => x.Id == item.ProductVariantId.Value);

                    if (variant == null)
                    {
                        return Fail("One of the selected variants could not be found.");
                    }

                    if (variant.ProductId != product.Id)
                    {
                        return Fail("A selected variant does not belong to the selected product.");
                    }

                    if (!variant.IsActive)
                    {
                        return Fail($"{product.ProductName} - {variant.VariantName} is inactive and cannot be sold.");
                    }

                    if (!allowNegativeStock && variant.QuantityInStock < item.Quantity)
                    {
                        return Fail($"Not enough stock for {product.ProductName} - {variant.VariantName}. Available: {variant.QuantityInStock:0.##}");
                    }
                }
                else
                {
                    if (product.TrackStock && !allowNegativeStock && product.QuantityInStock < item.Quantity)
                    {
                        return Fail($"Not enough stock for {product.ProductName}. Available: {product.QuantityInStock:0.##}");
                    }
                }
            }

            Sale? completedSale = null;
            var auditItems = new List<object>();
            var stockAudit = new List<object>();

            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var sale = new Sale
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    CashierUserId = cashierUserId,
                    CashierName = cashierName,

                    CustomerId = selectedCustomer?.Id,
                    CustomerName = finalCustomerName,
                    CustomerPhone = finalCustomerPhone,
                    Notes = Clean(request.Notes),
                    SaleNumber = $"SALE-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod.Trim(),
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Sales.Add(sale);
                await _db.SaveChangesAsync();

                decimal subtotal = 0;

                foreach (var item in groupedItems)
                {
                    var product = await _db.Products.FirstOrDefaultAsync(x =>
                        x.Id == item.ProductId &&
                        x.TenantId == tenantId.Value);

                    if (product == null)
                    {
                        await transaction.RollbackAsync();
                        return Fail("One of the selected items could not be found.");
                    }

                    ProductVariant? variant = null;

                    if (item.ProductVariantId.HasValue)
                    {
                        variant = await _db.ProductVariants.FirstOrDefaultAsync(x =>
                            x.Id == item.ProductVariantId.Value &&
                            x.TenantId == tenantId.Value &&
                            x.ProductId == product.Id);

                        if (variant == null)
                        {
                            await transaction.RollbackAsync();
                            return Fail("One of the selected variants could not be found.");
                        }
                    }

                    if (!product.IsActive)
                    {
                        await transaction.RollbackAsync();
                        return Fail($"{product.ProductName} is inactive and cannot be sold.");
                    }

                    if (variant != null && !variant.IsActive)
                    {
                        await transaction.RollbackAsync();
                        return Fail($"{product.ProductName} - {variant.VariantName} is inactive and cannot be sold.");
                    }

                    if (variant != null)
                    {
                        if (!allowNegativeStock && variant.QuantityInStock < item.Quantity)
                        {
                            await transaction.RollbackAsync();
                            return Fail($"Not enough stock for {product.ProductName} - {variant.VariantName}. Available: {variant.QuantityInStock:0.##}");
                        }
                    }
                    else if (product.TrackStock && !allowNegativeStock && product.QuantityInStock < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Fail($"Not enough stock for {product.ProductName}. Available: {product.QuantityInStock:0.##}");
                    }

                    var unitPrice = variant?.SellingPrice ?? product.SellingPrice;
                    var unitCost = variant?.CostPrice ?? product.CostPrice;
                    var lineTotal = item.Quantity * unitPrice;
                    var costTotal = item.Quantity * unitCost;

                    var displayProductName = variant == null
                        ? product.ProductName
                        : $"{product.ProductName} - {variant.VariantName}";

                    _db.SaleItems.Add(new SaleItem
                    {
                        SaleId = sale.Id,
                        ProductId = product.Id,
                        ProductVariantId = variant?.Id,

                        ProductName = displayProductName,
                        SKU = variant?.SKU ?? product.SKU,

                        VariantName = variant?.VariantName,
                        VariantSize = variant?.Size,
                        VariantColor = variant?.Color,
                        VariantSKU = variant?.SKU,
                        VariantBarcode = variant?.Barcode,

                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        LineTotal = lineTotal,

                        UnitCost = unitCost,
                        CostTotal = costTotal,

                        CreatedAt = DateTime.UtcNow
                    });

                    auditItems.Add(new
                    {
                        product.Id,
                        product.ProductName,
                        product.SKU,
                        product.ProductType,
                        product.TrackStock,
                        ProductVariantId = variant?.Id,
                        VariantName = variant?.VariantName,
                        VariantSize = variant?.Size,
                        VariantColor = variant?.Color,
                        VariantSKU = variant?.SKU,
                        VariantBarcode = variant?.Barcode,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        LineTotal = lineTotal,
                        UnitCost = unitCost,
                        CostTotal = costTotal
                    });

                    if (variant != null)
                    {
                        var quantityBefore = variant.QuantityInStock;
                        var quantityAfter = quantityBefore - item.Quantity;

                        variant.QuantityInStock = quantityAfter;
                        variant.UpdatedAt = DateTime.UtcNow;

                        _db.StockMovements.Add(new StockMovement
                        {
                            TenantId = tenantId.Value,
                            BranchId = branchId,
                            ProductId = product.Id,
                            MovementType = "Sale",
                            Quantity = -item.Quantity,
                            QuantityBefore = quantityBefore,
                            QuantityAfter = quantityAfter,
                            ReferenceType = "Sale",
                            ReferenceId = sale.Id,
                            Notes = $"Variant stock deducted for sale {sale.SaleNumber}: {product.ProductName} - {variant.VariantName}",
                            CreatedAt = DateTime.UtcNow
                        });

                        stockAudit.Add(new
                        {
                            product.Id,
                            product.ProductName,
                            ProductVariantId = variant.Id,
                            variant.VariantName,
                            variant.SKU,
                            QuantityBefore = quantityBefore,
                            QuantitySold = item.Quantity,
                            QuantityAfter = quantityAfter
                        });
                    }
                    else if (product.TrackStock)
                    {
                        var quantityBefore = product.QuantityInStock;
                        var quantityAfter = quantityBefore - item.Quantity;

                        product.QuantityInStock = quantityAfter;
                        product.UpdatedAt = DateTime.UtcNow;

                        _db.StockMovements.Add(new StockMovement
                        {
                            TenantId = tenantId.Value,
                            BranchId = branchId,
                            ProductId = product.Id,
                            MovementType = "Sale",
                            Quantity = -item.Quantity,
                            QuantityBefore = quantityBefore,
                            QuantityAfter = quantityAfter,
                            ReferenceType = "Sale",
                            ReferenceId = sale.Id,
                            Notes = $"Stock deducted for sale {sale.SaleNumber}",
                            CreatedAt = DateTime.UtcNow
                        });

                        stockAudit.Add(new
                        {
                            product.Id,
                            product.ProductName,
                            product.SKU,
                            QuantityBefore = quantityBefore,
                            QuantitySold = item.Quantity,
                            QuantityAfter = quantityAfter
                        });
                    }

                    subtotal += lineTotal;
                }

                if (request.DiscountAmount > subtotal)
                {
                    await transaction.RollbackAsync();
                    return Fail("Discount cannot be greater than subtotal.");
                }

                var taxableAmount = subtotal - request.DiscountAmount;
                var taxAmount = taxEnabled
                    ? Math.Round(taxableAmount * (taxRate / 100), 2)
                    : 0;

                var total = taxableAmount + taxAmount;
                var amountPaid = request.AmountPaid <= 0 ? total : request.AmountPaid;

                if (sale.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase) && amountPaid < total)
                {
                    await transaction.RollbackAsync();
                    return Fail("Amount paid cannot be less than the total for a cash sale.");
                }

                sale.Subtotal = subtotal;
                sale.DiscountAmount = request.DiscountAmount;
                sale.TaxAmount = taxAmount;
                sale.TotalAmount = total;
                sale.AmountPaid = amountPaid;
                sale.ChangeAmount = amountPaid - total;
                sale.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                completedSale = sale;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Fail($"Sale failed: {ex.Message}");
            }

            if (completedSale != null)
            {
                await _auditLogService.LogAsync(
                    module: "Sales",
                    action: "Create",
                    entityName: "Sale",
                    entityId: completedSale.Id,
                    summary: $"Completed sale {completedSale.SaleNumber} for {completedSale.TotalAmount:0.00}. Payment: {completedSale.PaymentMethod}.",
                    oldValues: null,
                    newValues: new
                    {
                        completedSale.Id,
                        completedSale.SaleNumber,
                        completedSale.BranchId,
                        completedSale.CashierUserId,
                        completedSale.CashierName,
                        completedSale.CustomerId,
                        completedSale.CustomerName,
                        completedSale.CustomerPhone,
                        completedSale.PaymentMethod,
                        completedSale.Subtotal,
                        completedSale.DiscountAmount,
                        completedSale.TaxAmount,
                        completedSale.TotalAmount,
                        completedSale.AmountPaid,
                        completedSale.ChangeAmount,
                        completedSale.Status,
                        completedSale.Notes,
                        Items = auditItems,
                        StockDeducted = stockAudit
                    });

                return new SaleResult
                {
                    Success = true,
                    Message = $"Sale completed successfully. Receipt: {completedSale.SaleNumber}",
                    SaleId = completedSale.Id,
                    SaleNumber = completedSale.SaleNumber,
                    TotalAmount = completedSale.TotalAmount,
                    ChangeAmount = completedSale.ChangeAmount
                };
            }

            return Fail("Sale could not be completed.");
        }

        public async Task<SaleReceiptViewModel?> GetReceiptAsync(int saleId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var cashierUserId = GetCurrentUserId();

            if (tenantId == null || cashierUserId == null)
            {
                return null;
            }

            var sale = await _db.Sales
                .AsNoTracking()
                .Include(x => x.SaleItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == saleId &&
                    x.TenantId == tenantId.Value &&
                    x.CashierUserId == cashierUserId.Value);

            if (sale == null)
            {
                return null;
            }

            return new SaleReceiptViewModel
            {
                SaleId = sale.Id,
                SaleNumber = sale.SaleNumber,
                CreatedAt = sale.CreatedAt,
                CashierName = sale.CashierName ?? "-",
                CustomerId = sale.CustomerId,
                CustomerName = sale.CustomerName,
                CustomerPhone = sale.CustomerPhone,
                PaymentMethod = sale.PaymentMethod,
                Subtotal = sale.Subtotal,
                DiscountAmount = sale.DiscountAmount,
                TaxAmount = sale.TaxAmount,
                TotalAmount = sale.TotalAmount,
                AmountPaid = sale.AmountPaid,
                ChangeAmount = sale.ChangeAmount,
                Items = sale.SaleItems
                    .OrderBy(x => x.Id)
                    .Select(x => new SaleReceiptItemViewModel
                    {
                        ProductName = x.ProductName,
                        SKU = string.IsNullOrWhiteSpace(x.VariantSKU) ? x.SKU : x.VariantSKU,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        LineTotal = x.LineTotal
                    })
                    .ToList()
            };
        }

        public async Task<List<SaleHistoryRowViewModel>> GetMySalesAsync(DateTime? fromDate, DateTime? toDate, int? branchId = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var cashierUserId = GetCurrentUserId();

            if (tenantId == null || cashierUserId == null)
            {
                return new List<SaleHistoryRowViewModel>();
            }

            var query = _db.Sales
                .AsNoTracking()
                .Include(x => x.SaleItems)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.CashierUserId == cashierUserId.Value &&
                    (!branchId.HasValue || x.BranchId == branchId.Value));

            query = ApplyDateFilter(query, fromDate, toDate);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SaleHistoryRowViewModel
                {
                    SaleId = x.Id,
                    SaleNumber = x.SaleNumber,
                    CreatedAt = x.CreatedAt,
                    PaymentMethod = x.PaymentMethod,
                    ItemCount = x.SaleItems.Count,
                    TotalAmount = x.TotalAmount,
                    AmountPaid = x.AmountPaid,
                    ChangeAmount = x.ChangeAmount,
                    Status = x.Status
                })
                .ToListAsync();
        }

        public async Task<SalesSummaryViewModel> GetMySalesSummaryAsync(DateTime? fromDate, DateTime? toDate, int? branchId = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var cashierUserId = GetCurrentUserId();

            if (tenantId == null || cashierUserId == null)
            {
                return new SalesSummaryViewModel();
            }

            var query = _db.Sales
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.CashierUserId == cashierUserId.Value &&
                    x.Status == "Completed" &&
                    (!branchId.HasValue || x.BranchId == branchId.Value));

            query = ApplyDateFilter(query, fromDate, toDate);

            var sales = await query.ToListAsync();

            return new SalesSummaryViewModel
            {
                TotalTransactions = sales.Count,
                TotalSales = sales.Sum(x => x.TotalAmount),
                TotalDiscounts = sales.Sum(x => x.DiscountAmount),
                TotalTax = sales.Sum(x => x.TaxAmount),
                CashSales = sales
                    .Where(x => x.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalAmount),
                CardSales = sales
                    .Where(x => x.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalAmount),
                EftSales = sales
                    .Where(x => x.PaymentMethod.Equals("EFT", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalAmount)
            };
        }

        private static IQueryable<Sale> ApplyDateFilter(IQueryable<Sale> query, DateTime? fromDate, DateTime? toDate)
        {
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

            return query;
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
                ?? "Cashier";
        }

        private static string GetCustomerDisplayName(Customer customer)
        {
            if (customer.CustomerType == "Business")
            {
                return string.IsNullOrWhiteSpace(customer.BusinessName)
                    ? "Business Customer"
                    : customer.BusinessName.Trim();
            }

            var fullName = $"{customer.FirstName} {customer.LastName}".Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? "Customer"
                : fullName;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static SaleResult Fail(string message)
        {
            return new SaleResult
            {
                Success = false,
                Message = message
            };
        }
    }
}