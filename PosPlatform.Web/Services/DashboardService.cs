using Microsoft.EntityFrameworkCore;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Dashboard;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DashboardService(
            AppDbContext db,
            TenantContextService tenantContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _tenantContext = tenantContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DashboardViewModel> GetDashboardAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new DashboardViewModel();
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var sales = await _db.Sales
                .AsNoTracking()
                .Include(x => x.SaleItems)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Status != "Voided" &&
                    x.CreatedAt >= today &&
                    x.CreatedAt < tomorrow)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var refunds = await _db.SaleReturns
                .AsNoTracking()
                .Include(x => x.SaleReturnItems)
                .Include(x => x.Sale)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Status == "Completed" &&
                    x.CreatedAt >= today &&
                    x.CreatedAt < tomorrow)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var expenses = await _db.Expenses
                .AsNoTracking()
                .Include(x => x.ExpenseCategory)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Status == "Recorded" &&
                    x.ExpenseDate >= today &&
                    x.ExpenseDate < tomorrow)
                .OrderByDescending(x => x.ExpenseDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var lowStockProducts = await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    x.TrackStock &&
                    x.QuantityInStock <= x.ReorderLevel)
                .OrderBy(x => x.QuantityInStock)
                .ThenBy(x => x.ProductName)
                .Take(8)
                .Select(x => new DashboardLowStockRow
                {
                    ProductName = x.ProductName,
                    SKU = x.SKU,
                    QuantityInStock = x.QuantityInStock,
                    ReorderLevel = x.ReorderLevel
                })
                .ToListAsync();

            var allStockProducts = await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    x.TrackStock)
                .Select(x => new
                {
                    x.QuantityInStock,
                    x.ReorderLevel
                })
                .ToListAsync();

            var grossSales = sales.Sum(x => x.TotalAmount);
            var totalRefunds = refunds.Sum(x => x.TotalRefundAmount);
            var netSales = grossSales - totalRefunds;

            var soldCost = sales
                .SelectMany(x => x.SaleItems)
                .Sum(x => x.CostTotal);

            var refundedCost = refunds
                .SelectMany(x => x.SaleReturnItems)
                .Sum(x => x.CostTotal);

            var netCost = soldCost - refundedCost;
            var grossProfit = netSales - netCost;

            var totalExpenses = expenses.Sum(x => x.TotalAmount);
            var netProfit = grossProfit - totalExpenses;

            return new DashboardViewModel
            {
                TodayGrossSales = grossSales,
                TodayRefunds = totalRefunds,
                TodayNetSales = netSales,

                TodayCostOfGoods = netCost,
                TodayGrossProfit = grossProfit,

                TodayExpenses = totalExpenses,
                TodayNetProfit = netProfit,

                TodayTransactionCount = sales.Count,
                TodayRefundCount = refunds.Count,
                TodayExpenseCount = expenses.Count,

                LowStockCount = allStockProducts.Count(x => x.QuantityInStock > 0 && x.QuantityInStock <= x.ReorderLevel),
                OutOfStockCount = allStockProducts.Count(x => x.QuantityInStock <= 0),

                OpenShift = await GetOpenShiftAsync(tenantId.Value),

                RecentSales = sales
                    .Take(6)
                    .Select(x => new DashboardRecentSaleRow
                    {
                        SaleId = x.Id,
                        SaleNumber = x.SaleNumber,
                        CreatedAt = x.CreatedAt,
                        PaymentMethod = x.PaymentMethod,
                        CustomerName = x.CustomerName,
                        TotalAmount = x.TotalAmount,
                        Status = x.Status
                    })
                    .ToList(),

                RecentRefunds = refunds
                    .Take(5)
                    .Select(x => new DashboardRecentRefundRow
                    {
                        ReturnNumber = x.ReturnNumber,
                        SaleNumber = x.Sale?.SaleNumber ?? "-",
                        CreatedAt = x.CreatedAt,
                        ReturnType = x.ReturnType,
                        TotalRefundAmount = x.TotalRefundAmount
                    })
                    .ToList(),

                RecentExpenses = expenses
                    .Take(5)
                    .Select(x => new DashboardRecentExpenseRow
                    {
                        ExpenseNumber = x.ExpenseNumber,
                        ExpenseDate = x.ExpenseDate,
                        CategoryName = x.ExpenseCategory != null ? x.ExpenseCategory.CategoryName : "-",
                        VendorName = x.VendorName,
                        TotalAmount = x.TotalAmount
                    })
                    .ToList(),

                LowStockItems = lowStockProducts
            };
        }

        private async Task<DashboardShiftViewModel?> GetOpenShiftAsync(int tenantId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return null;
            }

            var shift = await _db.CashierShifts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.CashierUserId == userId.Value &&
                    x.Status == "Open");

            if (shift == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;

            var sales = await _db.Sales
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.CashierUserId == userId.Value &&
                    x.Status != "Voided" &&
                    x.CreatedAt >= shift.OpenedAt &&
                    x.CreatedAt <= now)
                .Select(x => new
                {
                    x.PaymentMethod,
                    x.TotalAmount
                })
                .ToListAsync();

            var cashSales = sales
                .Where(x => x.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.TotalAmount);

            var cardSales = sales
                .Where(x => x.PaymentMethod.Equals("Card", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.TotalAmount);

            var eftSales = sales
                .Where(x => x.PaymentMethod.Equals("EFT", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.TotalAmount);

            return new DashboardShiftViewModel
            {
                Id = shift.Id,
                OpenedAt = shift.OpenedAt,
                OpeningCash = shift.OpeningCash,
                CashSales = cashSales,
                CardSales = cardSales,
                EftSales = eftSales,
                TotalSales = sales.Sum(x => x.TotalAmount),
                ExpectedCash = shift.OpeningCash + cashSales
            };
        }

        private int? GetCurrentUserId()
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}