using LuxuryStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuxuryStore.Infrastructure.Persistence.Seed;

/// <summary>
/// Loads CatalogSeedData into the database. Run explicitly, never on normal
/// startup:
///
///   dotnet run --project LuxuryStore-Backend -- --seed
///
/// Idempotent and additive: brands, categories and products are matched by
/// slug and only inserted when missing. Running it twice changes nothing;
/// running it after editing a product by hand never overwrites the edit.
/// Everything goes in one transaction — a failure leaves the catalog as it was.
///
/// Uses AppDbContext directly rather than IUnitOfWork: this is an
/// Infrastructure-level bulk loader, not application business logic, and
/// it needs set-based existence checks the repositories don't expose.
/// </summary>
public class CatalogSeeder(AppDbContext context, ILogger<CatalogSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var categories = await SeedCategoriesAsync(cancellationToken);
        var brands = await SeedBrandsAsync(cancellationToken);
        var (products, variants, images) = await SeedProductsAsync(brands, categories, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Catalog seed finished: {Categories} categories, {Brands} brands, {Products} products " +
            "({Variants} variants, {Images} images) inserted. Existing rows were left untouched.",
            categories.Inserted, brands.Inserted, products, variants, images);
    }

    private async Task<(Dictionary<string, Guid> IdsBySlug, int Inserted)> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var ids = await context.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);
        var inserted = 0;

        foreach (var seed in CatalogSeedData.Categories.Where(s => !ids.ContainsKey(s.Slug)))
        {
            var category = new Category { Slug = seed.Slug, Name = seed.Name, Description = seed.Description };
            context.Categories.Add(category);
            ids[seed.Slug] = category.Id;
            inserted++;
        }

        return (ids, inserted);
    }

    private async Task<(Dictionary<string, Guid> IdsBySlug, int Inserted)> SeedBrandsAsync(CancellationToken cancellationToken)
    {
        var ids = await context.Brands.ToDictionaryAsync(b => b.Slug, b => b.Id, cancellationToken);
        var inserted = 0;

        foreach (var seed in CatalogSeedData.Brands.Where(s => !ids.ContainsKey(s.Slug)))
        {
            var brand = new Brand { Slug = seed.Slug, Name = seed.Name };
            context.Brands.Add(brand);
            ids[seed.Slug] = brand.Id;
            inserted++;
        }

        return (ids, inserted);
    }

    private async Task<(int Products, int Variants, int Images)> SeedProductsAsync(
        (Dictionary<string, Guid> IdsBySlug, int Inserted) brands,
        (Dictionary<string, Guid> IdsBySlug, int Inserted) categories,
        CancellationToken cancellationToken)
    {
        var existingSlugs = (await context.Products.Select(p => p.Slug).ToListAsync(cancellationToken)).ToHashSet();
        var existingSkus = (await context.ProductVariants.Select(v => v.Sku).ToListAsync(cancellationToken)).ToHashSet();
        int products = 0, variants = 0, images = 0;

        foreach (var seed in CatalogSeedData.Products.Where(s => !existingSlugs.Contains(s.Slug)))
        {
            // Fail loudly on a typo in the seed data instead of inserting a
            // product with a dangling (and FK-violating) brand/category id.
            if (!brands.IdsBySlug.TryGetValue(seed.BrandSlug, out var brandId))
                throw new InvalidOperationException($"Seed product '{seed.Slug}' references unknown brand '{seed.BrandSlug}'.");
            if (!categories.IdsBySlug.TryGetValue(seed.CategorySlug, out var categoryId))
                throw new InvalidOperationException($"Seed product '{seed.Slug}' references unknown category '{seed.CategorySlug}'.");

            var product = new Product
            {
                Slug = seed.Slug,
                Name = seed.Name,
                Description = seed.Description,
                BasePrice = seed.BasePrice,
                BrandId = brandId,
                CategoryId = categoryId
            };

            // CreatedAt is staggered by a millisecond per variant so the
            // detail page's insertion-order sort (MappingExtensions) is
            // stable even though all rows are created in the same instant.
            var createdAt = DateTime.UtcNow;
            foreach (var (variantSeed, index) in seed.Variants.Select((v, i) => (v, i)))
            {
                // A SKU already taken (e.g. by a hand-made product) is skipped
                // rather than failing the whole seed on the unique index.
                if (!existingSkus.Add(variantSeed.Sku))
                    continue;

                product.Variants.Add(new ProductVariant
                {
                    Sku = variantSeed.Sku,
                    VariantLabel = variantSeed.Label,
                    StockQuantity = variantSeed.Stock,
                    PriceOverride = variantSeed.PriceOverride,
                    CreatedAt = createdAt.AddMilliseconds(index)
                });
                variants++;
            }

            // Alt text names the brand too ("Rolex Submariner Date 41 mm —
            // detail"): it's what a screen reader announces and what the
            // placeholder image shows until real photos are uploaded.
            var brandName = CatalogSeedData.Brands.Single(b => b.Slug == seed.BrandSlug).Name;
            foreach (var (view, index) in seed.ImageViews.Select((v, i) => (v, i)))
            {
                product.Images.Add(new ProductImage
                {
                    Path = $"products/{seed.Slug}/{index + 1:00}.jpg",
                    AltText = $"{brandName} {seed.Name} — {view}",
                    SortOrder = index
                });
                images++;
            }

            context.Products.Add(product);
            products++;
        }

        return (products, variants, images);
    }
}
