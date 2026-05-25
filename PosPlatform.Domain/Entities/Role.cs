using Microsoft.AspNetCore.Identity;

namespace PosPlatform.Domain.Entities
{
    public class Role : IdentityRole<int>
    {
        public int TenantId { get; set; }
        public string Description { get; set; } = string.Empty;

        public Tenant? Tenant { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}