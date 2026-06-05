namespace PosPlatform.Domain.Entities
{
    public class QuoteItem
    {
        public int Id { get; set; }

        public int QuoteId { get; set; }
        public Quote? Quote { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        public string ProductType { get; set; } = "Physical Product";
        public string? UnitOfMeasure { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}