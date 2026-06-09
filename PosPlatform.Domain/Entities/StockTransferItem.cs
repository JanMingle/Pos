namespace PosPlatform.Domain.Entities
{
    public class StockTransferItem
    {
        public int Id { get; set; }

        public int StockTransferId { get; set; }
        public StockTransfer? StockTransfer { get; set; }

        public int SourceProductId { get; set; }
        public Product? SourceProduct { get; set; }

        public int TargetProductId { get; set; }
        public Product? TargetProduct { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal SourceQuantityBefore { get; set; }
        public decimal SourceQuantityAfter { get; set; }

        public decimal DestinationQuantityBefore { get; set; }
        public decimal DestinationQuantityAfter { get; set; }

        public string? UnitOfMeasure { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? SourceProductVariantId { get; set; }
        public ProductVariant? SourceProductVariant { get; set; }

        public int? TargetProductVariantId { get; set; }
        public ProductVariant? TargetProductVariant { get; set; }

        public string? VariantName { get; set; }
        public string? VariantSize { get; set; }
        public string? VariantColor { get; set; }
        public string? VariantSKU { get; set; }
        public string? VariantBarcode { get; set; }
    }
}