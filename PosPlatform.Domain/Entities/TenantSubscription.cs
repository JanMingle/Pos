using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities
{
    public class TenantSubscription : BaseEntity
    {
        public int TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan? SubscriptionPlan { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsTrial { get; set; } = false;
    }
}