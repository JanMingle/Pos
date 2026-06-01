using Microsoft.EntityFrameworkCore;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Reports;

namespace PosPlatform.Web.Services
{
    public class ReportsService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public ReportsService(AppDbContext db, TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<ReportsDashboardViewModel> GetDashboardAsync(DateTime? fromDate, DateTime? toDate)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new ReportsDashboardViewModel();
            }

            var from = fromDate?.Date ?? DateTime.Today;
            var toExclusive = (toDate?.Date ?? DateTime.Today).AddDays(1);

            var sales = await _db.Sales
                .AsNoTracking()
                .Include(x => x.SaleItems)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Status == "Completed" &&
                    x.CreatedAt >= from &&
                    x.CreatedAt < toExclusive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var products = await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.TrackStock)
                .Select(x => new
                {
                    x.ProductName,
                    x.SKU,
                    x.QuantityInStock,
                    x.ReorderLevel,
                    x.CostPrice
                })
                .ToListAsync();

            var customers = await _db.Customers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value)
                .Select(x => new
                {
                    x.Id,
                    x.CustomerType,
                    x.FirstName,
                    x.LastName,
                    x.BusinessName
                })
                .ToListAsync();

            var shifts = await _db.CashierShifts
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.OpenedAt >= from &&
                    x.OpenedAt < toExclusive)
                .OrderByDescending(x => x.OpenedAt)
                .Take(20)
                .ToListAsync();

            var allItems = sales
                .SelectMany(s => s.SaleItems)
                .ToList();

            var paymentMethods = sales
                .GroupBy(x => string.IsNullOrWhiteSpace(x.PaymentMethod) ? "Unknown" : x.PaymentMethod)
                .Select(g => new ReportPaymentMethodRow
                {
                    PaymentMethod = g.Key,
                    Transactions = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            var topItems = allItems
                .GroupBy(x => new { x.ProductName, x.SKU })
                .Select(g => new ReportTopItemRow
                {
                    ProductName = g.Key.ProductName,
                    SKU = g.Key.SKU,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalAmount = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(10)
                .ToList();

            var cashiers = sales
                .GroupBy(x => string.IsNullOrWhiteSpace(x.CashierName) ? "Cashier" : x.CashierName)
                .Select(g => new ReportCashierRow
                {
                    CashierName = g.Key,
                    Transactions = g.Count(),
                    TotalSales = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSales)
                .ToList();

            var lowStock = products
                .Where(x => x.QuantityInStock <= x.ReorderLevel)
                .OrderBy(x => x.QuantityInStock)
                .ThenBy(x => x.ProductName)
                .Take(10)
                .Select(x => new ReportLowStockRow
                {
                    ProductName = x.ProductName,
                    SKU = x.SKU,
                    QuantityInStock = x.QuantityInStock,
                    ReorderLevel = x.ReorderLevel,
                    StockValue = x.QuantityInStock * x.CostPrice
                })
                .ToList();

            var customerSales = sales
                .Where(x => x.CustomerId.HasValue)
                .GroupBy(x => x.CustomerId!.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    Purchases = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount),
                    LastPurchaseDate = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToList();

            var topCustomers = customerSales
                .Select(stat =>
                {
                    var customer = customers.FirstOrDefault(x => x.Id == stat.CustomerId);

                    return new ReportCustomerRow
                    {
                        CustomerName = customer == null ? "Customer" : GetCustomerDisplayName(customer.CustomerType, customer.FirstName, customer.LastName, customer.BusinessName),
                        Purchases = stat.Purchases,
                        TotalSpent = stat.TotalSpent,
                        LastPurchaseDate = stat.LastPurchaseDate
                    };
                })
                .ToList();

            var shiftRows = shifts
                .Select(x => new ReportShiftRow
                {
                    CashierName = x.CashierName,
                    OpenedAt = x.OpenedAt,
                    ClosedAt = x.ClosedAt,
                    OpeningCash = x.OpeningCash,
                    CashSales = x.CashSales,
                    ExpectedCash = x.ExpectedCash,
                    ClosingCash = x.ClosingCash,
                    CashDifference = x.CashDifference,
                    Status = x.Status
                })
                .ToList();

            var recentSales = sales
                .Take(12)
                .Select(x => new ReportRecentSaleRow
                {
                    SaleNumber = x.SaleNumber,
                    CreatedAt = x.CreatedAt,
                    CashierName = x.CashierName ?? "Cashier",
                    PaymentMethod = x.PaymentMethod,
                    CustomerName = x.CustomerName,
                    ItemCount = x.SaleItems.Count,
                    TotalAmount = x.TotalAmount
                })
                .ToList();

            var totalTransactions = sales.Count;
            var totalSales = sales.Sum(x => x.TotalAmount);

            return new ReportsDashboardViewModel
            {
                TotalSales = totalSales,
                TotalTransactions = totalTransactions,
                AverageSale = totalTransactions == 0 ? 0 : totalSales / totalTransactions,

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
                    .Sum(x => x.TotalAmount),

                TotalCustomers = customers.Count,
                CustomersWithPurchases = customerSales.Count,

                LowStockItems = products.Count(x => x.QuantityInStock > 0 && x.QuantityInStock <= x.ReorderLevel),
                OutOfStockItems = products.Count(x => x.QuantityInStock <= 0),
                StockValue = products.Sum(x => x.QuantityInStock * x.CostPrice),

                TotalCashDifference = shifts.Sum(x => x.CashDifference),

                PaymentMethods = paymentMethods,
                TopItems = topItems,
                Cashiers = cashiers,
                LowStock = lowStock,
                TopCustomers = topCustomers,
                Shifts = shiftRows,
                RecentSales = recentSales
            };
        }

        private static string GetCustomerDisplayName(
            string customerType,
            string? firstName,
            string? lastName,
            string? businessName)
        {
            if (customerType == "Business")
            {
                return string.IsNullOrWhiteSpace(businessName) ? "Business Customer" : businessName;
            }

            var fullName = $"{firstName} {lastName}".Trim();

            return string.IsNullOrWhiteSpace(fullName) ? "Customer" : fullName;
        }
    }
}