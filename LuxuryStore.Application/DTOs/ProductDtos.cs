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
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
