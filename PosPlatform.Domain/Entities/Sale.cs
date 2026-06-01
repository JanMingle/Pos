namespace PosPlatform.Domain.Entities
{
    public class Sale
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public int? BranchId { get; set; }

        public int? CashierUserId { get; set; }
        public string? CashierName { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public string SaleNumber { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; } = "Cash";
        public decimal AmountPaid { get; set; }
        public decimal ChangeAmount { get; set; }

        public string Status { get; set; } = "Completed";
        public string? Notes { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Tenant? Tenant { get; set; }
        public Branch? Branch { get; set; }

        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}