using LuxuryStore.Application.Interfaces;
using LuxuryStore.Infrastructure.Persistence;

namespace LuxuryStore.Infrastructure.Repositories;

/// <summary>
/// Lazily creates each repository on first access, all sharing the same
/// AppDbContext instance passed in here — which is what makes
/// SaveChangesAsync() commit everything staged across Brands/Categories/
/// Products in one transaction. AppDbContext is itself DI-managed (Scoped),
/// so Dispose() here is a safety net, not the primary owner of its lifetime.
/// </summary>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IBrandRepository? _brands;
    private ICategoryRepository? _categories;
    private IProductRepository? _products;

    public IBrandRepository Brands => _brands ??= new BrandRepository(context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(context);
    public IProductRepository Products => _products ??= new ProductRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();
}
