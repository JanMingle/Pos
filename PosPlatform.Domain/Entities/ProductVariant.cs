namespace PosPlatform.Domain.Entities
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string VariantName { get; set; } = string.Empty;

        public string? Size { get; set; }
        public string? Color { get; set; }

        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }

        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public decimal QuantityInStock { get; set; }
        public decimal ReorderLevel { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}