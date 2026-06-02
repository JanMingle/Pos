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
                    x.Status != "Voided" &&
                    x.CreatedAt >= from &&
                    x.CreatedAt < toExclusive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var saleReturns = await _db.SaleReturns
                .AsNoTracking()
                .Include(x => x.SaleReturnItems)
                .Include(x => x.Sale)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Status == "Completed" &&
                    x.CreatedAt >= from &&
                    x.CreatedAt < toExclusive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var purchases = await _db.StockPurchases
    .AsNoTracking()
    .Include(x => x.Supplier)
    .Where(x =>
        x.TenantId == tenantId.Value &&
        x.PurchaseDate >= from &&
        x.PurchaseDate < toExclusive)
    .OrderByDescending(x => x.PurchaseDate)
    .ToListAsync();

            var expenses = await _db.Expenses
    .AsNoTracking()
    .Include(x => x.ExpenseCategory)
    .Where(x =>
        x.TenantId == tenantId.Value &&
        x.Status == "Recorded" &&
        x.ExpenseDate >= from &&
        x.ExpenseDate < toExclusive)
    .OrderByDescending(x => x.ExpenseDate)
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

            var allReturnItems = saleReturns
                .SelectMany(r => r.SaleReturnItems)
                .ToList();

            var grossSales = sales.Sum(x => x.TotalAmount);
            var totalRefunds = saleReturns.Sum(x => x.TotalRefundAmount);
            var netSales = grossSales - totalRefunds;

            var costOfGoodsSold = sales
    .SelectMany(x => x.SaleItems)
    .Sum(x => x.CostTotal);

            var refundedCost = saleReturns
                .SelectMany(x => x.SaleReturnItems)
                .Sum(x => x.CostTotal);

            var netCostOfGoodsSold = costOfGoodsSold - refundedCost;

            var grossProfit = netSales - netCostOfGoodsSold;

            var grossProfitMargin = netSales <= 0
                ? 0
                : Math.Round((grossProfit / netSales) * 100, 2);

            var totalExpenses = expenses.Sum(x => x.TotalAmount);
            var expenseTax = expenses.Sum(x => x.TaxAmount);
            var expenseCount = expenses.Count;

            var netProfit = grossProfit - totalExpenses;

            var netProfitMargin = netSales <= 0
                ? 0
                : Math.Round((netProfit / netSales) * 100, 2);

            var expensesByCategory = expenses
                .GroupBy(x => x.ExpenseCategory != null ? x.ExpenseCategory.CategoryName : "Uncategorised")
                .Select(g => new ReportExpenseCategoryRow
                {
                    CategoryName = g.Key,
                    ExpenseCount = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(10)
                .ToList();

            var recentExpenses = expenses
                .Take(12)
                .Select(x => new ReportRecentExpenseRow
                {
                    ExpenseNumber = x.ExpenseNumber,
                    ExpenseDate = x.ExpenseDate,
                    CategoryName = x.ExpenseCategory != null ? x.ExpenseCategory.CategoryName : "-",
                    VendorName = x.VendorName,
                    PaymentMethod = x.PaymentMethod,
                    Subtotal = x.Subtotal,
                    TaxAmount = x.TaxAmount,
                    TotalAmount = x.TotalAmount,
                    CreatedByName = x.CreatedByName
                })
                .ToList();

            var purchaseCount = purchases.Count;
            var purchaseSubtotal = purchases.Sum(x => x.Subtotal);
            var purchaseTax = purchases.Sum(x => x.TaxAmount);
            var purchaseTotal = purchases.Sum(x => x.TotalAmount);

            var topSuppliers = purchases
                .GroupBy(x => x.Supplier != null ? x.Supplier.SupplierName : "Supplier")
                .Select(g => new ReportSupplierPurchaseRow
                {
                    SupplierName = g.Key,
                    Purchases = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToList();

            var salesByPayment = sales
                .GroupBy(x => string.IsNullOrWhiteSpace(x.PaymentMethod) ? "Unknown" : x.PaymentMethod)
                .Select(g => new
                {
                    PaymentMethod = g.Key,
                    Transactions = g.Count(),
                    Gross = g.Sum(x => x.TotalAmount)
                })
                .ToList();

            var refundsByMethod = saleReturns
                .GroupBy(x => string.IsNullOrWhiteSpace(x.RefundMethod) ? "Unknown" : x.RefundMethod)
                .Select(g => new
                {
                    PaymentMethod = g.Key,
                    Refunds = g.Count(),
                    Refunded = g.Sum(x => x.TotalRefundAmount)
                })
                .ToList();

            var paymentMethodNames = salesByPayment
                .Select(x => x.PaymentMethod)
                .Union(refundsByMethod.Select(x => x.PaymentMethod))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var paymentMethods = paymentMethodNames
                .Select(method =>
                {
                    var saleData = salesByPayment.FirstOrDefault(x => x.PaymentMethod == method);
                    var refundData = refundsByMethod.FirstOrDefault(x => x.PaymentMethod == method);

                    var gross = saleData?.Gross ?? 0;
                    var refunded = refundData?.Refunded ?? 0;

                    return new ReportPaymentMethodRow
                    {
                        PaymentMethod = method,
                        Transactions = saleData?.Transactions ?? 0,
                        Refunds = refundData?.Refunds ?? 0,
                        TotalAmount = gross,
                        RefundAmount = refunded,
                        NetAmount = gross - refunded
                    };
                })
                .OrderByDescending(x => x.NetAmount)
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

            var topRefundedItems = allReturnItems
                .GroupBy(x => new { x.ProductName, x.SKU })
                .Select(g => new ReportRefundedItemRow
                {
                    ProductName = g.Key.ProductName,
                    SKU = g.Key.SKU,
                    QuantityReturned = g.Sum(x => x.Quantity),
                    RefundAmount = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.RefundAmount)
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

            var refundCashiers = saleReturns
                .GroupBy(x => string.IsNullOrWhiteSpace(x.ReturnedByName) ? "User" : x.ReturnedByName)
                .Select(g => new ReportRefundCashierRow
                {
                    CashierName = g.Key,
                    Refunds = g.Count(),
                    TotalRefunded = g.Sum(x => x.TotalRefundAmount)
                })
                .OrderByDescending(x => x.TotalRefunded)
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

            var customerGross = sales
                .Where(x => x.CustomerId.HasValue)
                .GroupBy(x => x.CustomerId!.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    Purchases = g.Count(),
                    GrossSpent = g.Sum(x => x.TotalAmount),
                    LastPurchaseDate = g.Max(x => x.CreatedAt)
                })
                .ToList();

            var customerRefunds = saleReturns
                .Where(x => x.Sale != null && x.Sale.CustomerId.HasValue)
                .GroupBy(x => x.Sale!.CustomerId!.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    Refunded = g.Sum(x => x.TotalRefundAmount)
                })
                .ToList();

            var topCustomers = customerGross
                .Select(stat =>
                {
                    var customer = customers.FirstOrDefault(x => x.Id == stat.CustomerId);
                    var refunded = customerRefunds.FirstOrDefault(x => x.CustomerId == stat.CustomerId)?.Refunded ?? 0;

                    return new ReportCustomerRow
                    {
                        CustomerName = customer == null
                            ? "Customer"
                            : GetCustomerDisplayName(customer.CustomerType, customer.FirstName, customer.LastName, customer.BusinessName),

                        Purchases = stat.Purchases,
                        GrossSpent = stat.GrossSpent,
                        Refunded = refunded,
                        TotalSpent = stat.GrossSpent - refunded,
                        LastPurchaseDate = stat.LastPurchaseDate
                    };
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
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
                    TotalAmount = x.TotalAmount,
                    Status = x.Status
                })
                .ToList();

            var recentRefunds = saleReturns
                .Take(12)
                .Select(x => new ReportRecentRefundRow
                {
                    ReturnNumber = x.ReturnNumber,
                    SaleNumber = x.Sale?.SaleNumber ?? "-",
                    CreatedAt = x.CreatedAt,
                    ReturnType = x.ReturnType,
                    RefundMethod = x.RefundMethod,
                    ReturnedByName = x.ReturnedByName,
                    TotalRefundAmount = x.TotalRefundAmount,
                    Status = x.Status
                })
                .ToList();

            var totalTransactions = sales.Count;

            return new ReportsDashboardViewModel
            {
                GrossSales = grossSales,
                TotalRefunds = totalRefunds,
                NetSales = netSales,
                TotalSales = netSales,

                TotalTransactions = totalTransactions,
                AverageSale = totalTransactions == 0 ? 0 : grossSales / totalTransactions,

                RefundCount = saleReturns.Count,
                VoidCount = saleReturns.Count(x => x.ReturnType == "Void"),

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
                CustomersWithPurchases = topCustomers.Count,

                LowStockItems = products.Count(x => x.QuantityInStock > 0 && x.QuantityInStock <= x.ReorderLevel),
                OutOfStockItems = products.Count(x => x.QuantityInStock <= 0),
                StockValue = products.Sum(x => x.QuantityInStock * x.CostPrice),

                TotalCashDifference = shifts.Sum(x => x.CashDifference),

                PaymentMethods = paymentMethods,
                TopItems = topItems,
                TopRefundedItems = topRefundedItems,
                Cashiers = cashiers,
                RefundCashiers = refundCashiers,
                LowStock = lowStock,
                TopCustomers = topCustomers,
                Shifts = shiftRows,
                RecentSales = recentSales,
                RecentRefunds = recentRefunds,

                CostOfGoodsSold = costOfGoodsSold,
                RefundedCost = refundedCost,
                NetCostOfGoodsSold = netCostOfGoodsSold,
                GrossProfit = grossProfit,
                GrossProfitMargin = grossProfitMargin,

                PurchaseCount = purchaseCount,
                PurchaseSubtotal = purchaseSubtotal,
                PurchaseTax = purchaseTax,
                PurchaseTotal = purchaseTotal,
                TopSuppliers = topSuppliers,

                TotalExpenses = totalExpenses,
                ExpenseTax = expenseTax,
                ExpenseCount = expenseCount,
                NetProfit = netProfit,
                NetProfitMargin = netProfitMargin,

                ExpensesByCategory = expensesByCategory,
                RecentExpenses = recentExpenses
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