using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Domain.Entities;

namespace LuxuryStore.Application.Mapping;

/// <summary>
/// Hand-written entity -&gt; DTO conversions.
///
/// This intentionally does NOT use a mapping library (AutoMapper, Mapster,
/// etc.). AutoMapper moved to a commercial license for larger orgs in late
/// 2024, and while that doesn't affect a solo project, the bigger reason is
/// that at this size explicit mapping is simply easier to trust: no
/// reflection magic, no silently-null field because a navigation property
/// wasn't Include()'d, and anyone reading a service can see exactly how its
/// DTO gets built without opening a separate mapping-profile file. A mapping
/// library becomes worth its cost once the number of DTOs grows a lot and
/// the boilerplate here starts to hurt — Mapster (MIT-licensed, no
/// reflection at runtime — it source-generates the mapping code) would be
/// the natural upgrade at that point.
/// </summary>
public static class MappingExtensions
{
    public static BrandDto ToDto(this Brand brand) => new()
    {
        Id = brand.Id,
        Name = brand.Name,
        Slug = brand.Slug,
        LogoUrl = brand.LogoUrl,
        IsActive = brand.IsActive
    };

    public static CategoryDto ToDto(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Slug = category.Slug,
        Description = category.Description,
        IsActive = category.IsActive
    };

    // Expects variant.Product to be loaded (either via Include, or fixed up
    // manually after an insert/update — see ProductService) so EffectivePrice
    // can fall back to the parent product's BasePrice.
    public static ProductVariantDto ToDto(this ProductVariant variant) => new()
    {
        Id = variant.Id,
        Sku = variant.Sku,
        VariantLabel = variant.VariantLabel,
        EffectivePrice = variant.PriceOverride ?? variant.Product.BasePrice,
        StockQuantity = variant.StockQuantity,
        IsActive = variant.IsActive
    };

    public static ProductImageDto ToDto(this ProductImage image, IImageUrlResolver urls) => new()
    {
        Id = image.Id,
        Url = urls.Resolve(image.Path, image.AltText),
        AltText = image.AltText,
        SortOrder = image.SortOrder,
        VariantId = image.VariantId
    };

    // Full detail shape — expects Brand, Category, Variants and Images to be
    // loaded (see IProductRepository.GetByIdWithDetailsAsync / GetBySlugWithDetailsAsync).
    public static ProductDto ToDto(this Product product, IImageUrlResolver urls) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Slug = product.Slug,
        Description = product.Description,
        BasePrice = product.BasePrice,
        IsActive = product.IsActive,
        Brand = product.Brand.ToDto(),
        Category = product.Category.ToDto(),
        Images = product.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => i.ToDto(urls))
            .ToList(),
        Variants = product.Variants
            .OrderBy(v => v.CreatedAt) // insertion order: sizes are entered smallest-first
            .Select(v => v.ToDto())
            .ToList()
    };

    // Lightweight shape for catalog grids — expects Brand, Category and the
    // primary image to be loaded (IProductRepository.GetPagedAsync loads
    // only the first image per product, not the whole gallery).
    public static ProductListItemDto ToListItemDto(this Product product, IImageUrlResolver urls)
    {
        var primary = product.Images
            .OrderBy(i => i.VariantId.HasValue) // product-wide shots first
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();

        return new ProductListItemDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            BasePrice = product.BasePrice,
            BrandName = product.Brand.Name,
            CategoryName = product.Category.Name,
            PrimaryImageUrl = primary is null ? null : urls.Resolve(primary.Path, primary.AltText),
            PrimaryImageAltText = primary?.AltText
        };
    }

    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Role = user.Role.ToString()
    };
}
