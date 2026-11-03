using LuxuryStore.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace LuxuryStore.UnitTests.Infrastructure;

public class ImageUrlResolverTests
{
    private static ImageUrlResolver Resolver(string? baseUrl) =>
        new(Options.Create(new StorageOptions { PublicBaseUrl = baseUrl }));

    private const string Bucket = "https://abc.supabase.co/storage/v1/object/public/product-images";

    [Theory]
    [InlineData("https://images.example.com/rolex.jpg")]
    [InlineData("http://legacy.example.com/rolex.jpg")]
    public void Absolute_urls_are_returned_unchanged(string url)
    {
        Resolver(Bucket).Resolve(url, "alt").ShouldBe(url);
    }

    [Theory]
    [InlineData(Bucket)]
    [InlineData(Bucket + "/")] // trailing slash in configuration
    public void Bucket_paths_are_joined_to_the_public_base_url(string baseUrl)
    {
        Resolver(baseUrl).Resolve("products/rolex-submariner-date/01.jpg", "alt")
            .ShouldBe(Bucket + "/products/rolex-submariner-date/01.jpg");
    }

    [Fact]
    public void Path_segments_are_url_escaped_but_slashes_are_kept()
    {
        Resolver(Bucket).Resolve("products/grand seiko/01.jpg", "alt")
            .ShouldBe(Bucket + "/products/grand%20seiko/01.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Without_a_bucket_url_a_placeholder_showing_the_alt_text_is_returned(string? baseUrl)
    {
        var url = Resolver(baseUrl).Resolve("products/rolex/01.jpg", "Rolex Submariner");

        url.ShouldStartWith("https://placehold.co/");
        url.ShouldContain("text=Rolex%20Submariner");
    }
}
