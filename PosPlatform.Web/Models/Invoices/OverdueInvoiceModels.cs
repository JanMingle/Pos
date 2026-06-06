namespace PosPlatform.Web.Models.Invoices
{
    public class OverdueInvoiceViewModel
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public string? QuoteNumber { get; set; }

        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }

        public int DaysOverdue { get; set; }

        public string CustomerName { get; set; } = "Walk-in / Unsaved";
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }

        public string Status { get; set; } = "Unpaid";
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class OverdueInvoiceSummaryViewModel
    {
        public int Count { get; set; }
        public decimal TotalBalanceDue { get; set; }
        public decimal TotalInvoiceValue { get; set; }
        public decimal TotalPaid { get; set; }

        public int MostOverdueDays { get; set; }
    }
}