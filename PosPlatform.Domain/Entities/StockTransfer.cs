namespace PosPlatform.Domain.Entities
{
    public class StockTransfer
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public string TransferNumber { get; set; } = string.Empty;

        public int SourceBranchId { get; set; }
        public Branch? SourceBranch { get; set; }

        public int DestinationBranchId { get; set; }
        public Branch? DestinationBranch { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Completed";

        public string? Notes { get; set; }

        public int? CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
    }
}