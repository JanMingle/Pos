namespace PosPlatform.Web.Models.Products
{
    public class ProductListItemViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }

        public string ProductType { get; set; } = "Physical Product";
        public bool TrackStock { get; set; }
        public bool AgeRestricted { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int? DurationMinutes { get; set; }

        public string CategoryName { get; set; } = "-";
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal QuantityInStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsActive { get; set; }

        public bool IsService => ProductType == "Service";
        public bool IsStockTracked => TrackStock;
    }
}