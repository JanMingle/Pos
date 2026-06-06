namespace PosPlatform.Domain.Entities
{
    public class Invoice
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int? QuoteId { get; set; }
        public Quote? Quote { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
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

        public int CreatedByUserId { get; set; }

        public string FollowUpStatus { get; set; } = "Not Started";
        public DateTime? LastFollowUpAt { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string? FollowUpNotes { get; set; }
        public int FollowUpCount { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();


    }
}