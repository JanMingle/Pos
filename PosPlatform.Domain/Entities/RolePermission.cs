using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities
{
    public class RolePermission : BaseEntity
    {
        public string RoleIdString
        {
            get => RoleId.ToString();
            set
            {
                if (int.TryParse(value, out var parsed))
                    RoleId = parsed;
            }
        }

        public int RoleId { get; set; }
        public Role? Role { get; set; }

        public int PermissionId { get; set; }
        public Permission? Permission { get; set; }
    }
}