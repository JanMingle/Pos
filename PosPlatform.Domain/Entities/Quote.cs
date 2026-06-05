namespace PosPlatform.Domain.Entities
{
    public class Quote
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public string QuoteNumber { get; set; } = string.Empty;

        public DateTime QuoteDate { get; set; } = DateTime.UtcNow;
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

        public int CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<QuoteItem> QuoteItems { get; set; } = new List<QuoteItem>();
    }
}