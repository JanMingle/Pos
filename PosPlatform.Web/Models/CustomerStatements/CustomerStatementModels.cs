namespace PosPlatform.Web.Models.CustomerStatements
{
    public class CustomerStatementCustomerOptionViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string CustomerType { get; set; } = "Individual";
    }

    public class CustomerStatementViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public decimal TotalInvoices { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal BalanceDue { get; set; }

        public decimal TotalSales { get; set; }
        public int InvoiceCount { get; set; }
        public int PaymentCount { get; set; }
        public int SalesCount { get; set; }

        public List<CustomerStatementLineViewModel> Lines { get; set; } = new();
        public List<CustomerSalesHistoryViewModel> SalesHistory { get; set; } = new();
    }

    public class CustomerStatementLineViewModel
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class CustomerSalesHistoryViewModel
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeAmount { get; set; }
    }
}