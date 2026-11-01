using LuxuryStore.Domain.Common;

namespace LuxuryStore.Domain.Entities;

/// <summary>
/// A purchasable size/color/configuration of a Product (what the "variant
/// or size selector" on the product page in brief 7.1 is choosing between).
/// Each variant has its own SKU and stock count, and can optionally override
/// the product's base price (e.g. a larger size costing more).
/// </summary>
public class ProductVariant : BaseEntity
{
    public string Sku { get; set; } = string.Empty;

    // e.g. "42", "Black / M", "41mm Steel" — free-text on purpose since the
    // label format differs a lot between clothing, watches and footwear.
    public string VariantLabel { get; set; } = string.Empty;

    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Variant-specific images only; product-wide photography lives on
    // Product.Images with VariantId = null (see ProductImage).
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
