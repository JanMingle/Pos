using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Users
{
    public class UserListItemViewModel
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public string RoleName { get; set; } = "-";
        public string BranchName { get; set; } = "-";

        public bool IsDisabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

        public string Status => IsDisabled ? "Disabled" : "Active";
    }

    public class UserFormModel
    {
        public int? Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [Required]
        public string RoleName { get; set; } = "Sales User";

        public int? BranchId { get; set; }

        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
    }

    public class RoleOptionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BranchOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}