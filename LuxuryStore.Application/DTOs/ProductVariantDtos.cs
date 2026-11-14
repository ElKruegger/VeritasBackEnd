namespace LuxuryStore.Application.DTOs;

// Validation rules: Validation/CatalogValidators.cs.

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
}

public class CreateProductVariantDto
{
    public string Sku { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }
}

public class UpdateProductVariantDto
{
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}
