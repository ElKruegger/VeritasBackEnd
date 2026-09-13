using System.ComponentModel.DataAnnotations;

namespace LuxuryStore.Application.DTOs;

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;

    // PriceOverride if set, otherwise the parent Product's BasePrice — the
    // frontend should never have to know which one applied, so the mapping
    // resolves it once here (see Mapping/MappingExtensions.cs).
    public decimal EffectivePrice { get; set; }

    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public List<string> ImageUrls { get; set; } = [];
}

public class CreateProductVariantDto
{
    [Required, MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string VariantLabel { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal? PriceOverride { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public List<string> ImageUrls { get; set; } = [];
}

public class UpdateProductVariantDto
{
    [Range(0, double.MaxValue)]
    public decimal? PriceOverride { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public List<string> ImageUrls { get; set; } = [];
}
