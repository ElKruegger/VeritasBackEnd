using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Mapping;
using LuxuryStore.Domain.Entities;
using LuxuryStore.Domain.Enums;

namespace LuxuryStore.UnitTests.Application.Mapping;

public class MappingExtensionsTests
{
    private readonly IImageUrlResolver _urls = Substitute.For<IImageUrlResolver>();

    public MappingExtensionsTests()
    {
        _urls.Resolve(Arg.Any<string>(), Arg.Any<string>()).Returns(call => "url:" + call.ArgAt<string>(0));
    }

    private static Product ProductWithGraph() => new()
    {
        Name = "714 Folding Sunglasses",
        Slug = "persol-714-folding-sunglasses",
        BasePrice = 480m,
        Brand = new Brand { Name = "Persol", Slug = "persol" },
        Category = new Category { Name = "Accessories", Slug = "accessories" }
    };

    [Fact]
    public void Effective_price_falls_back_to_the_product_base_price()
    {
        var product = ProductWithGraph();
        var variant = new ProductVariant { Sku = "PER-714-HAV", Product = product, PriceOverride = null };

        variant.ToDto().EffectivePrice.ShouldBe(480m);
    }

    [Fact]
    public void Effective_price_uses_the_variant_override_when_set()
    {
        var product = ProductWithGraph();
        var variant = new ProductVariant { Sku = "PER-714-BLK", Product = product, PriceOverride = 540m };

        variant.ToDto().EffectivePrice.ShouldBe(540m);
    }

    [Fact]
    public void Product_gallery_is_returned_in_sort_order()
    {
        var product = ProductWithGraph();
        product.Images.Add(new ProductImage { Path = "c.jpg", AltText = "c", SortOrder = 2 });
        product.Images.Add(new ProductImage { Path = "a.jpg", AltText = "a", SortOrder = 0 });
        product.Images.Add(new ProductImage { Path = "b.jpg", AltText = "b", SortOrder = 1 });

        var dto = product.ToDto(_urls);

        dto.Images.Select(i => i.Url).ShouldBe(["url:a.jpg", "url:b.jpg", "url:c.jpg"]);
    }

    [Fact]
    public void Listing_primary_image_prefers_product_level_shots_over_variant_shots()
    {
        var product = ProductWithGraph();
        product.Images.Add(new ProductImage { Path = "variant.jpg", AltText = "variant", SortOrder = 0, VariantId = Guid.NewGuid() });
        product.Images.Add(new ProductImage { Path = "product.jpg", AltText = "product", SortOrder = 5 });

        var dto = product.ToListItemDto(_urls);

        dto.PrimaryImageUrl.ShouldBe("url:product.jpg");
        dto.PrimaryImageAltText.ShouldBe("product");
    }

    [Fact]
    public void Listing_item_without_images_has_no_primary_image()
    {
        var dto = ProductWithGraph().ToListItemDto(_urls);

        dto.PrimaryImageUrl.ShouldBeNull();
        dto.BrandName.ShouldBe("Persol");
        dto.CategoryName.ShouldBe("Accessories");
    }

    [Fact]
    public void User_role_is_exposed_by_name()
    {
        var user = new User { Email = "admin@example.com", Role = UserRole.Admin };

        user.ToDto().Role.ShouldBe("Admin");
    }
}
