using LuxuryStore.Application.Interfaces;
using LuxuryStore.Domain.Entities;
using LuxuryStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LuxuryStore.Infrastructure.Repositories;

public class BrandRepository(AppDbContext context) : GenericRepository<Brand>(context), IBrandRepository
{
    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(b => b.Slug == slug && (excludeId == null || b.Id != excludeId), cancellationToken);
}
