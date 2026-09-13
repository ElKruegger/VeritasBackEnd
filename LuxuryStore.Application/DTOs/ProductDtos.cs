using System.ComponentModel.DataAnnotations;

namespace LuxuryStore.Application.DTOs;

/// <summary>Lightweight shape for catalog/listing grids — no description, no full variant list.</summary>
public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? PrimaryImageUrl { get; set; }
}

/// <summary>Full shape for the product detail page: brand, category and every variant.</summary>
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
    public List<ProductVariantDto> Variants { get; set; } = [];
}

public class CreateProductDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(180)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal BasePrice { get; set; }

    [Required]
    public Guid BrandId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }
}

public class UpdateProductDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public Guid BrandId { get; set; }

    [Required]
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

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
