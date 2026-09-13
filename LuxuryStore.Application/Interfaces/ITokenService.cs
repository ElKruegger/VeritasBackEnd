using LuxuryStore.Domain.Entities;

namespace LuxuryStore.Application.Interfaces;

/// <summary>
/// Abstraction over JWT/refresh-token mechanics so AuthService (business
/// logic: is this email taken, is this password right) never has to know
/// which token format or crypto library is behind it. Implemented in
/// LuxuryStore.Infrastructure — same split as IUnitOfWork/UnitOfWork —
/// since the concrete implementation needs configuration (issuer, audience,
/// signing key) and a JWT library, neither of which Application depends on.
/// </summary>
public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    /// <summary>The raw, opaque refresh token to hand to the client. Never persisted as-is — see HashRefreshToken.</summary>
    (string Token, DateTime ExpiresAt) GenerateRefreshToken();

    /// <summary>SHA-256 hash of a raw refresh token, for storage/lookup in RefreshTokens.TokenHash.</summary>
    string HashRefreshToken(string rawToken);
}
