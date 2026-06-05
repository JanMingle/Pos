using Microsoft.EntityFrameworkCore;
using PosPlatform.Domain.Entities;
using PosPlatform.Infrastructure.Data;
using PosPlatform.Web.Models.Quotes;
using System.Security.Claims;

namespace PosPlatform.Web.Services
{
    public class QuoteService
    {
        private readonly AppDbContext _db;
        private readonly TenantContextService _tenantContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditLogService _auditLogService;

        public QuoteService(
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

        public async Task<List<QuoteProductOptionViewModel>> SearchProductsAsync(string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();
            var branchId = await _tenantContext.GetBranchIdAsync();

            if (tenantId == null)
            {
                return new List<QuoteProductOptionViewModel>();
            }

            var query = _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.ProductName.Contains(term) ||
                    x.SKU.Contains(term) ||
                    x.ProductType.Contains(term) ||
                    (x.Barcode != null && x.Barcode.Contains(term)));
            }

            return await query
                .OrderBy(x => x.ProductName)
                .Take(30)
                .Select(x => new QuoteProductOptionViewModel
                {
                    Id = x.Id,
                    ProductName = x.ProductName,
                    SKU = x.SKU,
                    Barcode = x.Barcode,
                    ProductType = x.ProductType,
                    UnitOfMeasure = x.UnitOfMeasure,
                    SellingPrice = x.SellingPrice,
                    QuantityInStock = x.QuantityInStock,
                    TrackStock = x.TrackStock,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<QuoteCustomerOptionViewModel>> SearchCustomersAsync(string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<QuoteCustomerOptionViewModel>();
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
                .Take(30)
                .Select(x => new QuoteCustomerOptionViewModel
                {
                    Id = x.Id,
                    DisplayName = x.CustomerType == "Business"
                        ? (x.BusinessName ?? "Business Customer")
                        : ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim(),
                    Phone = x.Phone,
                    Email = x.Email
                })
                .ToListAsync();
        }

        public async Task<List<QuoteListItemViewModel>> GetQuotesAsync(DateTime? fromDate, DateTime? toDate, string? statusFilter, string? search)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return new List<QuoteListItemViewModel>();
            }

            var query = _db.Quotes
                .AsNoTracking()
                .Include(x => x.QuoteItems)
                .Where(x => x.TenantId == tenantId.Value);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.QuoteDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.QuoteDate < to);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
            {
                query = query.Where(x => x.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();

                query = query.Where(x =>
                    x.QuoteNumber.Contains(term) ||
                    (x.CustomerName != null && x.CustomerName.Contains(term)) ||
                    (x.CustomerPhone != null && x.CustomerPhone.Contains(term)) ||
                    (x.CustomerEmail != null && x.CustomerEmail.Contains(term)));
            }

            return await query
                .OrderByDescending(x => x.QuoteDate)
                .ThenByDescending(x => x.Id)
                .Take(150)
                .Select(x => new QuoteListItemViewModel
                {
                    Id = x.Id,
                    QuoteNumber = x.QuoteNumber,
                    QuoteDate = x.QuoteDate,
                    ExpiryDate = x.ExpiryDate,
                    CustomerName = string.IsNullOrWhiteSpace(x.CustomerName) ? "Walk-in / Unsaved" : x.CustomerName!,
                    Status = x.Status,
                    ItemCount = x.QuoteItems.Count,
                    TotalAmount = x.TotalAmount,
                    CreatedByName = x.CreatedByName,
                    Notes = x.Notes
                })
                .ToListAsync();
        }

        public async Task<QuoteDetailsViewModel?> GetQuoteDetailsAsync(int quoteId)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return null;
            }

            var details = await _db.Quotes
                .AsNoTracking()
                .Include(x => x.QuoteItems)
                .Where(x => x.Id == quoteId && x.TenantId == tenantId.Value)
                .Select(x => new QuoteDetailsViewModel
                {
                    Id = x.Id,
                    QuoteNumber = x.QuoteNumber,
                    QuoteDate = x.QuoteDate,
                    ExpiryDate = x.ExpiryDate,
                    CustomerName = x.CustomerName,
                    CustomerPhone = x.CustomerPhone,
                    CustomerEmail = x.CustomerEmail,
                    Subtotal = x.Subtotal,
                    DiscountAmount = x.DiscountAmount,
                    TaxAmount = x.TaxAmount,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status,
                    Notes = x.Notes,
                    Terms = x.Terms,
                    CreatedByName = x.CreatedByName,
                    CreatedAt = x.CreatedAt,
                    Items = x.QuoteItems
                        .OrderBy(i => i.Id)
                        .Select(i => new QuoteItemViewModel
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

            if (details == null)
            {
                return null;
            }

            details.InvoiceId = await _db.Invoices
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.QuoteId == quoteId)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            return details;
        }

        public async Task<(bool Success, string Message)> CreateQuoteAsync(CreateQuoteModel model)
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

            if (model.Items.Count == 0)
            {
                return (false, "Add at least one item to the quote.");
            }

            if (model.Items.Any(x => x.ProductId <= 0))
            {
                return (false, "Select a product for every line.");
            }

            if (model.Items.Any(x => x.Quantity <= 0))
            {
                return (false, "Quantity must be greater than zero.");
            }

            if (model.DiscountAmount < 0)
            {
                return (false, "Discount cannot be negative.");
            }

            var settings = await _db.BusinessSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value);

            var taxEnabled = settings?.TaxEnabled ?? false;
            var taxRate = settings?.TaxRate ?? 0;

            Customer? selectedCustomer = null;

            if (model.CustomerId.HasValue && model.CustomerId.Value > 0)
            {
                selectedCustomer = await _db.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.CustomerId.Value &&
                        x.TenantId == tenantId.Value &&
                        x.IsActive);

                if (selectedCustomer == null)
                {
                    return (false, "Selected customer could not be found or is inactive.");
                }
            }

            var groupedItems = model.Items
                .GroupBy(x => x.ProductId)
                .Select(x => new CreateQuoteItemModel
                {
                    ProductId = x.Key,
                    Quantity = x.Sum(i => i.Quantity)
                })
                .ToList();

            var productIds = groupedItems.Select(x => x.ProductId).ToList();

            var products = await _db.Products
                .Where(x =>
                    x.TenantId == tenantId.Value &&
                    x.IsActive &&
                    productIds.Contains(x.Id) &&
                    (!branchId.HasValue || x.BranchId == null || x.BranchId == branchId.Value))
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return (false, "One or more products could not be found for your branch.");
            }

            var subtotal = groupedItems.Sum(item =>
            {
                var product = products.First(x => x.Id == item.ProductId);
                return item.Quantity * product.SellingPrice;
            });

            if (model.DiscountAmount > subtotal)
            {
                return (false, "Discount cannot be greater than subtotal.");
            }

            var taxableAmount = subtotal - model.DiscountAmount;
            var taxAmount = taxEnabled
                ? Math.Round(taxableAmount * (taxRate / 100), 2)
                : 0;

            var total = taxableAmount + taxAmount;

            var customerName = selectedCustomer != null
                ? GetCustomerDisplayName(selectedCustomer)
                : Clean(model.CustomerName);

            var customerPhone = selectedCustomer?.Phone ?? Clean(model.CustomerPhone);
            var customerEmail = selectedCustomer?.Email ?? Clean(model.CustomerEmail);

            Quote? completedQuote = null;
            var auditItems = new List<object>();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var quote = new Quote
                {
                    TenantId = tenantId.Value,
                    BranchId = branchId,
                    CustomerId = selectedCustomer?.Id,
                    QuoteNumber = $"QT-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    QuoteDate = model.QuoteDate.Date,
                    ExpiryDate = model.ExpiryDate?.Date,
                    CustomerName = customerName,
                    CustomerPhone = customerPhone,
                    CustomerEmail = customerEmail,
                    Subtotal = subtotal,
                    DiscountAmount = model.DiscountAmount,
                    TaxAmount = taxAmount,
                    TotalAmount = total,
                    Status = "Draft",
                    Notes = Clean(model.Notes),
                    Terms = Clean(model.Terms),
                    CreatedByUserId = userId.Value,
                    CreatedByName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Quotes.Add(quote);
                await _db.SaveChangesAsync();

                foreach (var item in groupedItems)
                {
                    var product = products.First(x => x.Id == item.ProductId);
                    var lineTotal = item.Quantity * product.SellingPrice;

                    _db.QuoteItems.Add(new QuoteItem
                    {
                        QuoteId = quote.Id,
                        ProductId = product.Id,
                        ProductName = product.ProductName,
                        SKU = product.SKU,
                        ProductType = product.ProductType,
                        UnitOfMeasure = product.UnitOfMeasure,
                        Quantity = item.Quantity,
                        UnitPrice = product.SellingPrice,
                        LineTotal = lineTotal,
                        CreatedAt = DateTime.UtcNow
                    });

                    auditItems.Add(new
                    {
                        product.Id,
                        product.ProductName,
                        product.SKU,
                        product.ProductType,
                        product.UnitOfMeasure,
                        Quantity = item.Quantity,
                        UnitPrice = product.SellingPrice,
                        LineTotal = lineTotal
                    });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                completedQuote = quote;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Quote failed: {ex.Message}");
            }

            if (completedQuote != null)
            {
                await _auditLogService.LogAsync(
                    module: "Quotes",
                    action: "Create",
                    entityName: "Quote",
                    entityId: completedQuote.Id,
                    summary: $"Created quote {completedQuote.QuoteNumber} for {completedQuote.TotalAmount:0.00}.",
                    oldValues: null,
                    newValues: new
                    {
                        completedQuote.Id,
                        completedQuote.QuoteNumber,
                        completedQuote.BranchId,
                        completedQuote.CustomerId,
                        completedQuote.CustomerName,
                        completedQuote.CustomerPhone,
                        completedQuote.CustomerEmail,
                        completedQuote.QuoteDate,
                        completedQuote.ExpiryDate,
                        completedQuote.Subtotal,
                        completedQuote.DiscountAmount,
                        completedQuote.TaxAmount,
                        completedQuote.TotalAmount,
                        completedQuote.Status,
                        completedQuote.Notes,
                        completedQuote.Terms,
                        Items = auditItems
                    });
            }

            return (true, $"Quote created successfully. Quote: {completedQuote?.QuoteNumber}");
        }

        public async Task<(bool Success, string Message)> UpdateStatusAsync(int quoteId, string status)
        {
            var tenantId = await _tenantContext.GetTenantIdAsync();

            if (tenantId == null)
            {
                return (false, "Tenant not found.");
            }

            var normalizedStatus = NormalizeStatus(status);

            if (normalizedStatus == null)
            {
                return (false, "Invalid quote status.");
            }

            var quote = await _db.Quotes.FirstOrDefaultAsync(x =>
                x.Id == quoteId &&
                x.TenantId == tenantId.Value);

            if (quote == null)
            {
                return (false, "Quote not found.");
            }

            var oldValues = new
            {
                quote.QuoteNumber,
                quote.Status
            };

            quote.Status = normalizedStatus;
            quote.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _auditLogService.LogAsync(
                module: "Quotes",
                action: "Update",
                entityName: "Quote",
                entityId: quote.Id,
                summary: $"Updated quote {quote.QuoteNumber} status to {quote.Status}.",
                oldValues: oldValues,
                newValues: new
                {
                    quote.QuoteNumber,
                    quote.Status
                });

            return (true, $"Quote marked as {quote.Status}.");
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

        private static string? NormalizeStatus(string? value)
        {
            return value?.Trim() switch
            {
                "Draft" => "Draft",
                "Sent" => "Sent",
                "Accepted" => "Accepted",
                "Cancelled" => "Cancelled",
                _ => null
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

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}