namespace PosPlatform.Web.Models.Reports
{
    public class ReportsDashboardViewModel
    {
        public decimal GrossSales { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal NetSales { get; set; }

        // Keep this for older UI references.
        public decimal TotalSales { get; set; }

        public int TotalTransactions { get; set; }
        public decimal AverageSale { get; set; }

        public int RefundCount { get; set; }
        public int VoidCount { get; set; }

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
        public List<ReportRefundedItemRow> TopRefundedItems { get; set; } = new();
        public List<ReportCashierRow> Cashiers { get; set; } = new();
        public List<ReportRefundCashierRow> RefundCashiers { get; set; } = new();
        public List<ReportLowStockRow> LowStock { get; set; } = new();
        public List<ReportCustomerRow> TopCustomers { get; set; } = new();
        public List<ReportShiftRow> Shifts { get; set; } = new();
        public List<ReportRecentSaleRow> RecentSales { get; set; } = new();
        public List<ReportRecentRefundRow> RecentRefunds { get; set; } = new();

        public decimal CostOfGoodsSold { get; set; }
        public decimal RefundedCost { get; set; }
        public decimal NetCostOfGoodsSold { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossProfitMargin { get; set; }

        public int PurchaseCount { get; set; }
        public decimal PurchaseSubtotal { get; set; }
        public decimal PurchaseTax { get; set; }
        public decimal PurchaseTotal { get; set; }

        public List<ReportSupplierPurchaseRow> TopSuppliers { get; set; } = new();

        public decimal TotalExpenses { get; set; }
        public decimal ExpenseTax { get; set; }
        public decimal NetProfit { get; set; }
        public decimal NetProfitMargin { get; set; }
        public int ExpenseCount { get; set; }

        public List<ReportExpenseCategoryRow> ExpensesByCategory { get; set; } = new();
        public List<ReportRecentExpenseRow> RecentExpenses { get; set; } = new();
    }



    public class ReportPaymentMethodRow
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int Transactions { get; set; }
        public int Refunds { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal NetAmount { get; set; }
    }

    public class ReportTopItemRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ReportRefundedItemRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal QuantityReturned { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class ReportCashierRow
    {
        public string CashierName { get; set; } = string.Empty;
        public int Transactions { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class ReportRefundCashierRow
    {
        public string CashierName { get; set; } = string.Empty;
        public int Refunds { get; set; }
        public decimal TotalRefunded { get; set; }
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
        public decimal GrossSpent { get; set; }
        public decimal Refunded { get; set; }
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
        public string Status { get; set; } = string.Empty;
    }

    public class ReportRecentRefundRow
    {
        public string ReturnNumber { get; set; } = string.Empty;
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public string RefundMethod { get; set; } = string.Empty;
        public string ReturnedByName { get; set; } = string.Empty;
        public decimal TotalRefundAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}

public class ReportSupplierPurchaseRow
{
    public string SupplierName { get; set; } = string.Empty;
    public int Purchases { get; set; }
    public decimal TotalSpent { get; set; }
}

public class ReportExpenseCategoryRow
{
    public string CategoryName { get; set; } = string.Empty;
    public int ExpenseCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ReportRecentExpenseRow
{
    public string ExpenseNumber { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}