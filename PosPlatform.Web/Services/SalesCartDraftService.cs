using PosPlatform.Web.Models.Sales;

namespace PosPlatform.Web.Services;

public class SalesCartDraftService
{
    public List<SaleCartLineViewModel> Cart { get; } = new();

    public string PaymentMethod { get; set; } = "Cash";
    public decimal DiscountAmount { get; set; }
    public decimal AmountPaid { get; set; }

    public int? SelectedCustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? SaleNotes { get; set; }

    public bool AgeRestrictionConfirmed { get; set; }
    public DateTime? LastUpdatedAt { get; set; }

    public bool HasDraft =>
        Cart.Count > 0 ||
        DiscountAmount > 0 ||
        AmountPaid > 0 ||
        SelectedCustomerId.HasValue ||
        !string.IsNullOrWhiteSpace(CustomerName) ||
        !string.IsNullOrWhiteSpace(CustomerPhone) ||
        !string.IsNullOrWhiteSpace(SaleNotes);

    public void Touch()
    {
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        Cart.Clear();

        PaymentMethod = "Cash";
        DiscountAmount = 0;
        AmountPaid = 0;

        SelectedCustomerId = null;
        CustomerName = null;
        CustomerPhone = null;
        SaleNotes = null;

        AgeRestrictionConfirmed = false;
        LastUpdatedAt = null;
    }
}