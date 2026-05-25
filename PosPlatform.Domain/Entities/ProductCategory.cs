using PosPlatform.Domain.Common;

namespace PosPlatform.Domain.Entities
{
    public class ProductCategory : TenantEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}