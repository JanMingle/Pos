using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Invoices;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogService _auditLogService;

        public InvoiceService(
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

        public async Task<List<InvoiceListItemViewModel>> GetInvoicesAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string? statusFilter,
            string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<InvoiceListItemViewModel>();
            }

            var query = _db.Invoices
                .AsNoTracking()
                .Include(x => x.Quote)
                .Include(x => x.InvoiceItems)
                .Where(x => x.TenantId == tenantId.Value);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.InvoiceDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.InvoiceDate < to);
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

            return await query
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Id)
                .Take(150)
                .Select(x => new InvoiceListItemViewModel
                {
                    Id = x.Id,
                    QuoteId = x.QuoteId,
                    InvoiceNumber = x.InvoiceNumber,
                    QuoteNumber = x.Quote != null ? x.Quote.QuoteNumber : null,
                    InvoiceDate = x.InvoiceDate,
                    DueDate = x.DueDate,
                    CustomerName = string.IsNullOrWhiteSpace(x.CustomerName) ? "Walk-in / Unsaved" : x.CustomerName!,
                    ItemCount = x.InvoiceItems.Count,
                    TotalAmount = x.TotalAmount,
                    AmountPaid = x.AmountPaid,
                    BalanceDue = x.BalanceDue,
                    Status = x.Status,
                    CreatedByName = x.CreatedByName
                })
                .ToListAsync();
        }

        public async Task<InvoiceDetailsViewModel?> GetInvoiceDetailsAsync(int invoiceId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            return await _db.Invoices
                .AsNoTracking()
                .Include(x => x.Quote)
                .Include(x => x.InvoiceItems)
                .Include(x => x.Payments)
                .Where(x => x.Id == invoiceId && x.TenantId == tenantId.Value)
                .Select(x => new InvoiceDetailsViewModel
                {
                    Id = x.Id,
                    QuoteId = x.QuoteId,
                    InvoiceNumber = x.InvoiceNumber,
                    QuoteNumber = x.Quote != null ? x.Quote.QuoteNumber : null,
                    InvoiceDate = x.InvoiceDate,
                    DueDate = x.DueDate,
                    CustomerName = x.CustomerName,
                    CustomerPhone = x.CustomerPhone,
                    CustomerEmail = x.CustomerEmail,
                    Subtotal = x.Subtotal,
                    DiscountAmount = x.DiscountAmount,
                    TaxAmount = x.TaxAmount,
                    TotalAmount = x.TotalAmount,
                    AmountPaid = x.AmountPaid,
                    BalanceDue = x.BalanceDue,
                    Status = x.Status,
                    Notes = x.Notes,
                    Terms = x.Terms,
                    CreatedByName = x.CreatedByName,
                    CreatedAt = x.CreatedAt,
                    Items = x.InvoiceItems
                        .OrderBy(i => i.Id)
                        .Select(i => new InvoiceItemViewModel
                        {
                            ProductName = i.ProductName,
                            SKU = i.SKU,
                            ProductType = i.ProductType,
                            UnitOfMeasure = i.UnitOfMeasure,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            LineTotal = i.LineTotal
                        })
                        .ToList(),
                    Payments = x.Payments
                        .OrderByDescending(p => p.PaymentDate)
                        .ThenByDescending(p => p.Id)
                        .Select(p => new InvoicePaymentViewModel
                        {
                            Id = p.Id,
                            PaymentDate = p.PaymentDate,
                            Amount = p.Amount,
                            PaymentMethod = p.PaymentMethod,
                            ReferenceNumber = p.ReferenceNumber,
                            Notes = p.Notes,
                            ReceivedByName = p.ReceivedByName,
                            CreatedAt = p.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Success, string Message, int? InvoiceId)> ConvertQuoteToInvoiceAsync(int quoteId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserDisplayName();

            if (tenantId == null)
            {
                return (false, "Tenant not found.", null);
            }

            if (userId == null)
            {
                return (false, "Logged-in user could not be identified.", null);
            }

            var existingInvoice = await _db.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId.Value &&
                    x.QuoteId == quoteId);

            if (existingInvoice != null)
            {
                return (false, $"This quote has already been converted to invoice {existingInvoice.InvoiceNumber}.", existingInvoice.Id);
            }

            var quote = await _db.Quotes
                .Include(x => x.QuoteItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == quoteId &&
                    x.TenantId == tenantId.Value);

            if (quote == null)
            {
                return (false, "Quote not found.", null);
            }

            if (quote.Status != "Accepted")
            {
                return (false, "Only accepted quotes can be converted to invoices.", null);
            }

            if (quote.QuoteItems.Count == 0)
            {
                return (false, "This quote has no items.", null);
            }

            Invoice? completedInvoice = null;
            var auditItems = new List<object>();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var invoice = new Invoice
                {
                    TenantId = quote.TenantId,
                    BranchId = quote.BranchId,
                    QuoteId = quote.Id,
                    CustomerId = quote.CustomerId,

                    InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    InvoiceDate = DateTime.UtcNow.Date,
                    DueDate = DateTime.UtcNow.Date.AddDays(7),

                    CustomerName = quote.CustomerName,
                    CustomerPhone = quote.CustomerPhone,
                    CustomerEmail = quote.CustomerEmail,

                    Subtotal = quote.Subtotal,
                    DiscountAmount = quote.DiscountAmount,
                    TaxAmount = quote.TaxAmount,
                    TotalAmount = quote.TotalAmount,
                    AmountPaid = 0,
                    BalanceDue = quote.TotalAmount,

                    Status = "Unpaid",
                    Notes = quote.Notes,
                    Terms = quote.Terms,

                    CreatedByUserId = userId.Value,
                    CreatedByName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Invoices.Add(invoice);
                await _db.SaveChangesAsync();

                foreach (var quoteItem in quote.QuoteItems)
                {
                    _db.InvoiceItems.Add(new InvoiceItem
                    {
                        InvoiceId = invoice.Id,
                        ProductId = quoteItem.ProductId,
                        ProductName = quoteItem.ProductName,
                        SKU = quoteItem.SKU,
                        ProductType = quoteItem.ProductType,
                        UnitOfMeasure = quoteItem.UnitOfMeasure,
                        Quantity = quoteItem.Quantity,
                        UnitPrice = quoteItem.UnitPrice,
                        LineTotal = quoteItem.LineTotal,
                        CreatedAt = DateTime.UtcNow
                    });

                    auditItems.Add(new
                    {
                        quoteItem.ProductId,
                        quoteItem.ProductName,
                        quoteItem.SKU,
                        quoteItem.ProductType,
                        quoteItem.UnitOfMeasure,
                        quoteItem.Quantity,
                        quoteItem.UnitPrice,
                        quoteItem.LineTotal
                    });
                }

                quote.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                completedInvoice = invoice;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Invoice conversion failed: {ex.Message}", null);
            }

            if (completedInvoice != null)
            {
                await _auditLogService.LogAsync(
                    module: "Invoices",
                    action: "Create",
                    entityName: "Invoice",
                    entityId: completedInvoice.Id,
                    summary: $"Created invoice {completedInvoice.InvoiceNumber} from quote {quote.QuoteNumber}. Total {completedInvoice.TotalAmount:0.00}.",
                    oldValues: new
                    {
                        QuoteId = quote.Id,
                        quote.QuoteNumber,
                        quote.Status,
                        quote.TotalAmount
                    },
                    newValues: new
                    {
                        completedInvoice.Id,
                        completedInvoice.InvoiceNumber,
                        completedInvoice.QuoteId,
                        QuoteNumber = quote.QuoteNumber,
                        completedInvoice.CustomerId,
                        completedInvoice.CustomerName,
                        completedInvoice.CustomerPhone,
                        completedInvoice.CustomerEmail,
                        completedInvoice.InvoiceDate,
                        completedInvoice.DueDate,
                        completedInvoice.Subtotal,
                        completedInvoice.DiscountAmount,
                        completedInvoice.TaxAmount,
                        completedInvoice.TotalAmount,
                        completedInvoice.AmountPaid,
                        completedInvoice.BalanceDue,
                        completedInvoice.Status,
                        completedInvoice.Notes,
                        completedInvoice.Terms,
                        Items = auditItems
                    });
            }

            return (true, $"Invoice created successfully: {completedInvoice?.InvoiceNumber}", completedInvoice?.Id);
        }

        public async Task<(bool Success, string Message)> RecordPaymentAsync(RecordInvoicePaymentModel model)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();
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

            if (model.InvoiceId <= 0)
            {
                return (false, "Invoice is required.");
            }

            if (model.Amount <= 0)
            {
                return (false, "Payment amount must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(model.PaymentMethod))
            {
                return (false, "Payment method is required.");
            }

            var invoice = await _db.Invoices
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x =>
                    x.Id == model.InvoiceId &&
                    x.TenantId == tenantId.Value);

            if (invoice == null)
            {
                return (false, "Invoice not found.");
            }

            if (invoice.Status == "Cancelled")
            {
                return (false, "Cannot record payment for a cancelled invoice.");
            }

            if (invoice.Status == "Paid" || invoice.BalanceDue <= 0)
            {
                return (false, "This invoice is already fully paid.");
            }

            if (model.Amount > invoice.BalanceDue)
            {
                return (false, $"Payment cannot be greater than the balance due. Balance due: {invoice.BalanceDue:0.00}");
            }

            var oldValues = new
            {
                invoice.InvoiceNumber,
                invoice.TotalAmount,
                invoice.AmountPaid,
                invoice.BalanceDue,
                invoice.Status
            };

            var payment = new InvoicePayment
            {
                TenantId = tenantId.Value,
                BranchId = invoice.BranchId ?? branchId,
                InvoiceId = invoice.Id,
                Amount = model.Amount,
                PaymentMethod = model.PaymentMethod.Trim(),
                ReferenceNumber = Clean(model.ReferenceNumber),
                PaymentDate = model.PaymentDate.Date,
                Notes = Clean(model.Notes),
                ReceivedByUserId = userId.Value,
                ReceivedByName = userName,
                CreatedAt = DateTime.UtcNow
            };

            _db.InvoicePayments.Add(payment);

            invoice.AmountPaid += model.Amount;
            invoice.BalanceDue = invoice.TotalAmount - invoice.AmountPaid;

            if (invoice.BalanceDue <= 0)
            {
                invoice.BalanceDue = 0;
                invoice.Status = "Paid";
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.Status = "Partially Paid";
            }
            else
            {
                invoice.Status = "Unpaid";
            }

            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Invoices",
                action: "Payment",
                entityName: "InvoicePayment",
                entityId: payment.Id,
                summary: $"Recorded payment of {payment.Amount:0.00} for invoice {invoice.InvoiceNumber}. Balance due {invoice.BalanceDue:0.00}.",
                oldValues: oldValues,
                newValues: new
                {
                    payment.Id,
                    payment.InvoiceId,
                    invoice.InvoiceNumber,
                    payment.Amount,
                    payment.PaymentMethod,
                    payment.ReferenceNumber,
                    payment.PaymentDate,
                    payment.Notes,
                    payment.ReceivedByName,
                    InvoiceAfterPayment = new
                    {
                        invoice.TotalAmount,
                        invoice.AmountPaid,
                        invoice.BalanceDue,
                        invoice.Status
                    }
                });

            return (true, "Payment recorded successfully.");
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