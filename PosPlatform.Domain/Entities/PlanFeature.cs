using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities
{
    public class PlanFeature : BaseEntity
    {
        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan? SubscriptionPlan { get; set; }

        public int FeatureId { get; set; }
        public Feature? Feature { get; set; }
    }
}