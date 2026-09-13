using LuxuryStore.Application.Interfaces;
using LuxuryStore.Domain.Entities;
using LuxuryStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LuxuryStore.Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : GenericRepository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower(), cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(u => u.Email == email.Trim().ToLower() && (excludeId == null || u.Id != excludeId), cancellationToken);
}
