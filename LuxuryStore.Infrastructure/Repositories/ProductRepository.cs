using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Domain.Entities;
using LuxuryStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LuxuryStore.Infrastructure.Repositories;

public class ProductRepository(AppDbContext context) : GenericRepository<Product>(context), IProductRepository
{
    public async Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            // Two collection Includes in one SQL query would multiply rows
            // (variants x images); split queries load each collection with
            // its own round trip instead.
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Product?> GetBySlugWithDetailsAsync(string slug, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        ProductQueryParams query, CancellationToken cancellationToken = default)
    {
        var filtered = ApplySlice(DbSet.AsNoTracking().Where(p => p.IsActive), query.CategoryId, query.BrandId, query.Search);

        if (query.MinPrice is not null)
            filtered = filtered.Where(p => p.BasePrice >= query.MinPrice);

        if (query.MaxPrice is not null)
            filtered = filtered.Where(p => p.BasePrice <= query.MaxPrice);

        var sizes = query.Sizes?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct()
            .ToList() ?? [];

        // One EXISTS over the variants covers every combination: size only,
        // stock only, or "in stock IN this size" when both are set (a product
        // with UK 8 sold out and UK 9 available is not a match for UK 8 + inStock).
        if (sizes.Count > 0 || query.InStock)
        {
            var inStock = query.InStock;
            filtered = filtered.Where(p => p.Variants.Any(v =>
                v.IsActive &&
                (sizes.Count == 0 || sizes.Contains(v.VariantLabel)) &&
                (!inStock || v.StockQuantity > 0)));
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await IncludeListingData(Sort(filtered, query.Sort))
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<ProductFacets> GetFacetsAsync(ProductFilterQuery query, CancellationToken cancellationToken = default)
    {
        var active = DbSet.AsNoTracking().Where(p => p.IsActive);
        var slice = ApplySlice(active, query.CategoryId, query.BrandId, query.Search);

        // Brands ignore query.BrandId (see ProductFiltersDto.Brands).
        var brands = await ApplySlice(active, query.CategoryId, brandId: null, query.Search)
            .Select(p => p.Brand)
            .Where(b => b.IsActive)
            .Distinct()
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var sizes = await slice
            .SelectMany(p => p.Variants)
            .Where(v => v.IsActive)
            .Select(v => v.VariantLabel)
            .Distinct()
            .ToListAsync(cancellationToken);

        // (decimal?) casts: MIN/MAX over zero rows is NULL in SQL, which a
        // non-nullable decimal can't hold.
        var minPrice = await slice.MinAsync(p => (decimal?)p.BasePrice, cancellationToken);
        var maxPrice = await slice.MaxAsync(p => (decimal?)p.BasePrice, cancellationToken);

        return new ProductFacets(brands, sizes, minPrice, maxPrice);
    }

    public async Task<IReadOnlyList<Product>> GetRelatedAsync(Product product, int limit, CancellationToken cancellationToken = default)
    {
        var brandId = product.BrandId;

        return await IncludeListingData(DbSet
                .AsNoTracking()
                .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
                .OrderByDescending(p => p.BrandId == brandId)
                .ThenByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Id))
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // Category, brand and text search: the "which slice of the catalog"
    // part shared by the listing and the filter facets.
    internal static IQueryable<Product> ApplySlice(IQueryable<Product> products, Guid? categoryId, Guid? brandId, string? search)
    {
        if (categoryId is not null)
            products = products.Where(p => p.CategoryId == categoryId);

        if (brandId is not null)
            products = products.Where(p => p.BrandId == brandId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // The escape character MUST be passed: without it the Npgsql
            // provider emits ESCAPE '' (escaping disabled), and the
            // backslashes added by LikePattern would be searched literally.
            var pattern = LikePattern.Contains(search);
            products = products.Where(p =>
                EF.Functions.ILike(p.Name, pattern, LikePattern.EscapeCharacter) ||
                EF.Functions.ILike(p.Brand.Name, pattern, LikePattern.EscapeCharacter));
        }

        return products;
    }

    // Every sort ends with Id: without a unique tie-breaker, products with
    // the same price/name/date can come back in a different order on each
    // query, so paging could show one product twice and skip another.
    private static IOrderedQueryable<Product> Sort(IQueryable<Product> products, ProductSort sort) => sort switch
    {
        ProductSort.PriceAsc => products.OrderBy(p => p.BasePrice).ThenBy(p => p.Name).ThenBy(p => p.Id),
        ProductSort.PriceDesc => products.OrderByDescending(p => p.BasePrice).ThenBy(p => p.Name).ThenBy(p => p.Id),
        ProductSort.Newest => products.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
        _ => products.OrderBy(p => p.Name).ThenBy(p => p.Id)
    };

    // What a listing card needs (MappingExtensions.ToListItemDto), applied
    // after sorting so the filtered Includes don't interfere with it.
    private static IQueryable<Product> IncludeListingData(IQueryable<Product> products) => products
        .Include(p => p.Brand)
        .Include(p => p.Category)
        // Only the primary image, not the whole gallery. Same ordering as
        // MappingExtensions.ToListItemDto.
        .Include(p => p.Images
            .OrderBy(i => i.VariantId.HasValue)
            .ThenBy(i => i.SortOrder)
            .Take(1))
        // At most one in-stock variant: enough to know whether the card
        // shows "Sold out" (ProductListItemDto.InStock), without loading
        // every size of every product.
        .Include(p => p.Variants
            .Where(v => v.IsActive && v.StockQuantity > 0)
            .Take(1))
        // Two collection Includes in one SQL query would multiply rows.
        .AsSplitQuery();

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), cancellationToken);

    public async Task<ProductVariant?> GetVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default) =>
        await Context.Set<ProductVariant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, cancellationToken);

    public void AddVariant(ProductVariant variant) => Context.Set<ProductVariant>().Add(variant);

    public void UpdateVariant(ProductVariant variant) => Context.Set<ProductVariant>().Update(variant);

    public void RemoveVariant(ProductVariant variant) => Context.Set<ProductVariant>().Remove(variant);

    public async Task<ProductImage?> GetImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default) =>
        await Context.Set<ProductImage>()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken);

    public async Task<int?> GetMaxImageSortOrderAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await Context.Set<ProductImage>()
            .Where(i => i.ProductId == productId)
            .MaxAsync(i => (int?)i.SortOrder, cancellationToken);

    public void AddImage(ProductImage image) => Context.Set<ProductImage>().Add(image);

    public void RemoveImage(ProductImage image) => Context.Set<ProductImage>().Remove(image);
}
