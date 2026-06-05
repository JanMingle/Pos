using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Quotes
{
    public class QuoteListItemViewModel
    {
        public int Id { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public DateTime QuoteDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public string CustomerName { get; set; } = "Walk-in / Unsaved";
        public string Status { get; set; } = "Draft";

        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }

        public string CreatedByName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class QuoteProductOptionViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }

        public string ProductType { get; set; } = "Physical Product";
        public string? UnitOfMeasure { get; set; }

        public decimal SellingPrice { get; set; }
        public decimal QuantityInStock { get; set; }
        public bool TrackStock { get; set; }
        public bool IsActive { get; set; }
    }

    public class QuoteCustomerOptionViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class CreateQuoteModel
    {
        public int? CustomerId { get; set; }

        [StringLength(150)]
        public string? CustomerName { get; set; }

        [StringLength(50)]
        public string? CustomerPhone { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? CustomerEmail { get; set; }

        public DateTime QuoteDate { get; set; } = DateTime.Today;
        public DateTime? ExpiryDate { get; set; } = DateTime.Today.AddDays(7);

        [Range(0, 999999999)]
        public decimal DiscountAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(800)]
        public string? Terms { get; set; }

        public List<CreateQuoteItemModel> Items { get; set; } = new();
    }

    public class CreateQuoteItemModel
    {
        public int ProductId { get; set; }

        [Range(0.01, 999999999)]
        public decimal Quantity { get; set; } = 1;
    }

    public class QuoteDetailsViewModel
    {
        public int Id { get; set; }
        public int? InvoiceId { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public DateTime QuoteDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Draft";
        public string? Notes { get; set; }
        public string? Terms { get; set; }

        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<QuoteItemViewModel> Items { get; set; } = new();
    }

    public class QuoteItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string? UnitOfMeasure { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}