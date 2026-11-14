using LuxuryStore.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace LuxuryStore.Infrastructure.Storage;

/// <summary>
/// Resolves a ProductImage.Path to a loadable URL:
///
///   1. Absolute http(s) URL  -> returned unchanged (externally hosted image).
///   2. Bucket object path    -> Storage:PublicBaseUrl + "/" + path.
///   3. Object path but no PublicBaseUrl configured -> a neutral placeholder
///      (placehold.co, off-white on near-black, serif — the brief's palette)
///      showing the image's alt text.
///
/// Case 3 is deliberate for local development: the seeded catalog renders
/// with tasteful, correctly proportioned placeholders before a single real
/// photo has been uploaded. As soon as PublicBaseUrl is set, the very same
/// rows point at the real bucket — no data change needed.
/// </summary>
public class ImageUrlResolver(IOptions<StorageOptions> options) : IImageUrlResolver
{
    // 4:5 portrait — the usual aspect ratio for editorial product photography.
    private const string PlaceholderBase = "https://placehold.co/1200x1500/F5F5F2/0A0A0A/png";

    private readonly string? _baseUrl = options.Value.PublicBaseUrl?.TrimEnd('/');

    public string Resolve(string path, string altText)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttps || absolute.Scheme == Uri.UriSchemeHttp))
            return path;

        if (!string.IsNullOrWhiteSpace(_baseUrl))
            return $"{_baseUrl}/{EscapePath(path)}";

        return $"{PlaceholderBase}?font=playfair-display&text={Uri.EscapeDataString(altText)}";
    }

    // Escapes each segment (spaces, accents) but keeps the "/" separators.
    private static string EscapePath(string path) =>
        string.Join('/', path.TrimStart('/').Split('/').Select(Uri.EscapeDataString));
}
