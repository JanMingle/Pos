namespace PosPlatform.Domain.Entities
{
    public class Expense
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }

        public string ExpenseNumber { get; set; } = string.Empty;

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        public string? VendorName { get; set; }
        public string? ReferenceNumber { get; set; }

        public string PaymentMethod { get; set; } = "Cash";

        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Recorded";

        public string? Notes { get; set; }

        public int CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}