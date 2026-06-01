using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Customers
{
    public class CustomerListItemViewModel
    {
        public int Id { get; set; }

        public string CustomerType { get; set; } = "Individual";
        public string DisplayName { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? BusinessName { get; set; }

        public string? Phone { get; set; }
        public string? Email { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }

    public class CustomerFormModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(30)]
        public string CustomerType { get; set; } = "Individual";

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(180)]
        public string? BusinessName { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CustomerOptionViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}