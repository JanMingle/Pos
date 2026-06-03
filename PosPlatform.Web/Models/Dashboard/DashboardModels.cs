namespace PosPlatform.Web.Models.Dashboard
{
    public class DashboardViewModel
    {
        public int? SelectedBranchId { get; set; }
        public string SelectedBranchName { get; set; } = "All Branches";
        public List<DashboardBranchOptionRow> Branches { get; set; } = new();

        public decimal TodayGrossSales { get; set; }
        public decimal TodayRefunds { get; set; }
        public decimal TodayNetSales { get; set; }

        public decimal TodayCostOfGoods { get; set; }
        public decimal TodayGrossProfit { get; set; }

        public decimal TodayExpenses { get; set; }
        public decimal TodayNetProfit { get; set; }

        public int TodayTransactionCount { get; set; }
        public int TodayRefundCount { get; set; }
        public int TodayExpenseCount { get; set; }

        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }

        public DashboardShiftViewModel? OpenShift { get; set; }

        public List<DashboardRecentSaleRow> RecentSales { get; set; } = new();
        public List<DashboardRecentRefundRow> RecentRefunds { get; set; } = new();
        public List<DashboardRecentExpenseRow> RecentExpenses { get; set; } = new();
        public List<DashboardLowStockRow> LowStockItems { get; set; } = new();
    }

    public class DashboardBranchOptionRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsMainBranch { get; set; }
    }

    public class DashboardShiftViewModel
    {
        public int Id { get; set; }
        public DateTime OpenedAt { get; set; }

        public decimal OpeningCash { get; set; }
        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal EftSales { get; set; }
        public decimal TotalSales { get; set; }
        public decimal ExpectedCash { get; set; }
    }

    public class DashboardRecentSaleRow
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class DashboardRecentRefundRow
    {
        public string ReturnNumber { get; set; } = string.Empty;
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public decimal TotalRefundAmount { get; set; }
    }

    public class DashboardRecentExpenseRow
    {
        public string ExpenseNumber { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DashboardLowStockRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal QuantityInStock { get; set; }
        public decimal ReorderLevel { get; set; }
    }
}