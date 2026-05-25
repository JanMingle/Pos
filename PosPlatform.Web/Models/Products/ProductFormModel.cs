using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Products
{
    public class ProductFormModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string SKU { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Barcode { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductType { get; set; } = "Physical Product";

        public bool TrackStock { get; set; } = true;

        public bool AgeRestricted { get; set; }

        [StringLength(50)]
        public string? UnitOfMeasure { get; set; } = "Each";

        [Range(0, 100000)]
        public int? DurationMinutes { get; set; }

        [Range(0, 999999999)]
        public decimal CostPrice { get; set; }

        [Range(0.01, 999999999)]
        public decimal SellingPrice { get; set; }

        [Range(0, 999999999)]
        public decimal QuantityInStock { get; set; }

        [Range(0, 999999999)]
        public decimal ReorderLevel { get; set; }

        public int? ProductCategoryId { get; set; }
        public int? BranchId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}