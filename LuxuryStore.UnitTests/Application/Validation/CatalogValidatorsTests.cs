using FluentValidation.TestHelper;
using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Validation;

namespace LuxuryStore.UnitTests.Application.Validation;

public class SlugRuleTests
{
    private readonly CreateBrandDtoValidator _validator = new();

    [Theory]
    [InlineData("rolex")]
    [InlineData("grand-seiko")]
    [InlineData("crockett-jones")]
    [InlineData("tom-ford-2026")]
    public void Valid_slugs_pass(string slug)
    {
        var dto = new CreateBrandDto { Name = "Brand", Slug = slug };

        _validator.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    [Theory]
    [InlineData("Grand Seiko")]   // uppercase and space
    [InlineData("grand_seiko")]   // underscore
    [InlineData("-rolex")]        // leading hyphen
    [InlineData("rolex-")]        // trailing hyphen
    [InlineData("grand--seiko")]  // double hyphen
    [InlineData("")]
    public void Invalid_slugs_are_rejected(string slug)
    {
        var dto = new CreateBrandDto { Name = "Brand", Slug = slug };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Theory]
    [InlineData("http://cdn.example.com/logo.png")] // not https
    [InlineData("logo.png")]                        // not absolute
    public void Logo_url_must_be_absolute_https(string logoUrl)
    {
        var dto = new CreateBrandDto { Name = "Brand", Slug = "brand", LogoUrl = logoUrl };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.LogoUrl);
    }
}

public class CreateProductDtoValidatorTests
{
    private readonly CreateProductDtoValidator _validator = new();

    private static CreateProductDto ValidDto() => new()
    {
        Name = "Submariner Date 41 mm",
        Slug = "rolex-submariner-date",
        BasePrice = 10950m,
        BrandId = Guid.NewGuid(),
        CategoryId = Guid.NewGuid()
    };

    [Fact]
    public void Valid_product_passes()
    {
        _validator.TestValidate(ValidDto()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100_000_000)] // exceeds numeric(10,2)
    public void Out_of_range_price_is_rejected(decimal price)
    {
        var dto = ValidDto();
        dto.BasePrice = price;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.BasePrice);
    }

    [Fact]
    public void Empty_brand_and_category_ids_are_rejected()
    {
        var dto = ValidDto();
        dto.BrandId = Guid.Empty;
        dto.CategoryId = Guid.Empty;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.BrandId);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }
}

public class CreateProductVariantDtoValidatorTests
{
    private readonly CreateProductVariantDtoValidator _validator = new();

    [Theory]
    [InlineData("ROL-126610LN")]
    [InlineData("hb.shirt_15")]
    public void Valid_skus_pass(string sku)
    {
        var dto = new CreateProductVariantDto { Sku = sku, VariantLabel = "41 mm" };

        _validator.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Sku);
    }

    [Theory]
    [InlineData("ROL 126610")] // space
    [InlineData("ROL/126610")] // slash
    public void Invalid_skus_are_rejected(string sku)
    {
        var dto = new CreateProductVariantDto { Sku = sku, VariantLabel = "41 mm" };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Fact]
    public void Null_price_override_is_allowed()
    {
        var dto = new CreateProductVariantDto { Sku = "SKU-1", VariantLabel = "M", PriceOverride = null };

        _validator.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.PriceOverride);
    }

    [Fact]
    public void Zero_price_override_and_negative_stock_are_rejected()
    {
        var dto = new CreateProductVariantDto { Sku = "SKU-1", VariantLabel = "M", PriceOverride = 0m, StockQuantity = -1 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.PriceOverride);
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }
}

public class CreateProductImageDtoValidatorTests
{
    private readonly CreateProductImageDtoValidator _validator = new();

    [Theory]
    [InlineData("products/rolex-submariner-date/01.jpg")]
    [InlineData("https://images.example.com/rolex.jpg")]
    public void Bucket_paths_and_https_urls_pass(string path)
    {
        var dto = new CreateProductImageDto { Path = path, AltText = "Rolex Submariner" };

        _validator.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Path);
    }

    [Theory]
    [InlineData("/products/a.jpg")]          // leading slash
    [InlineData("products/../secrets.jpg")]  // path traversal
    [InlineData("products//a.jpg")]          // empty segment
    [InlineData("products\\a.jpg")]          // backslash
    [InlineData("ftp://example.com/a.jpg")]  // other scheme
    [InlineData("")]
    public void Unsafe_or_malformed_paths_are_rejected(string path)
    {
        var dto = new CreateProductImageDto { Path = path, AltText = "Alt" };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Path);
    }

    [Fact]
    public void Alt_text_is_required()
    {
        var dto = new CreateProductImageDto { Path = "products/a/01.jpg", AltText = "" };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.AltText);
    }
}

public class ProductQueryParamsValidatorTests
{
    private readonly ProductQueryParamsValidator _validator = new();

    [Fact]
    public void Default_query_passes()
    {
        _validator.TestValidate(new ProductQueryParams()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Page_size_outside_1_to_100_is_rejected(int pageSize)
    {
        _validator.TestValidate(new ProductQueryParams { PageSize = pageSize })
            .ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Max_price_below_min_price_is_rejected()
    {
        var query = new ProductQueryParams { MinPrice = 500, MaxPrice = 100 };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.MaxPrice);
    }
}
