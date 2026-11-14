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
        var filtered = DbSet
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            // Listing cards only need the primary image, so a filtered
            // Include loads one image per product instead of every gallery
            // (and variants aren't loaded at all on the listing anymore).
            // Same ordering as MappingExtensions.ToListItemDto.
            .Include(p => p.Images
                .OrderBy(i => i.VariantId.HasValue)
                .ThenBy(i => i.SortOrder)
                .Take(1))
            .Where(p => p.IsActive);

        if (query.BrandId is not null)
            filtered = filtered.Where(p => p.BrandId == query.BrandId);

        if (query.CategoryId is not null)
            filtered = filtered.Where(p => p.CategoryId == query.CategoryId);

        if (query.MinPrice is not null)
            filtered = filtered.Where(p => p.BasePrice >= query.MinPrice);

        if (query.MaxPrice is not null)
            filtered = filtered.Where(p => p.BasePrice <= query.MaxPrice);

        if (!string.IsNullOrWhiteSpace(query.Search))
            filtered = filtered.Where(p => EF.Functions.ILike(p.Name, $"%{query.Search}%"));

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderBy(p => p.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

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
