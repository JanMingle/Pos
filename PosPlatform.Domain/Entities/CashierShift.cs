namespace PosPlatform.Domain.Entities
{
    public class CashierShift
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int CashierUserId { get; set; }
        public ApplicationUser? CashierUser { get; set; }

        public string CashierName { get; set; } = string.Empty;

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public decimal OpeningCash { get; set; }
        public decimal ClosingCash { get; set; }

        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal EftSales { get; set; }
        public decimal TotalSales { get; set; }

        public decimal ExpectedCash { get; set; }
        public decimal CashDifference { get; set; }

        public string Status { get; set; } = "Open";

        public string? OpeningNotes { get; set; }
        public string? ClosingNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}