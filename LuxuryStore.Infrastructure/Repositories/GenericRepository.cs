using LuxuryStore.Application.Interfaces;
using LuxuryStore.Domain.Common;
using LuxuryStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LuxuryStore.Infrastructure.Repositories;

/// <summary>
/// Shared CRUD implementation for every aggregate. Entity-specific
/// repositories inherit from this and only add the queries that need
/// custom filtering/Include() logic (see ProductRepository).
///
/// Reads use AsNoTracking() throughout: every read here is either a public
/// GET or an existence check, and the entities returned for a subsequent
/// mutation (Update/Remove) get explicitly re-attached by Update()/Remove()
/// below — so nothing depends on the change tracker having kept a "live"
/// tracked instance around between the read and the write.
/// </summary>
public class GenericRepository<T>(AppDbContext context) : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);
}
