using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Settings
{
    public class BusinessSettingsFormModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string BusinessType { get; set; } = "General Business";

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Required]
        [StringLength(10)]
        public string CurrencyCode { get; set; } = "ZAR";

        [Required]
        [StringLength(10)]
        public string CurrencySymbol { get; set; } = "R";

        public bool TaxEnabled { get; set; }

        [Required]
        [StringLength(50)]
        public string TaxName { get; set; } = "VAT";

        [Range(0, 100)]
        public decimal TaxRate { get; set; }

        public bool ProductsEnabled { get; set; } = true;
        public bool StockTrackingEnabled { get; set; } = true;
        public bool ServicesEnabled { get; set; }
        public bool AppointmentsEnabled { get; set; }
        public bool CustomersEnabled { get; set; } = true;
        public bool AgeRestrictedProductsEnabled { get; set; }

        public bool AllowNegativeStock { get; set; }
        public bool RequireCustomerForSale { get; set; }
        public bool AllowDiscounts { get; set; } = true;

        [Required]
        [StringLength(100)]
        public string ReceiptTitle { get; set; } = "Sales Receipt";

        [StringLength(300)]
        public string? ReceiptFooterMessage { get; set; } = "Thank you for your purchase.";

        [StringLength(500)]
        public string? ReturnPolicyText { get; set; }
    }

    public class BusinessTypeOptionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}