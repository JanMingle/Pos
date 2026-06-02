using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Expenses
{
    public class ExpenseCategoryListItemViewModel
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int ExpenseCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class ExpenseCategoryFormModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(120)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ExpenseCategoryOptionViewModel
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class ExpenseListItemViewModel
    {
        public int Id { get; set; }

        public string ExpenseNumber { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }

        public string CategoryName { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public string? ReferenceNumber { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ExpenseFormModel
    {
        public int? Id { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Select expense category.")]
        public int ExpenseCategoryId { get; set; }

        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [StringLength(150)]
        public string? VendorName { get; set; }

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "Cash";

        [Range(0.01, 999999999, ErrorMessage = "Subtotal must be greater than zero.")]
        public decimal Subtotal { get; set; }

        [Range(0, 999999999)]
        public decimal TaxAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}