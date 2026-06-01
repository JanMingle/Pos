using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Suppliers
{
    public class SupplierListItemViewModel
    {
        public int Id { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }

    public class SupplierFormModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(180)]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? ContactPerson { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(80)]
        public string? TaxNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class SupplierOptionViewModel
    {
        public int Id { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }
}