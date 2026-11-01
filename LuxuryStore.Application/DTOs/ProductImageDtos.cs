namespace LuxuryStore.Application.DTOs;

// Validation rules: Validation/CatalogValidators.cs.

public class ProductImageDto
{
    public Guid Id { get; set; }

    // Always a full, directly usable URL — storage object paths are resolved
    // against Storage:PublicBaseUrl by IImageUrlResolver before they leave
    // the API, so the frontend never builds Supabase URLs itself.
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? VariantId { get; set; }
}

/// <summary>
/// Registers an image that already exists in the product-images bucket (or
/// at an external https URL). The binary upload itself happens in Supabase
/// Storage (dashboard or the frontend's signed-upload flow, later) — this
/// API only stores the metadata, so it never has to proxy large files.
/// </summary>
public class CreateProductImageDto
{
    // Object path inside the bucket, e.g. "products/rolex-submariner-date/01.jpg",
    // or an absolute https URL.
    public string Path { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;

    // Optional: appended after the current last image when omitted.
    public int? SortOrder { get; set; }

    // Optional: ties the image to one variant of this product.
    public Guid? VariantId { get; set; }
}
