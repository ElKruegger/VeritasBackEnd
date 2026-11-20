using LuxuryStore.Application.DTOs;
using LuxuryStore.Domain.Common;
using LuxuryStore.Domain.Entities;

namespace LuxuryStore.Application.Interfaces;

/// <summary>
/// Basic CRUD surface shared by every aggregate. Entity-specific
/// repositories extend this with the queries that need custom
/// Include()/filtering logic — see IProductRepository — instead of leaking
/// IQueryable out of the repository, which would defeat the point of the
/// abstraction (Infrastructure/EF Core details would leak into Application).
/// </summary>
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IBrandRepository : IGenericRepository<Brand>
{
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugWithDetailsAsync(string slug, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Raw filter values of a catalog slice (active products only). Sizes
    /// come back unsorted — ordering them for display is ProductService's job.
    /// </summary>
    Task<ProductFacets> GetFacetsAsync(ProductFilterQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active products of the same category, excluding the product itself:
    /// same brand first, then newest. Loaded like GetPagedAsync items.
    /// </summary>
    Task<IReadOnlyList<Product>> GetRelatedAsync(Product product, int limit, CancellationToken cancellationToken = default);

    // Variants aren't their own aggregate (nothing ever queries a variant
    // outside the context of a product), so they're reached through the
    // product repository rather than getting an IProductVariantRepository.
    Task<ProductVariant?> GetVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default);
    void AddVariant(ProductVariant variant);
    void UpdateVariant(ProductVariant variant);
    void RemoveVariant(ProductVariant variant);

    // Images, same reasoning as variants: always reached through their product.
    Task<ProductImage?> GetImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);
    Task<int?> GetMaxImageSortOrderAsync(Guid productId, CancellationToken cancellationToken = default);
    void AddImage(ProductImage image);
    void RemoveImage(ProductImage image);
}

/// <summary>Result of IProductRepository.GetFacetsAsync.</summary>
public sealed record ProductFacets(
    IReadOnlyList<Brand> Brands,
    IReadOnlyList<string> Sizes,
    decimal? MinPrice,
    decimal? MaxPrice);

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every still-active refresh token of a user in one UPDATE
    /// statement. Runs immediately (ExecuteUpdate), NOT deferred until
    /// IUnitOfWork.SaveChangesAsync — callers use it right before throwing,
    /// so there is no later SaveChanges that would persist a staged change.
    /// Returns how many tokens were revoked.
    /// </summary>
    Task<int> RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every refresh token that expired before <paramref name="cutoff"/>,
    /// revoked or not, in one DELETE statement (runs immediately, like
    /// RevokeAllActiveForUserAsync). Returns how many rows were deleted.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default);
}

/// <summary>
/// Coordinates repositories that share one DbContext/transaction. Services
/// call repository methods to stage changes in memory, then call
/// SaveChangesAsync exactly once per request — that's what makes a
/// multi-entity write (e.g. validating a Brand and a Category before
/// inserting a Product) atomic without each repository opening its own
/// transaction, and it's why repositories never call SaveChanges themselves.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IBrandRepository Brands { get; }
    ICategoryRepository Categories { get; }
    IProductRepository Products { get; }
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
