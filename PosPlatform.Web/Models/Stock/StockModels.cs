using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Stock
{
    public class StockProductRowViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string CategoryName { get; set; } = "-";

        public string ProductType { get; set; } = "Physical Product";
        public bool TrackStock { get; set; }
        public bool AgeRestricted { get; set; }
        public string? UnitOfMeasure { get; set; }

        public decimal QuantityInStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public bool IsActive { get; set; }

        public decimal StockValue => QuantityInStock * CostPrice;

        public bool IsOutOfStock => QuantityInStock <= 0;
        public bool IsLowStock => QuantityInStock > 0 && QuantityInStock <= ReorderLevel;
        public bool IsHealthy => QuantityInStock > ReorderLevel;
    }

    public class StockMovementRowViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string? UnitOfMeasure { get; set; }

        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal QuantityBefore { get; set; }
        public decimal QuantityAfter { get; set; }

        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
        public string? Notes { get; set; }
    }

    public class StockSummaryViewModel
    {
        public int TotalStockItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public decimal TotalUnits { get; set; }
        public decimal TotalStockValue { get; set; }
    }

    public class StockAdjustmentModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Select a stock-tracked item.")]
        public int ProductId { get; set; }

        [Required]
        public string AdjustmentType { get; set; } = "Stock In";

        [Range(0.01, 999999999, ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }

        [Range(0, 999999999, ErrorMessage = "New quantity cannot be negative.")]
        public decimal NewQuantity { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}