namespace PosPlatform.Domain.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string UserName { get; set; } = "System";

        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }

        public string Summary { get; set; } = string.Empty;

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}