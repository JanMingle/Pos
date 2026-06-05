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

        public decimal CashIn { get; set; }
        public decimal CashOut { get; set; }

        public decimal ExpectedCash { get; set; }
        public decimal CashDifference { get; set; }

        public string Status { get; set; } = "Open";
        public string? OpeningNotes { get; set; }
        public string? ClosingNotes { get; set; }

        public List<CashierShiftCashMovementViewModel> CashMovements { get; set; } = new();
    }

    public class CashierShiftCashMovementViewModel
    {
        public int Id { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
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

    public class CashMovementModel
    {
        [Required]
        public string MovementType { get; set; } = "Cash In";

        [Range(0.01, 999999999, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(120)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}