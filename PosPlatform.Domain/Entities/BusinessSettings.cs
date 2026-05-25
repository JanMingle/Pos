namespace PosPlatform.Domain.Entities
{
    public class BusinessSettings
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public string BusinessName { get; set; } = string.Empty;
        public string BusinessType { get; set; } = "General Business";

        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public string CurrencyCode { get; set; } = "ZAR";
        public string CurrencySymbol { get; set; } = "R";

        public bool TaxEnabled { get; set; }
        public string TaxName { get; set; } = "VAT";
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

        public string ReceiptTitle { get; set; } = "Sales Receipt";
        public string? ReceiptFooterMessage { get; set; } = "Thank you for your purchase.";
        public string? ReturnPolicyText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}