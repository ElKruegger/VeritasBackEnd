namespace LuxuryStore.Infrastructure.Storage;

/// <summary>
/// Bound from the "Storage" configuration section.
///
/// PublicBaseUrl is the public URL of the product-images bucket, e.g.
///   https://&lt;project-ref&gt;.supabase.co/storage/v1/object/public/product-images
/// Not a secret (public buckets are world-readable by design), so it can
/// live in appsettings; Render sets it as Storage__PublicBaseUrl.
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    public string? PublicBaseUrl { get; set; }
}
