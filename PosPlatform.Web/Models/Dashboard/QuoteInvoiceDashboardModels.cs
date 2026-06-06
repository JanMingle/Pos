namespace PosPlatform.Web.Models.Dashboard
{
    public class QuoteInvoiceDashboardViewModel
    {
        public string CurrencySymbol { get; set; } = "R";

        public int QuoteCount { get; set; }
        public int AcceptedQuoteCount { get; set; }
        public decimal QuoteValue { get; set; }

        public int InvoiceCount { get; set; }
        public decimal InvoiceValue { get; set; }

        public int UnpaidInvoiceCount { get; set; }
        public int PartiallyPaidInvoiceCount { get; set; }
        public int PaidInvoiceCount { get; set; }

        public decimal OutstandingBalance { get; set; }

        public int OverdueInvoiceCount { get; set; }
        public decimal OverdueBalance { get; set; }

        public decimal PaymentsReceivedThisMonth { get; set; }

        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
    }
}