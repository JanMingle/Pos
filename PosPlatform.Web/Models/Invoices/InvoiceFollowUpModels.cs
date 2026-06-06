using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Invoices
{
    public class InvoiceFollowUpItemViewModel
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
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

        public string FollowUpStatus { get; set; } = "Not Started";
        public DateTime? LastFollowUpAt { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string? FollowUpNotes { get; set; }
        public int FollowUpCount { get; set; }
    }

    public class UpdateInvoiceFollowUpModel
    {
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(50)]
        public string FollowUpStatus { get; set; } = "Contacted";

        public DateTime? NextFollowUpDate { get; set; }

        [StringLength(800)]
        public string? FollowUpNotes { get; set; }
    }

    public class InvoiceFollowUpSummaryViewModel
    {
        public int TotalOutstanding { get; set; }
        public int DueForFollowUp { get; set; }
        public int PromisedToPay { get; set; }
        public int Escalated { get; set; }

        public decimal OutstandingBalance { get; set; }
    }
}