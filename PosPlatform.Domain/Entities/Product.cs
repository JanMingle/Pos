using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities
{
    public class Product : TenantEntity
    {
        public int? BranchId { get; set; }
        public int? ProductCategoryId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Description { get; set; }

        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal QuantityInStock { get; set; }
        public decimal ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;

        public Branch? Branch { get; set; }
        public ProductCategory? ProductCategory { get; set; }
        public string ProductType { get; set; } = "Physical Product";

        public bool TrackStock { get; set; } = true;

        public bool AgeRestricted { get; set; }

        public string? UnitOfMeasure { get; set; } = "Each";

        public int? DurationMinutes { get; set; }
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}