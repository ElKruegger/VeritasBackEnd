using System.ComponentModel.DataAnnotations;

namespace LuxuryStore.Application.DTOs;

// DTOs exist so the API never serializes an EF Core entity directly (brief
// section 3): a Brand entity could later grow an internal-only column, or a
// full Products navigation collection, that we never want leaking into a
// public JSON response — the DTO is the contract we actually promise to callers.

public class BrandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBrandDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }
}

public class UpdateBrandDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;
}
