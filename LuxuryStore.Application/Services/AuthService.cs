using LuxuryStore.Application.DTOs;
using LuxuryStore.Application.Exceptions;
using LuxuryStore.Application.Interfaces;
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
            throw new ConflictException($"An account with email '{dto.Email}' already exists.");

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

    public async Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(dto.Email.Trim(), cancellationToken)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("This account has been deactivated.");

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid email or password.");

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResultDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = tokenService.HashRefreshToken(refreshToken);
        var existing = await unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (!existing.IsActive)
            throw new UnauthorizedException("Refresh token is expired or has already been used.");

        // Fetched separately rather than via an Include() on the query
        // above: existing is about to be passed to Update() below, and an
        // already-loaded User navigation on that call would make EF mark
        // the whole User row Modified too, overwriting any concurrent
        // change to it with stale AsNoTracking values (see RefreshTokenRepository).
        var user = await unitOfWork.Users.GetByIdAsync(existing.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

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
            User = ToUserDto(user)
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
            User = ToUserDto(user)
        };
    }

    private static UserDto ToUserDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName
    };
}
