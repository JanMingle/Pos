using Microsoft.AspNetCore.Identity;

namespace PosPlatform.Domain.Entities
{
    public class UserRole : IdentityUserRole<int>
    {
        public ApplicationUser? User { get; set; }
        public Role? Role { get; set; }
    }
}