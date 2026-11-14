using LuxuryStore.Domain.Common;

namespace LuxuryStore.Domain.Entities;

/// <summary>
/// One image in a product's gallery (brief 7.1: "image gallery" on the
/// product detail page, large photography on listing grids).
///
/// Replaces the former ProductVariant.ImageUrls text[] column. A table of
/// its own because a luxury catalog needs more than a bare URL per image:
///   - AltText: accessibility and SEO — required, never empty.
///   - SortOrder: the gallery order is editorial, not insertion order. The
///     image with the lowest SortOrder is the product's primary image (the
///     one on listing cards), so there's no separate IsPrimary flag whose
///     "exactly one per product" invariant would need enforcing.
///   - Product-level by default: most photography (a suit on a model, a
///     watch on the wrist) applies to every size. VariantId is set only for
///     images specific to one variant (e.g. a different dial colour).
///
/// Path holds either an object path inside the product-images Supabase
/// Storage bucket ("products/rolex-submariner-date/01.jpg") or an absolute
/// https URL. Object paths are preferred: the public base URL is composed at
/// read time from configuration (IImageUrlResolver), so moving the bucket or
/// putting a CDN in front of it never requires rewriting rows.
/// </summary>
public class ProductImage : BaseEntity
{
    public string Path { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
}
