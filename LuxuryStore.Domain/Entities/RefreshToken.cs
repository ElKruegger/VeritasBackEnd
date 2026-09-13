using LuxuryStore.Domain.Common;

namespace LuxuryStore.Domain.Entities;

/// <summary>
/// One refresh-token "session" for a User (brief 7.2: "short-lived access
/// token + refresh token rotation"). Only a SHA-256 hash of the actual
/// token value is ever persisted here — never the raw token — the same
/// principle as password hashing: if this table ever leaked, no usable
/// refresh token leaks with it (see LuxuryStore.Infrastructure.Security.TokenService).
///
/// Rotation-on-use: every successful /auth/refresh call revokes the token
/// it was given (RevokedAt) and records the new one's hash
/// (ReplacedByTokenHash), forming a chain. That chain is what lets a stolen
/// refresh token be detected later: if a revoked token is ever presented
/// again, that's a replay, not a legitimate retry.
/// </summary>
public class RefreshToken : BaseEntity
{
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    // Get-only, no backing field — EF Core's default conventions don't map
    // this to a column, so it's safe to keep next to the data it derives from.
    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
