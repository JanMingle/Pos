namespace PosPlatform.Web.Models.Reports
{
    public class ReportsDashboardViewModel
    {
        public decimal TotalSales { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageSale { get; set; }

        public decimal TotalDiscounts { get; set; }
        public decimal TotalTax { get; set; }

        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal EftSales { get; set; }

        public int TotalCustomers { get; set; }
        public int CustomersWithPurchases { get; set; }

        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public decimal StockValue { get; set; }

        public decimal TotalCashDifference { get; set; }

        public List<ReportPaymentMethodRow> PaymentMethods { get; set; } = new();
        public List<ReportTopItemRow> TopItems { get; set; } = new();
        public List<ReportCashierRow> Cashiers { get; set; } = new();
        public List<ReportLowStockRow> LowStock { get; set; } = new();
        public List<ReportCustomerRow> TopCustomers { get; set; } = new();
        public List<ReportShiftRow> Shifts { get; set; } = new();
        public List<ReportRecentSaleRow> RecentSales { get; set; } = new();
    }

    public class ReportPaymentMethodRow
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int Transactions { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ReportTopItemRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ReportCashierRow
    {
        public string CashierName { get; set; } = string.Empty;
        public int Transactions { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class ReportLowStockRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal QuantityInStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal StockValue { get; set; }
    }

    public class ReportCustomerRow
    {
        public string CustomerName { get; set; } = string.Empty;
        public int Purchases { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }

    public class ReportShiftRow
    {
        public string CashierName { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal CashSales { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ClosingCash { get; set; }
        public decimal CashDifference { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ReportRecentSaleRow
    {
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}