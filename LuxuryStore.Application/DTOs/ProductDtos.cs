namespace LuxuryStore.Application.DTOs;

// Validation rules: Validation/CatalogValidators.cs.

/// <summary>Lightweight shape for catalog/listing grids — no description, no full variant list.</summary>
public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    // The product's primary image (lowest SortOrder), already resolved to a
    // full URL — null only if the product has no images yet.
    public string? PrimaryImageUrl { get; set; }
    public string? PrimaryImageAltText { get; set; }

    // False when no active variant has stock: lets the grid show a
    // "Sold out" badge without loading the product.
    public bool InStock { get; set; }
}

/// <summary>Full shape for the product detail page: brand, category, gallery and every variant.</summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; }
    public BrandDto Brand { get; set; } = null!;
    public CategoryDto Category { get; set; } = null!;

    // Whole gallery in display order. Images with a VariantId belong to one
    // variant only — the frontend shows them when that variant is selected.
    public List<ProductImageDto> Images { get; set; } = [];
    public List<ProductVariantDto> Variants { get; set; } = [];
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public Guid BrandId { get; set; }
    public Guid CategoryId { get; set; }
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid BrandId { get; set; }
    public Guid CategoryId { get; set; }
}

/// <summary>
/// Query parameters for GET /api/v1/products — mirrors the category-page
/// filters from brief 7.1 (brand, price range) plus pagination.
/// </summary>
public class ProductQueryParams
{
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Search { get; set; }

    // Variant labels exactly as shown by GET /products/filters ("UK 8",
    // "EU 48 Regular"). Repeat the parameter for several: ?sizes=UK 8&sizes=UK 9.
    // A product matches when it has an active variant in any of them.
    public List<string>? Sizes { get; set; }

    // Only products that can be bought right now: an active variant with
    // stock (in one of the requested sizes, when Sizes is set).
    public bool InStock { get; set; }

    public ProductSort Sort { get; set; } = ProductSort.Name;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Sort order of GET /api/v1/products (?sort=priceAsc). Prices are the
/// product's BasePrice, the same value the price filter uses.
/// </summary>
public enum ProductSort
{
    Name,
    PriceAsc,
    PriceDesc,
    Newest
}

/// <summary>
/// Query of GET /api/v1/products/filters: the slice of the catalog the
/// filter sidebar is built for (usually one category page).
/// </summary>
public class ProductFilterQuery
{
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string? Search { get; set; }
}

/// <summary>
/// What can be filtered on within a ProductFilterQuery slice — only
/// values that actually exist there, so the frontend never offers a
/// filter that returns nothing.
/// </summary>
public class ProductFiltersDto
{
    // Ignores the BrandId of the query on purpose: once a brand is
    // selected, the sidebar must still list the others to switch to.
    public List<BrandDto> Brands { get; set; } = [];

    // In natural order ("UK 7" before "UK 10").
    public List<string> Sizes { get; set; } = [];

    // Null when the slice has no products.
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

/// <summary>Query of GET /api/v1/products/{id}/related.</summary>
public class RelatedProductsQuery
{
    public int Limit { get; set; } = 4;
}
