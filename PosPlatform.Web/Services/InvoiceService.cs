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
    }
}