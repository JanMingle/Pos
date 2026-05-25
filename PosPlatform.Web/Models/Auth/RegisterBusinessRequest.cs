namespace PosPlatform.Web.Models.Auth
{
    public class RegisterBusinessRequest
    {
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessType { get; set; } = "General Business";
        public string? BusinessEmail { get; set; }
        public string? BusinessPhone { get; set; }
        public string? BusinessAddress { get; set; }

        public string? BranchName { get; set; }
        public string? BranchCode { get; set; }

        public string OwnerFullName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public int? SubscriptionPlanId { get; set; }

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
    }
}