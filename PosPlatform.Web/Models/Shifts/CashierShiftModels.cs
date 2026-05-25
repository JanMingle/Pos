using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Shifts
{
    public class CashierShiftViewModel
    {
        public int Id { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string BranchName { get; set; } = "-";

        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public decimal OpeningCash { get; set; }
        public decimal ClosingCash { get; set; }

        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal EftSales { get; set; }
        public decimal TotalSales { get; set; }

        public decimal ExpectedCash { get; set; }
        public decimal CashDifference { get; set; }

        public string Status { get; set; } = "Open";
        public string? OpeningNotes { get; set; }
        public string? ClosingNotes { get; set; }
    }

    public class OpenShiftModel
    {
        [Range(0, 999999999)]
        public decimal OpeningCash { get; set; }

        [StringLength(300)]
        public string? OpeningNotes { get; set; }
    }

    public class CloseShiftModel
    {
        [Range(0, 999999999)]
        public decimal ClosingCash { get; set; }

        [StringLength(300)]
        public string? ClosingNotes { get; set; }
    }
}