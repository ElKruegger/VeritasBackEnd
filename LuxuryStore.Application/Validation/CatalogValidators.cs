using FluentValidation;
using LuxuryStore.Application.DTOs;

namespace LuxuryStore.Application.Validation;

// Max lengths mirror the column sizes in LuxuryStore.Infrastructure's
// Persistence/Configurations — keep both in sync when one changes.

public class CreateBrandDtoValidator : AbstractValidator<CreateBrandDto>
{
    public CreateBrandDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).ValidSlug(120);
        RuleFor(x => x.LogoUrl).OptionalHttpsUrl(500);
    }
}

public class UpdateBrandDtoValidator : AbstractValidator<UpdateBrandDto>
{
    public UpdateBrandDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LogoUrl).OptionalHttpsUrl(500);
    }
}

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).ValidSlug(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).ValidSlug(180);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.BasePrice).ValidPrice();
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.BasePrice).ValidPrice();
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public class CreateProductVariantDtoValidator : AbstractValidator<CreateProductVariantDto>
{
    public CreateProductVariantDtoValidator()
    {
        // SKUs end up on labels, invoices and (later) Stripe line items —
        // keep them to a conservative, case-insensitive-safe character set.
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("SKU may contain only letters, digits, '.', '_' and '-'.");

        RuleFor(x => x.VariantLabel).NotEmpty().MaximumLength(50);

        RuleFor(x => x.PriceOverride)
            .Must(price => price is null || (price > 0 && price <= ValidationRules.MaxPrice))
            .WithMessage($"Price override must be greater than 0 and at most {ValidationRules.MaxPrice}.");

        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductVariantDtoValidator : AbstractValidator<UpdateProductVariantDto>
{
    public UpdateProductVariantDtoValidator()
    {
        RuleFor(x => x.PriceOverride)
            .Must(price => price is null || (price > 0 && price <= ValidationRules.MaxPrice))
            .WithMessage($"Price override must be greater than 0 and at most {ValidationRules.MaxPrice}.");

        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public class CreateProductImageDtoValidator : AbstractValidator<CreateProductImageDto>
{
    public CreateProductImageDtoValidator()
    {
        RuleFor(x => x.Path)
            .NotEmpty()
            .MaximumLength(500)
            .Must(BeStoragePathOrHttpsUrl)
            .WithMessage("Path must be a bucket object path (e.g. 'products/my-product/01.jpg') or an absolute https URL.");

        RuleFor(x => x.AltText).NotEmpty().MaximumLength(200);

        RuleFor(x => x.SortOrder)
            .Must(order => order is null || order >= 0)
            .WithMessage("Sort order must be zero or greater.");
    }

    private static bool BeStoragePathOrHttpsUrl(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (ValidationRules.IsHttpsUrl(path))
            return true;

        // Relative object path: no scheme, no leading slash, no ".." segment
        // (so it can never be used to address something outside the bucket
        // once concatenated onto Storage:PublicBaseUrl), no backslashes.
        return !path.Contains("://")
               && !path.StartsWith('/')
               && !path.Contains('\\')
               && !path.Split('/').Any(segment => segment is "" or "." or "..");
    }
}

public class ProductQueryParamsValidator : AbstractValidator<ProductQueryParams>
{
    public ProductQueryParamsValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.MinPrice)
            .Must(min => min is null || min >= 0)
            .WithMessage("Minimum price cannot be negative.");

        RuleFor(x => x.MaxPrice)
            .Must((query, max) => max is null || query.MinPrice is null || max >= query.MinPrice)
            .WithMessage("Maximum price must be greater than or equal to the minimum price.");

        RuleFor(x => x.Search).MaximumLength(100);
    }
}
