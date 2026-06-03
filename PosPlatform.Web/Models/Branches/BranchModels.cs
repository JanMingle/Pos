using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Web.Models.Branches
{
    public class BranchListItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? BranchCode { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        public bool IsMainBranch { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public int SalesCount { get; set; }
        public decimal SalesTotal { get; set; }

        public int PurchaseCount { get; set; }
        public decimal PurchaseTotal { get; set; }

        public int ExpenseCount { get; set; }
        public decimal ExpenseTotal { get; set; }
    }

    public class BranchFormModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? BranchCode { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsMainBranch { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class BranchOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}