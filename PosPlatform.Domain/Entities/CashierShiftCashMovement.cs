namespace PosPlatform.Domain.Entities
{
    public class CashierShiftCashMovement
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int CashierShiftId { get; set; }
        public CashierShift? CashierShift { get; set; }

        public int CashierUserId { get; set; }
        public ApplicationUser? CashierUser { get; set; }

        public string CashierName { get; set; } = string.Empty;

        public string MovementType { get; set; } = "Cash In"; // Cash In / Cash Out

        public decimal Amount { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}