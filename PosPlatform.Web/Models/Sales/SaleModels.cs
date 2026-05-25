using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Sales
{
    public class SaleProductOptionViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string? Barcode { get; set; }

        public string ProductType { get; set; } = "Physical Product";
        public bool TrackStock { get; set; }
        public bool AgeRestricted { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int? DurationMinutes { get; set; }

        public decimal SellingPrice { get; set; }
        public decimal QuantityInStock { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSaleRequest
    {
        public string PaymentMethod { get; set; } = "Cash";

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Notes { get; set; }

        public bool AgeRestrictionConfirmed { get; set; }

        [Range(0, 999999999)]
        public decimal DiscountAmount { get; set; }

        [Range(0, 999999999)]
        public decimal TaxAmount { get; set; }

        [Range(0, 999999999)]
        public decimal AmountPaid { get; set; }

        public List<CreateSaleItemRequest> Items { get; set; } = new();
    }

    public class CreateSaleItemRequest
    {
        public int ProductId { get; set; }

        [Range(0.01, 999999999)]
        public decimal Quantity { get; set; }
    }

    public class SaleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? SaleId { get; set; }
        public string? SaleNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public bool RequiresAgeConfirmation { get; set; }
    }

    public class SaleCartLineViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        public string ProductType { get; set; } = "Physical Product";
        public bool TrackStock { get; set; }
        public bool AgeRestricted { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int? DurationMinutes { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal AvailableStock { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class SaleReceiptViewModel
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string CashierName { get; set; } = "-";
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public string PaymentMethod { get; set; } = "Cash";
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeAmount { get; set; }

        public List<SaleReceiptItemViewModel> Items { get; set; } = new();
    }

    public class SaleReceiptItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class SaleHistoryRowViewModel
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SalesSummaryViewModel
    {
        public int TotalTransactions { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalTax { get; set; }

        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal EftSales { get; set; }

        public decimal AverageSale =>
            TotalTransactions == 0 ? 0 : TotalSales / TotalTransactions;
    }
}