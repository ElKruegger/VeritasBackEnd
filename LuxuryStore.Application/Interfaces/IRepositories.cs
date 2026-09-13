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

    // Variants aren't their own aggregate (nothing ever queries a variant
    // outside the context of a product), so they're reached through the
    // product repository rather than getting an IProductVariantRepository.
    Task<ProductVariant?> GetVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default);
    void AddVariant(ProductVariant variant);
    void UpdateVariant(ProductVariant variant);
    void RemoveVariant(ProductVariant variant);
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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
