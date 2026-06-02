namespace PosPlatform.Domain.Entities
{
    public class ExpenseCategory
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}