namespace PosPlatform.Domain.Common
{
    public abstract class TenantEntity : BaseEntity
    {
        public int TenantId { get; set; }
    }
}