namespace PosPlatform.Domain.Entities
{
    public class StockPurchaseItem
    {
        public int Id { get; set; }

        public int StockPurchaseId { get; set; }
        public StockPurchase? StockPurchase { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal { get; set; }

        public decimal QuantityBefore { get; set; }
        public decimal QuantityAfter { get; set; }

        public string? UnitOfMeasure { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}