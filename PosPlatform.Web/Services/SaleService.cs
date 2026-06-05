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

        public SaleService(
            AppDbContext db,
            TenantContextService tenantContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<SaleProductOptionViewModel>> SearchProductsAsync(string? search = null)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var currentBranchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return new List<SaleProductOptionViewModel>();
            }

            var query = _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    (!currentBranchId.HasValue || x.BranchId == null || x.BranchId == currentBranchId.Value));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.ProductName.Contains(term) ||
                    x.SKU.Contains(term) ||
                    x.ProductType.Contains(term) ||
                    (x.Barcode != null && x.Barcode.Contains(term)));
            }

            return await query
                .OrderBy(x => x.ProductName)
                .Take(30)
                .Select(x => new SaleProductOptionViewModel
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

                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        
        }

     
        public async Task<SaleProductOptionViewModel?> GetProductByBarcodeAsync(string? barcodeOrSku)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var currentBranchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null || string.IsNullOrWhiteSpace(barcodeOrSku))
            {
                return null;
            }

            var code = barcodeOrSku.Trim();

            return await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    (
                        x.SKU == code ||
                        (x.Barcode != null && x.Barcode == code)
                    ) &&
                    (!currentBranchId.HasValue || x.BranchId == null || x.BranchId == currentBranchId.Value))
                .OrderByDescending(x => currentBranchId.HasValue && x.BranchId == currentBranchId.Value)
                .ThenBy(x => x.ProductName)
                .Select(x => new SaleProductOptionViewModel
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

            if (requireCustomerForSale && string.IsNullOrWhiteSpace(request.CustomerName))
            {
                return Fail("Customer name is required for this business.");
            }

            var groupedItems = request.Items
                .GroupBy(x => x.ProductId)
                .Select(x => new CreateSaleItemRequest
                {
                    ProductId = x.Key,
                    Quantity = x.Sum(i => i.Quantity)
                })
                .ToList();

            var productIds = groupedItems.Select(x => x.ProductId).ToList();

            var productsForValidation = await _db.Products
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && productIds.Contains(x.Id))
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

                if (product.TrackStock && !allowNegativeStock && product.QuantityInStock < item.Quantity)
                {
                    return Fail($"Not enough stock for {product.ProductName}. Available: {product.QuantityInStock:0.##}");
                }
            }

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

                    if (!product.IsActive)
                    {
                        await transaction.RollbackAsync();
                        return Fail($"{product.ProductName} is inactive and cannot be sold.");
                    }

                    if (product.TrackStock && !allowNegativeStock && product.QuantityInStock < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return Fail($"Not enough stock for {product.ProductName}. Available: {product.QuantityInStock:0.##}");
                    }

                    var unitPrice = product.SellingPrice;
                    var lineTotal = item.Quantity * unitPrice;

                    var unitCost = product.CostPrice;
                    var costTotal = item.Quantity * unitCost;

                    _db.SaleItems.Add(new SaleItem
                    {
                        SaleId = sale.Id,
                        ProductId = product.Id,
                        ProductName = product.ProductName,
                        SKU = product.SKU,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        LineTotal = lineTotal,

                        UnitCost = unitCost,
                        CostTotal = costTotal,

                        CreatedAt = DateTime.UtcNow
                    });

                    if (product.TrackStock)
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

                return new SaleResult
                {
                    Success = true,
                    Message = $"Sale completed successfully. Receipt: {sale.SaleNumber}",
                    SaleId = sale.Id,
                    SaleNumber = sale.SaleNumber,
                    TotalAmount = sale.TotalAmount,
                    ChangeAmount = sale.ChangeAmount
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Fail($"Sale failed: {ex.Message}");
            }
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
                        SKU = x.SKU,
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