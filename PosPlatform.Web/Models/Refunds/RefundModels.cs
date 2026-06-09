using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Refunds
{
    public class RefundableSaleViewModel
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string CashierName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }

        public List<RefundableSaleItemViewModel> Items { get; set; } = new();
    }

    public class RefundableSaleItemViewModel
    {
        public int SaleItemId { get; set; }

        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        public string? VariantName { get; set; }
        public string? VariantSize { get; set; }
        public string? VariantColor { get; set; }
        public string? VariantSKU { get; set; }
        public string? VariantBarcode { get; set; }

        public string ProductType { get; set; } = "Physical Product";
        public bool TrackStock { get; set; }
        public string? UnitOfMeasure { get; set; }

        public decimal QuantitySold { get; set; }
        public decimal QuantityAlreadyReturned { get; set; }
        public decimal QuantityRemaining { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal RefundableAmount => QuantityRemaining * UnitPrice;

        public bool IsVariant => ProductVariantId.HasValue;
    }

    public class CreateRefundRequest
    {
        [Required]
        public int SaleId { get; set; }

        [Required]
        public string ReturnType { get; set; } = "Refund";

        [Required]
        public string RefundMethod { get; set; } = "Original Payment";

        public bool RestockItems { get; set; } = true;

        [StringLength(500)]
        public string? Reason { get; set; }

        public List<CreateRefundItemRequest> Items { get; set; } = new();
    }

    public class CreateRefundItemRequest
    {
        [Required]
        public int SaleItemId { get; set; }

        [Range(0.01, 999999999)]
        public decimal Quantity { get; set; }
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public int? SaleReturnId { get; set; }
        public string? ReturnNumber { get; set; }
        public decimal TotalRefundAmount { get; set; }
    }

    public class SaleReturnHistoryRowViewModel
    {
        public int Id { get; set; }

        public string ReturnNumber { get; set; } = string.Empty;
        public string SaleNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string ReturnType { get; set; } = string.Empty;
        public string RefundMethod { get; set; } = string.Empty;
        public string ReturnedByName { get; set; } = string.Empty;

        public decimal TotalRefundAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}