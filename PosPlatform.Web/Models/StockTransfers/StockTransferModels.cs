using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.StockTransfers
{
    public class CreateStockTransferModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select source branch.")]
        public int SourceBranchId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select destination branch.")]
        public int DestinationBranchId { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.Today;

        [StringLength(500)]
        public string? Notes { get; set; }

        public List<CreateStockTransferItemModel> Items { get; set; } = new();
    }

    public class CreateStockTransferItemModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select product.")]
        public int ProductId { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; } = 1;
    }

    public class StockTransferProductOptionViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal QuantityInStock { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal CostPrice { get; set; }
    }

    public class StockTransferHistoryRowViewModel
    {
        public int Id { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public DateTime TransferDate { get; set; }

        public string SourceBranchName { get; set; } = "-";
        public string DestinationBranchName { get; set; } = "-";

        public int ItemCount { get; set; }
        public decimal TotalQuantity { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public string? Notes { get; set; }
    }
}