using Microsoft.EntityFrameworkCore;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Invoices;

namespace PosPlatform.Web.Services
{
    public class OverdueInvoiceService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public OverdueInvoiceService(
            AppDbContext db,
            TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<List<OverdueInvoiceViewModel>> GetOverdueInvoicesAsync(
            string? search,
            string? statusFilter,
            int minimumDaysOverdue)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<OverdueInvoiceViewModel>();
            }

            var today = DateTime.Today;

            var query = _db.Invoices
                .AsNoTracking()
                .Include(x => x.Quote)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.BalanceDue > 0 &&
                    x.Status != "Paid" &&
                    x.Status != "Cancelled");

            // If minimumDaysOverdue is 0, show all outstanding invoices.
            // If it is 1 or higher, show only truly overdue invoices.
            if (minimumDaysOverdue > 0)
            {
                query = query.Where(x =>
                    x.DueDate != null &&
                    x.DueDate.Value.Date < today);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
            {
                query = query.Where(x => x.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.InvoiceNumber.Contains(term) ||
                    (x.Quote != null && x.Quote.QuoteNumber.Contains(term)) ||
                    (x.CustomerName != null && x.CustomerName.Contains(term)) ||
                    (x.CustomerPhone != null && x.CustomerPhone.Contains(term)) ||
                    (x.CustomerEmail != null && x.CustomerEmail.Contains(term)));
            }

            var raw = await query
                .OrderBy(x => x.DueDate)
                .ThenByDescending(x => x.BalanceDue)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNumber,
                    QuoteNumber = x.Quote != null ? x.Quote.QuoteNumber : null,
                    x.InvoiceDate,
                    x.DueDate,
                    x.CustomerName,
                    x.CustomerPhone,
                    x.CustomerEmail,
                    x.TotalAmount,
                    x.AmountPaid,
                    x.BalanceDue,
                    x.Status,
                    x.CreatedByName
                })
                .ToListAsync();

            return raw
                .Select(x => new OverdueInvoiceViewModel
                {
                    Id = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    QuoteNumber = x.QuoteNumber,
                    InvoiceDate = x.InvoiceDate,
                    DueDate = x.DueDate,
                    DaysOverdue = x.DueDate.HasValue && x.DueDate.Value.Date < today
                        ? (today - x.DueDate.Value.Date).Days
                        : 0,
                    CustomerName = string.IsNullOrWhiteSpace(x.CustomerName)
                        ? "Walk-in / Unsaved"
                        : x.CustomerName!,
                    CustomerPhone = x.CustomerPhone,
                    CustomerEmail = x.CustomerEmail,
                    TotalAmount = x.TotalAmount,
                    AmountPaid = x.AmountPaid,
                    BalanceDue = x.BalanceDue,
                    Status = x.Status,
                    CreatedByName = x.CreatedByName
                })
                .Where(x => minimumDaysOverdue == 0 || x.DaysOverdue >= minimumDaysOverdue)
                .OrderByDescending(x => x.DaysOverdue)
                .ThenByDescending(x => x.BalanceDue)
                .ToList();
        }

        public OverdueInvoiceSummaryViewModel BuildSummary(List<OverdueInvoiceViewModel> invoices)
        {
            return new OverdueInvoiceSummaryViewModel
            {
                Count = invoices.Count,
                TotalBalanceDue = invoices.Sum(x => x.BalanceDue),
                TotalInvoiceValue = invoices.Sum(x => x.TotalAmount),
                TotalPaid = invoices.Sum(x => x.AmountPaid),
                MostOverdueDays = invoices.Count == 0 ? 0 : invoices.Max(x => x.DaysOverdue)
            };
        }
    }
}