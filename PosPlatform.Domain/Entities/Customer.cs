namespace PosPlatform.Domain.Entities
{
    public class Customer
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string CustomerType { get; set; } = "Individual";

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? BusinessName { get; set; }

        public string? Phone { get; set; }
        public string? Email { get; set; }

        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}