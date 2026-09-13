using LuxuryStore.Application.Interfaces;
using LuxuryStore.Domain.Entities;
using LuxuryStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LuxuryStore.Infrastructure.Repositories;

public class CategoryRepository(AppDbContext context) : GenericRepository<Category>(context), ICategoryRepository
{
    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId), cancellationToken);
}
