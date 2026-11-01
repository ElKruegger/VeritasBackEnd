using FluentValidation;

namespace LuxuryStore.Application.Validation;

/// <summary>
/// Rules shared by more than one validator, so "what is a valid slug" or
/// "what is a valid price" is defined exactly once.
/// </summary>
public static class ValidationRules
{
    // Lowercase letters/digits separated by single hyphens: "grand-seiko",
    // "rolex-submariner-date". No leading/trailing/double hyphens, so a slug
    // is always safe to drop into a URL path segment as-is.
    public const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

    // Matches the numeric(10,2) columns (ProductConfiguration /
    // ProductVariantConfiguration): anything larger would fail at the
    // database with a 500 instead of a clear 400 here.
    public const decimal MaxPrice = 99_999_999.99m;

    public static IRuleBuilderOptions<T, string> ValidSlug<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.NotEmpty()
            .MaximumLength(maxLength)
            .Matches(SlugPattern)
            .WithMessage("Slug must contain only lowercase letters, digits and single hyphens (e.g. 'grand-seiko').");

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0m)
            .LessThanOrEqualTo(MaxPrice);

    /// <summary>Optional absolute https URL (e.g. a brand logo hosted elsewhere).</summary>
    public static IRuleBuilderOptions<T, string?> OptionalHttpsUrl<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule.Must(value => value is null || (value.Length <= maxLength && IsHttpsUrl(value)))
            .WithMessage($"Must be an absolute https URL of at most {maxLength} characters.");

    public static bool IsHttpsUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
