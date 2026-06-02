namespace PosPlatform.Domain.Entities
{
    public class SaleReturnItem
    {
        public int Id { get; set; }

        public int SaleReturnId { get; set; }
        public SaleReturn? SaleReturn { get; set; }

        public int SaleItemId { get; set; }
        public SaleItem? SaleItem { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public bool TrackStock { get; set; }
        public string ProductType { get; set; } = "Physical Product";
        public string? UnitOfMeasure { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal UnitCost { get; set; }
        public decimal CostTotal { get; set; }
    }
}