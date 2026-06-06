using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Products
{
    public class ProductVariantViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;

        public string? Size { get; set; }
        public string? Color { get; set; }

        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }

        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public decimal QuantityInStock { get; set; }
        public decimal ReorderLevel { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class ProductVariantFormModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        [Required]
        [StringLength(150)]
        public string VariantName { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Size { get; set; }

        [StringLength(80)]
        public string? Color { get; set; }

        [Required]
        [StringLength(80)]
        public string SKU { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Barcode { get; set; }

        [Range(0, 999999999)]
        public decimal CostPrice { get; set; }

        [Range(0, 999999999)]
        public decimal SellingPrice { get; set; }

        [Range(0, 999999999)]
        public decimal QuantityInStock { get; set; }

        [Range(0, 999999999)]
        public decimal ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;
    }
}