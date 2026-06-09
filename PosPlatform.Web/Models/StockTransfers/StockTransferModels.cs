using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.StockTransfers
{
    public class StockTransferHistoryRowViewModel
    {
        public int Id { get; set; }

        public string TransferNumber { get; set; } = string.Empty;
        public DateTime TransferDate { get; set; }

        public string SourceBranchName { get; set; } = string.Empty;
        public string DestinationBranchName { get; set; } = string.Empty;

        public int ItemCount { get; set; }
        public decimal TotalQuantity { get; set; }

        public string Status { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class CreateStockTransferModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Select source branch.")]
        public int SourceBranchId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Select destination branch.")]
        public int DestinationBranchId { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.Today;

        [StringLength(500)]
        public string? Notes { get; set; }

        public List<CreateStockTransferItemModel> Items { get; set; } = new();
    }

    public class CreateStockTransferItemModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Select product.")]
        public int ProductId { get; set; }

        public int? SourceProductVariantId { get; set; }

        public int? TargetProductVariantId { get; set; }

        [Range(0.01, 999999999)]
        public decimal Quantity { get; set; }
    }

    public class StockTransferProductOptionViewModel
    {
        // Backward-compatible fields used by older page/service code
        public int Id { get; set; }
        public decimal QuantityInStock { get; set; }
        public decimal CostPrice { get; set; }

        // Variant-aware fields
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public string SKU { get; set; } = string.Empty;

        public string? VariantName { get; set; }
        public string? VariantSize { get; set; }
        public string? VariantColor { get; set; }
        public string? VariantSKU { get; set; }
        public string? VariantBarcode { get; set; }

        public decimal CurrentStock { get; set; }
        public string? UnitOfMeasure { get; set; }

        public bool IsVariant => ProductVariantId.HasValue;

        public string OptionKey =>
            ProductVariantId.HasValue
                ? $"v:{ProductVariantId.Value}"
                : $"p:{ProductId}";
    }
}