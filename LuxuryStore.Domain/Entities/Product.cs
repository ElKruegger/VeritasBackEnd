using LuxuryStore.Domain.Common;

namespace LuxuryStore.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Single currency (USD) for v1 — see brief section 11 (multi-currency is out of scope).
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
