using LuxuryStore.Application.Interfaces;
using LuxuryStore.Domain.Entities;
using LuxuryStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LuxuryStore.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext context) : GenericRepository<RefreshToken>(context), IRefreshTokenRepository
{
    // No Include(User) here on purpose — same reasoning as
    // ProductService.UpdateAsync's comment about fetching without Include():
    // every caller of this method goes on to call Update() on the result to
    // revoke it, and attaching an already-loaded User navigation to that
    // call would make EF mark the whole User row Modified too, silently
    // overwriting any concurrent change to it with these stale, AsNoTracking
    // values. AuthService fetches the User separately (unitOfWork.Users.GetByIdAsync)
    // on the one path that actually needs it (RefreshAsync).
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
}
