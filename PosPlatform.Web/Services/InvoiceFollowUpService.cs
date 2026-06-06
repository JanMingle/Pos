using Microsoft.EntityFrameworkCore;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Invoices;

namespace PosPlatform.Web.Services
{
    public class InvoiceFollowUpService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly AuditLogService _auditLogService;

        public InvoiceFollowUpService(
            AppDbContext db,
            TenantContextService tenantContext,
            AuditLogService auditLogService)
        {
            _db = db;
            _tenantContext = tenantContext;
            _auditLogService = auditLogService;
        }

        public async Task<List<InvoiceFollowUpItemViewModel>> GetFollowUpsAsync(
            string? search,
            string? followUpStatus,
            string? reminderFilter)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<InvoiceFollowUpItemViewModel>();
            }

            var today = DateTime.Today;

            var query = _db.Invoices
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.BalanceDue > 0 &&
                    x.Status != "Paid" &&
                    x.Status != "Cancelled");

            if (!string.IsNullOrWhiteSpace(followUpStatus) && followUpStatus != "all")
            {
                query = query.Where(x => x.FollowUpStatus == followUpStatus);
            }

            if (!string.IsNullOrWhiteSpace(reminderFilter) && reminderFilter != "all")
            {
                if (reminderFilter == "due-today")
                {
                    query = query.Where(x =>
                        x.NextFollowUpDate != null &&
                        x.NextFollowUpDate.Value.Date == today);
                }
                else if (reminderFilter == "overdue-follow-up")
                {
                    query = query.Where(x =>
                        x.NextFollowUpDate != null &&
                        x.NextFollowUpDate.Value.Date < today);
                }
                else if (reminderFilter == "no-follow-up-date")
                {
                    query = query.Where(x => x.NextFollowUpDate == null);
                }
                else if (reminderFilter == "overdue-invoice")
                {
                    query = query.Where(x =>
                        x.DueDate != null &&
                        x.DueDate.Value.Date < today);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.InvoiceNumber.Contains(term) ||
                    (x.CustomerName != null && x.CustomerName.Contains(term)) ||
                    (x.CustomerPhone != null && x.CustomerPhone.Contains(term)) ||
                    (x.CustomerEmail != null && x.CustomerEmail.Contains(term)));
            }

            var raw = await query
                .OrderBy(x => x.NextFollowUpDate == null)
                .ThenBy(x => x.NextFollowUpDate)
                .ThenBy(x => x.DueDate)
                .ThenByDescending(x => x.BalanceDue)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNumber,
                    x.InvoiceDate,
                    x.DueDate,
                    x.CustomerName,
                    x.CustomerPhone,
                    x.CustomerEmail,
                    x.TotalAmount,
                    x.AmountPaid,
                    x.BalanceDue,
                    x.Status,
                    x.FollowUpStatus,
                    x.LastFollowUpAt,
                    x.NextFollowUpDate,
                    x.FollowUpNotes,
                    x.FollowUpCount
                })
                .ToListAsync();

            return raw.Select(x => new InvoiceFollowUpItemViewModel
            {
                Id = x.Id,
                InvoiceNumber = x.InvoiceNumber,
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
                FollowUpStatus = string.IsNullOrWhiteSpace(x.FollowUpStatus)
                        ? "Not Started"
                        : x.FollowUpStatus,
                LastFollowUpAt = x.LastFollowUpAt,
                NextFollowUpDate = x.NextFollowUpDate,
                FollowUpNotes = x.FollowUpNotes,
                FollowUpCount = x.FollowUpCount
            })
                .ToList();
        }

        public InvoiceFollowUpSummaryViewModel BuildSummary(List<InvoiceFollowUpItemViewModel> items)
        {
            var today = DateTime.Today;

            return new InvoiceFollowUpSummaryViewModel
            {
                TotalOutstanding = items.Count,
                OutstandingBalance = items.Sum(x => x.BalanceDue),
                DueForFollowUp = items.Count(x =>
                    x.NextFollowUpDate.HasValue &&
                    x.NextFollowUpDate.Value.Date <= today),
                PromisedToPay = items.Count(x => x.FollowUpStatus == "Promised to Pay"),
                Escalated = items.Count(x => x.FollowUpStatus == "Escalated")
            };
        }

        public async Task<(bool Success, string Message)> UpdateFollowUpAsync(UpdateInvoiceFollowUpModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            if (model.InvoiceId <= 0)
            {
                return (false, "Invoice is required.");
            }

            if (string.IsNullOrWhiteSpace(model.FollowUpStatus))
            {
                return (false, "Follow-up status is required.");
            }

            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(x =>
                    x.Id == model.InvoiceId &&
                    x.TenantId == tenantId.Value);

            if (invoice == null)
            {
                return (false, "Invoice not found.");
            }

            if (invoice.Status == "Paid" || invoice.Status == "Cancelled")
            {
                return (false, "Cannot update follow-up for a paid or cancelled invoice.");
            }

            var oldValues = new
            {
                invoice.InvoiceNumber,
                invoice.FollowUpStatus,
                invoice.LastFollowUpAt,
                invoice.NextFollowUpDate,
                invoice.FollowUpNotes,
                invoice.FollowUpCount
            };

            invoice.FollowUpStatus = model.FollowUpStatus.Trim();
            invoice.LastFollowUpAt = DateTime.UtcNow;
            invoice.NextFollowUpDate = model.NextFollowUpDate?.Date;
            invoice.FollowUpNotes = Clean(model.FollowUpNotes);
            invoice.FollowUpCount += 1;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Invoices",
                action: "Follow Up",
                entityName: "Invoice",
                entityId: invoice.Id,
                summary: $"Updated follow-up for invoice {invoice.InvoiceNumber}. Status: {invoice.FollowUpStatus}.",
                oldValues: oldValues,
                newValues: new
                {
                    invoice.InvoiceNumber,
                    invoice.FollowUpStatus,
                    invoice.LastFollowUpAt,
                    invoice.NextFollowUpDate,
                    invoice.FollowUpNotes,
                    invoice.FollowUpCount
                });

            return (true, "Invoice follow-up updated successfully.");
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}