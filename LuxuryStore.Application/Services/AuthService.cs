using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Exceptions;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Application.Mapping;
using LuxuryStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LuxuryStore.Application.Services;

/// <summary>
/// Business logic for registration, login and refresh-token rotation
/// (brief 7.2). Same shape as BrandService/ProductService: depends only on
/// IUnitOfWork plus two narrow abstractions (IPasswordHasher&lt;User&gt; from
/// ASP.NET Core Identity, ITokenService for JWT/refresh-token mechanics) —
/// never on EF Core, ASP.NET Core Authentication, or a JWT library directly.
/// </summary>
public class AuthService(
    IUnitOfWork unitOfWork,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (await unitOfWork.Users.EmailExistsAsync(dto.Email, cancellationToken: cancellationToken))
            // Still reveals that the email is registered — unavoidable until
            // registration confirms by email ("check your inbox" for every
            // address). Kept as small as possible meanwhile: the input is
            // not echoed back, and the endpoint is rate-limited per IP.
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            Email = dto.Email.Trim().ToLowerInvariant(),
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };
        user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    /// <summary>
    /// Account lockout: after this many consecutive wrong passwords, sign-in
    /// is refused for LockoutDuration — even with the right password. The
    /// per-IP rate limit alone doesn't stop a slow attack spread over many
    /// IPs against one account; this does. Temporary on purpose: a permanent
    /// lock would let anyone lock any customer out by typing wrong passwords.
    /// </summary>
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The ONE message for every failed login: unknown email, wrong password
    /// and locked account all look identical, so the login form can't be
    /// used to find out which emails have an account (user enumeration).
    /// </summary>
    public const string InvalidCredentialsMessage =
        "Invalid email or password. Repeated failed attempts temporarily block sign-in.";

    public async Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = await unitOfWork.Users.GetByEmailAsync(dto.Email.Trim(), cancellationToken);

        // Unknown email or locked account: still run a full password hash
        // before answering. Hashing is deliberately slow (PBKDF2), so
        // skipping it would make these answers measurably faster than a
        // wrong password — and the response time would leak what the
        // message hides.
        if (user is null || user.IsLockedOut(now))
        {
            VerifyAgainstDummyHash(dto.Password);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await RecordFailedLoginAsync(user, now, cancellationToken);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        // Only after the right password: before, this message told anyone
        // typing a random password that the email exists (and is disabled).
        if (!user.IsActive)
            throw new UnauthorizedException("This account has been deactivated.");

        if (user.FailedLoginCount > 0 || user.LockoutEndsAt is not null)
        {
            user.FailedLoginCount = 0;
            user.LockoutEndsAt = null;
            unitOfWork.Users.Update(user); // saved together with the new refresh token
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    private async Task RecordFailedLoginAsync(User user, DateTime now, CancellationToken cancellationToken)
    {
        user.FailedLoginCount++;
        if (user.FailedLoginCount >= MaxFailedLogins)
        {
            user.LockoutEndsAt = now.Add(LockoutDuration);
            // Start counting again from zero once the lockout is over.
            user.FailedLoginCount = 0;
        }

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // A real hash of a random password, created once per process. Verifying
    // against it costs exactly what verifying a real user's password costs.
    private static readonly User DummyUser = new();
    private static string? _dummyPasswordHash;

    private void VerifyAgainstDummyHash(string password)
    {
        var hash = _dummyPasswordHash ??= passwordHasher.HashPassword(DummyUser, Guid.NewGuid().ToString());
        passwordHasher.VerifyHashedPassword(DummyUser, hash, password);
    }

    public async Task<AuthResultDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashRefreshToken(refreshToken);
        var existing = await unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        // Reuse detection: a token that was already rotated (RevokedAt set)
        // should never come back — the legitimate client threw it away when
        // it received the replacement. Seeing it again means either it was
        // stolen or something is replaying it, and we can't tell which copy
        // is the attacker's, so every live session of that user is killed
        // and they have to log in again. The frontend must therefore never
        // fire two /auth/refresh calls in parallel (single-flight the refresh),
        // or the second one would trip this on perfectly honest traffic.
        if (existing.RevokedAt is not null)
        {
            await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(existing.UserId, cancellationToken);
            throw new UnauthorizedException("Refresh token has already been used. Please log in again.");
        }

        if (!existing.IsActive)
            throw new UnauthorizedException("Refresh token has expired. Please log in again.");

        // Fetched separately rather than via an Include() on the query
        // above: existing is about to be passed to Update() below, and an
        // already-loaded User navigation on that call would make EF mark
        // the whole User row Modified too, overwriting any concurrent
        // change to it with stale AsNoTracking values (see RefreshTokenRepository).
        var user = await unitOfWork.Users.GetByIdAsync(existing.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        // Same check LoginAsync does. Without it, deactivating an account
        // only blocked new logins — any refresh token issued before the
        // deactivation kept minting access tokens until it expired (14 days).
        if (!user.IsActive)
        {
            await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(user.Id, cancellationToken);
            throw new UnauthorizedException("This account has been deactivated.");
        }

        var (newRawToken, newExpiresAt) = tokenService.GenerateRefreshToken();
        var newTokenHash = tokenService.HashRefreshToken(newRawToken);

        // Rotation: revoke the presented token and chain it to its
        // replacement instead of just deleting it — this is what makes a
        // replay of an already-rotated token detectable later.
        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByTokenHash = newTokenHash;
        unitOfWork.RefreshTokens.Update(existing);

        var (accessToken, accessExpiresAt) = tokenService.GenerateAccessToken(user);

        await unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = newExpiresAt
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResultDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = newRawToken,
            RefreshTokenExpiresAt = newExpiresAt,
            User = user.ToDto()
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashRefreshToken(refreshToken);
        var existing = await unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        // Idempotent on purpose: logging out with an already-revoked or
        // unknown token should never error — the caller's goal (not being
        // logged in anymore) is already achieved.
        if (existing is null || existing.RevokedAt is not null)
            return;

        existing.RevokedAt = DateTime.UtcNow;
        unitOfWork.RefreshTokens.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResultDto> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, accessExpiresAt) = tokenService.GenerateAccessToken(user);
        var (refreshToken, refreshExpiresAt) = tokenService.GenerateRefreshToken();

        await unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = refreshExpiresAt
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResultDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = user.ToDto()
        };
    }
}
