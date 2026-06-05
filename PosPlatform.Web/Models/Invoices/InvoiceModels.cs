using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Invoices
{
    public class InvoiceListItemViewModel
    {
        public int Id { get; set; }
        public int? QuoteId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public string? QuoteNumber { get; set; }

        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string CustomerName { get; set; } = "Walk-in / Unsaved";

        public int ItemCount { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }

        public string Status { get; set; } = "Unpaid";
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class InvoiceDetailsViewModel
    {
        public int Id { get; set; }
        public int? QuoteId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public string? QuoteNumber { get; set; }

        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }

        public string Status { get; set; } = "Unpaid";
        public string? Notes { get; set; }
        public string? Terms { get; set; }

        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public List<InvoiceItemViewModel> Items { get; set; } = new();
        public List<InvoicePaymentViewModel> Payments { get; set; } = new();
    }

    public class InvoiceItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string? UnitOfMeasure { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class InvoicePaymentViewModel
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Cash";
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }

        public string ReceivedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RecordInvoicePaymentModel
    {
        public int InvoiceId { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}