namespace PosPlatform.Domain.Entities
{
    public class SaleReturn
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public int ReturnedByUserId { get; set; }
        public ApplicationUser? ReturnedByUser { get; set; }

        public string ReturnedByName { get; set; } = string.Empty;

        public string ReturnNumber { get; set; } = string.Empty;

        // Return, Refund, Void
        public string ReturnType { get; set; } = "Refund";

        public string RefundMethod { get; set; } = "Original Payment";

        public string Status { get; set; } = "Completed";

        public string? Reason { get; set; }

        public bool RestockItems { get; set; } = true;

        public decimal TotalRefundAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SaleReturnItem> SaleReturnItems { get; set; } = new List<SaleReturnItem>();
    }
}