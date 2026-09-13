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

    // Npgsql's EF Core provider maps List<string> to a native Postgres
    // array column (text[]) by convention, so the gallery images for this
    // variant live here without a separate ProductImage table.
    public List<string> ImageUrls { get; set; } = [];

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
