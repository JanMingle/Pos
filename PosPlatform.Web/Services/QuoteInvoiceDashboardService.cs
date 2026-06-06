using Microsoft.EntityFrameworkCore;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Dashboard;

namespace PosPlatform.Web.Services
{
    public class QuoteInvoiceDashboardService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TenantContextService _tenantContext;

        public QuoteInvoiceDashboardService(
            IServiceScopeFactory scopeFactory,
            TenantContextService tenantContext)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
        }

        public async Task<QuoteInvoiceDashboardViewModel> GetDashboardAsync()
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            var today = DateTime.Today;
            var fromDate = today.AddDays(-30);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            if (tenantId == null)
            {
                return new QuoteInvoiceDashboardViewModel
                {
                    PeriodFrom = fromDate,
                    PeriodTo = today
                };
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var settings = await db.BusinessSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            var currencySymbol = string.IsNullOrWhiteSpace(settings?.CurrencySymbol)
                ? "R"
                : settings.CurrencySymbol;

            var quotesQuery = db.Quotes
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.QuoteDate >= fromDate &&
                    x.QuoteDate < today.AddDays(1) &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value));

            var invoicesQuery = db.Invoices
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.InvoiceDate >= fromDate &&
                    x.InvoiceDate < today.AddDays(1) &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value));

            var allOutstandingInvoicesQuery = db.Invoices
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.BalanceDue > 0 &&
                    x.Status != "Paid" &&
                    x.Status != "Cancelled" &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value));

            var overdueInvoicesQuery = allOutstandingInvoicesQuery
                .Where(x =>
                    x.DueDate != null &&
                    x.DueDate.Value.Date < today);

            var paymentsThisMonthQuery = db.InvoicePayments
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.PaymentDate >= monthStart &&
                    x.PaymentDate < today.AddDays(1) &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value));

            return new QuoteInvoiceDashboardViewModel
            {
                CurrencySymbol = currencySymbol,
                PeriodFrom = fromDate,
                PeriodTo = today,

                QuoteCount = await quotesQuery.CountAsync(),
                AcceptedQuoteCount = await quotesQuery.CountAsync(x => x.Status == "Accepted"),
                QuoteValue = await quotesQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0,

                InvoiceCount = await invoicesQuery.CountAsync(),
                InvoiceValue = await invoicesQuery.SumAsync(x => (decimal?)x.TotalAmount) ?? 0,

                UnpaidInvoiceCount = await allOutstandingInvoicesQuery.CountAsync(x => x.Status == "Unpaid"),
                PartiallyPaidInvoiceCount = await allOutstandingInvoicesQuery.CountAsync(x => x.Status == "Partially Paid"),
                PaidInvoiceCount = await invoicesQuery.CountAsync(x => x.Status == "Paid"),

                OutstandingBalance = await allOutstandingInvoicesQuery.SumAsync(x => (decimal?)x.BalanceDue) ?? 0,

                OverdueInvoiceCount = await overdueInvoicesQuery.CountAsync(),
                OverdueBalance = await overdueInvoicesQuery.SumAsync(x => (decimal?)x.BalanceDue) ?? 0,

                PaymentsReceivedThisMonth = await paymentsThisMonthQuery.SumAsync(x => (decimal?)x.Amount) ?? 0
            };
        }
    }
}