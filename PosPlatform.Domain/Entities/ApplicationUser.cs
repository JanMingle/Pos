using Microsoft.AspNetCore.Identity;

namespace PosPlatform.Domain.Entities
{
    public class ApplicationUser : IdentityUser<int>
    {
        public int TenantId { get; set; }
        public int? BranchId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Tenant? Tenant { get; set; }
        public Branch? Branch { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}