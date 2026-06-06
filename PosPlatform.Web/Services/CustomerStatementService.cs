using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.CustomerStatements;

namespace PosPlatform.Web.Services
{
    public class CustomerStatementService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;

        public CustomerStatementService(
            AppDbContext db,
            TenantContextService tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<List<CustomerStatementCustomerOptionViewModel>> SearchCustomersAsync(string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<CustomerStatementCustomerOptionViewModel>();
            }

            var query = _db.Customers
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId.Value && x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    (x.FirstName != null && x.FirstName.Contains(term)) ||
                    (x.LastName != null && x.LastName.Contains(term)) ||
                    (x.BusinessName != null && x.BusinessName.Contains(term)) ||
                    (x.Phone != null && x.Phone.Contains(term)) ||
                    (x.Email != null && x.Email.Contains(term)));
            }

            return await query
                .OrderBy(x => x.CustomerType == "Business" ? x.BusinessName : x.FirstName)
                .Take(40)
                .Select(x => new CustomerStatementCustomerOptionViewModel
                {
                    Id = x.Id,
                    DisplayName = x.CustomerType == "Business"
                        ? (x.BusinessName ?? "Business Customer")
                        : ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim(),
                    Phone = x.Phone,
                    Email = x.Email,
                    CustomerType = x.CustomerType
                })
                .ToListAsync();
        }

        public async Task<CustomerStatementViewModel?> GetStatementAsync(
            int customerId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var customer = await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == customerId &&
                    x.TenantId == tenantId.Value);

            if (customer == null)
            {
                return null;
            }

            var from = fromDate?.Date;
            var toExclusive = toDate?.Date.AddDays(1);

            var invoicesQuery = _db.Invoices
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.CustomerId == customerId);

            if (from.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(x => x.InvoiceDate >= from.Value);
            }

            if (toExclusive.HasValue)
            {
                invoicesQuery = invoicesQuery.Where(x => x.InvoiceDate < toExclusive.Value);
            }

            var invoices = await invoicesQuery
                .OrderBy(x => x.InvoiceDate)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNumber,
                    x.InvoiceDate,
                    x.TotalAmount,
                    x.AmountPaid,
                    x.BalanceDue,
                    x.Status
                })
                .ToListAsync();

            var invoiceIds = invoices.Select(x => x.Id).ToList();

            var paymentsQuery = _db.InvoicePayments
                .AsNoTracking()
                .Include(x => x.Invoice)
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.Invoice != null &&
                    x.Invoice.CustomerId == customerId);

            if (from.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(x => x.PaymentDate >= from.Value);
            }

            if (toExclusive.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(x => x.PaymentDate < toExclusive.Value);
            }

            var payments = await paymentsQuery
                .OrderBy(x => x.PaymentDate)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.PaymentDate,
                    x.Amount,
                    x.PaymentMethod,
                    x.ReferenceNumber,
                    x.ReceivedByName,
                    InvoiceNumber = x.Invoice != null ? x.Invoice.InvoiceNumber : "-"
                })
                .ToListAsync();

            var salesQuery = _db.Sales
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.CustomerId == customerId &&
                    x.Status != "Voided");

            if (from.HasValue)
            {
                salesQuery = salesQuery.Where(x => x.CreatedAt >= from.Value);
            }

            if (toExclusive.HasValue)
            {
                salesQuery = salesQuery.Where(x => x.CreatedAt < toExclusive.Value);
            }

            var salesHistory = await salesQuery
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(100)
                .Select(x => new CustomerSalesHistoryViewModel
                {
                    Id = x.Id,
                    SaleNumber = x.SaleNumber,
                    CreatedAt = x.CreatedAt,
                    PaymentMethod = x.PaymentMethod,
                    Status = x.Status,
                    TotalAmount = x.TotalAmount,
                    AmountPaid = x.AmountPaid,
                    ChangeAmount = x.ChangeAmount
                })
                .ToListAsync();

            var lines = new List<CustomerStatementLineViewModel>();

            foreach (var invoice in invoices)
            {
                lines.Add(new CustomerStatementLineViewModel
                {
                    Date = invoice.InvoiceDate,
                    Type = "Invoice",
                    Reference = invoice.InvoiceNumber,
                    Description = $"Invoice raised - {invoice.Status}",
                    Debit = invoice.TotalAmount,
                    Credit = 0
                });
            }

            foreach (var payment in payments)
            {
                lines.Add(new CustomerStatementLineViewModel
                {
                    Date = payment.PaymentDate,
                    Type = "Payment",
                    Reference = string.IsNullOrWhiteSpace(payment.ReferenceNumber)
                        ? payment.InvoiceNumber
                        : payment.ReferenceNumber,
                    Description = $"Payment received via {payment.PaymentMethod} for invoice {payment.InvoiceNumber}",
                    Debit = 0,
                    Credit = payment.Amount
                });
            }

            lines = lines
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Type == "Invoice" ? 0 : 1)
                .ToList();

            decimal runningBalance = 0;

            foreach (var line in lines)
            {
                runningBalance += line.Debit;
                runningBalance -= line.Credit;
                line.Balance = runningBalance;
            }

            var customerName = GetCustomerDisplayName(customer);

            return new CustomerStatementViewModel
            {
                CustomerId = customer.Id,
                CustomerName = customerName,
                CustomerPhone = customer.Phone,
                CustomerEmail = customer.Email,
                FromDate = fromDate,
                ToDate = toDate,

                TotalInvoices = invoices.Sum(x => x.TotalAmount),
                TotalPayments = payments.Sum(x => x.Amount),
                BalanceDue = invoices.Sum(x => x.TotalAmount) - payments.Sum(x => x.Amount),

                InvoiceCount = invoices.Count,
                PaymentCount = payments.Count,
                SalesCount = salesHistory.Count,
                TotalSales = salesHistory.Sum(x => x.TotalAmount),

                Lines = lines,
                SalesHistory = salesHistory
            };
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
    }
}