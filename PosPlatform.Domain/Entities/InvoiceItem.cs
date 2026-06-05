namespace PosPlatform.Domain.Entities
{
    public class InvoiceItem
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

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