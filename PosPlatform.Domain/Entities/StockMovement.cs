namespace PosPlatform.Domain.Entities
{
    public class StockMovement
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public int? BranchId { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }

        public decimal QuantityBefore { get; set; }
        public decimal QuantityAfter { get; set; }

        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Tenant? Tenant { get; set; }
        public Branch? Branch { get; set; }
    }
}