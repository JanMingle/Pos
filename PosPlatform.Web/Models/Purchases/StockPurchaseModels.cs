using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Purchases
{
    public class StockPurchaseListItemViewModel
    {
        public int Id { get; set; }

        public string PurchaseNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierInvoiceNumber { get; set; }

        public DateTime PurchaseDate { get; set; }

        public int ItemCount { get; set; }
        public decimal TotalQuantity { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class CreateStockPurchaseModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Select supplier.")]
        public int SupplierId { get; set; }

        [StringLength(100)]
        public string? SupplierInvoiceNumber { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [Range(0, 999999999)]
        public decimal TaxAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool UpdateProductCostPrice { get; set; } = true;

        public List<CreateStockPurchaseItemModel> Items { get; set; } = new();
    }

    public class CreateStockPurchaseItemModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Select product.")]
        public int ProductId { get; set; }

        [Range(0.01, 999999999)]
        public decimal Quantity { get; set; }

        [Range(0, 999999999)]
        public decimal UnitCost { get; set; }
    }

    public class PurchaseProductOptionViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal CostPrice { get; set; }
        public string? UnitOfMeasure { get; set; }
    }
}