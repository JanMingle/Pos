namespace PosPlatform.Domain.Entities
{
    public class StockPurchase
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public string PurchaseNumber { get; set; } = string.Empty;
        public string? SupplierInvoiceNumber { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Received";

        public string? Notes { get; set; }

        public int CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public string CreatedByName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StockPurchaseItem> StockPurchaseItems { get; set; } = new List<StockPurchaseItem>();
    }
}