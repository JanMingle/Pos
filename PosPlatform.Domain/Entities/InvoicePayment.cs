namespace PosPlatform.Domain.Entities
{
    public class InvoicePayment
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Cash";
        public string? ReferenceNumber { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        public int ReceivedByUserId { get; set; }
        public ApplicationUser? ReceivedByUser { get; set; }

        public string ReceivedByName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}