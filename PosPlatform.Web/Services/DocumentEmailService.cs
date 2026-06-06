using Microsoft.EntityFrameworkCore;
using PosPlatform.Infrastructure.Data;
using System.Net;

namespace PosPlatform.Web.Services
{
    public class DocumentEmailService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly EmailService _emailService;
        private readonly AuditLogService _auditLogService;

        public DocumentEmailService(
            AppDbContext db,
            TenantContextService tenantContext,
            EmailService emailService,
            AuditLogService auditLogService)
        {
            _db = db;
            _tenantContext = tenantContext;
            _emailService = emailService;
            _auditLogService = auditLogService;
        }

        public async Task<(bool Success, string Message)> SendQuoteEmailAsync(int quoteId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var quote = await _db.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == quoteId && x.TenantId == tenantId.Value);

            if (quote == null)
            {
                return (false, "Quote not found.");
            }

            if (string.IsNullOrWhiteSpace(quote.CustomerEmail))
            {
                return (false, "This quote does not have a customer email address.");
            }

            var settings = await _db.BusinessSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            var businessName = settings?.BusinessName ?? "POS Platform";
            var currency = string.IsNullOrWhiteSpace(settings?.CurrencySymbol) ? "R" : settings.CurrencySymbol;
            var quoteUrl = $"{_emailService.GetAppBaseUrl()}/quotes/print/{quote.Id}";

            var subject = $"Quote {quote.QuoteNumber} from {businessName}";

            var body = BuildDocumentEmailHtml(
                businessName: businessName,
                greetingName: quote.CustomerName,
                documentType: "quote",
                documentNumber: quote.QuoteNumber,
                totalLabel: "Quote Total",
                totalAmount: $"{currency} {quote.TotalAmount:0.00}",
                documentUrl: quoteUrl,
                buttonText: "View / Print Quote",
                footerText: "This quote can be printed or saved as a PDF from the link above."
            );

            var result = await _emailService.SendEmailAsync(
                quote.CustomerEmail,
                subject,
                body
            );

            if (result.Success)
            {
                await _auditLogService.LogAsync(
                    module: "Quotes",
                    action: "Email",
                    entityName: "Quote",
                    entityId: quote.Id,
                    summary: $"Emailed quote {quote.QuoteNumber} to {quote.CustomerEmail}.",
                    oldValues: null,
                    newValues: new
                    {
                        quote.Id,
                        quote.QuoteNumber,
                        quote.CustomerEmail,
                        quote.CustomerName,
                        QuoteUrl = quoteUrl
                    });
            }

            return result;
        }

        public async Task<(bool Success, string Message)> SendInvoiceEmailAsync(int invoiceId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var invoice = await _db.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.TenantId == tenantId.Value);

            if (invoice == null)
            {
                return (false, "Invoice not found.");
            }

            if (string.IsNullOrWhiteSpace(invoice.CustomerEmail))
            {
                return (false, "This invoice does not have a customer email address.");
            }

            var settings = await _db.BusinessSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            var businessName = settings?.BusinessName ?? "POS Platform";
            var currency = string.IsNullOrWhiteSpace(settings?.CurrencySymbol) ? "R" : settings.CurrencySymbol;
            var invoiceUrl = $"{_emailService.GetAppBaseUrl()}/invoices/print/{invoice.Id}";

            var subject = $"Invoice {invoice.InvoiceNumber} from {businessName}";

            var body = BuildDocumentEmailHtml(
                businessName: businessName,
                greetingName: invoice.CustomerName,
                documentType: "invoice",
                documentNumber: invoice.InvoiceNumber,
                totalLabel: "Balance Due",
                totalAmount: $"{currency} {invoice.BalanceDue:0.00}",
                documentUrl: invoiceUrl,
                buttonText: "View / Print Invoice",
                footerText: "This invoice can be printed or saved as a PDF from the link above."
            );

            var result = await _emailService.SendEmailAsync(
                invoice.CustomerEmail,
                subject,
                body
            );

            if (result.Success)
            {
                await _auditLogService.LogAsync(
                    module: "Invoices",
                    action: "Email",
                    entityName: "Invoice",
                    entityId: invoice.Id,
                    summary: $"Emailed invoice {invoice.InvoiceNumber} to {invoice.CustomerEmail}.",
                    oldValues: null,
                    newValues: new
                    {
                        invoice.Id,
                        invoice.InvoiceNumber,
                        invoice.CustomerEmail,
                        invoice.CustomerName,
                        InvoiceUrl = invoiceUrl
                    });
            }

            return result;
        }

        private static string BuildDocumentEmailHtml(
            string businessName,
            string? greetingName,
            string documentType,
            string documentNumber,
            string totalLabel,
            string totalAmount,
            string documentUrl,
            string buttonText,
            string footerText)
        {
            var safeBusinessName = H(businessName);
            var safeName = H(string.IsNullOrWhiteSpace(greetingName) ? "Customer" : greetingName);
            var safeDocumentType = H(documentType);
            var safeDocumentNumber = H(documentNumber);
            var safeTotalLabel = H(totalLabel);
            var safeTotalAmount = H(totalAmount);
            var safeDocumentUrl = H(documentUrl);
            var safeButtonText = H(buttonText);
            var safeFooterText = H(footerText);

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
</head>
<body style=""margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif;color:#111827;"">
    <div style=""max-width:640px;margin:0 auto;padding:28px 16px;"">
        <div style=""background:#ffffff;border-radius:18px;padding:28px;border:1px solid #e5e7eb;"">
            <div style=""border-bottom:1px solid #e5e7eb;padding-bottom:18px;margin-bottom:22px;"">
                <h2 style=""margin:0;font-size:22px;color:#111827;"">{safeBusinessName}</h2>
                <p style=""margin:6px 0 0;color:#64748b;font-size:14px;"">Your {safeDocumentType} is ready.</p>
            </div>

            <p style=""font-size:15px;line-height:1.6;margin:0 0 14px;"">Hi {safeName},</p>

            <p style=""font-size:15px;line-height:1.6;margin:0 0 18px;"">
                Please find your {safeDocumentType} <strong>{safeDocumentNumber}</strong> from {safeBusinessName}.
            </p>

            <div style=""background:#f8fafc;border:1px solid #e5e7eb;border-radius:14px;padding:16px;margin:18px 0;"">
                <span style=""display:block;color:#64748b;font-size:12px;text-transform:uppercase;font-weight:bold;"">{safeTotalLabel}</span>
                <strong style=""display:block;margin-top:5px;font-size:22px;color:#111827;"">{safeTotalAmount}</strong>
            </div>

            <a href=""{safeDocumentUrl}""
               style=""display:inline-block;background:#111827;color:#ffffff;text-decoration:none;padding:13px 18px;border-radius:12px;font-size:14px;font-weight:bold;"">
                {safeButtonText}
            </a>

            <p style=""font-size:13px;line-height:1.6;color:#64748b;margin:20px 0 0;"">
                {safeFooterText}
            </p>
        </div>

        <p style=""text-align:center;font-size:12px;color:#94a3b8;margin-top:16px;"">
            Sent by {safeBusinessName}
        </p>
    </div>
</body>
</html>";
        }

        private static string H(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}