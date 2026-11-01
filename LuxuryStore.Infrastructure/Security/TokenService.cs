using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LuxuryStore.Application.Interfaces;
using LuxuryStore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LuxuryStore.Infrastructure.Security;

/// <summary>
/// Issues the two tokens Auth revolves around (brief 7.2):
///
/// - A short-lived JWT access token, HMAC-SHA256 signed, validated on every
///   request by the JwtBearer middleware configured in Program.cs.
/// - An opaque, long-lived refresh token: 64 random bytes, base64-encoded.
///   Only its SHA-256 hash is ever persisted (see RefreshToken.TokenHash) —
///   the raw value goes to the client and nowhere else, mirroring how
///   passwords are never stored in plaintext either.
///
/// Reads Jwt:Issuer / Jwt:Audience / Jwt:SigningKey / Jwt:AccessTokenMinutes
/// / Jwt:RefreshTokenDays from configuration — same pattern as
/// AppDbContext's connection string in DependencyInjection.cs: SigningKey
/// is a secret and must come from user-secrets locally or the
/// Jwt__SigningKey environment variable everywhere else, never appsettings.json.
/// </summary>
public class TokenService(IConfiguration configuration) : ITokenService
{
    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        var minutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15;
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        // Short JWT claim names on purpose ("sub", "email", "role", ...).
        // Program.cs sets MapInboundClaims = false, so the API reads these
        // exact names back (no silent remapping to the long WS-Fed URIs),
        // and RoleClaimType = "role" is what [Authorize(Policy = AdminOnly)]
        // checks. GivenName/FamilyName let GET /auth/me answer without a
        // database round trip.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        var days = configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 14;
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (rawToken, DateTime.UtcNow.AddDays(days));
    }

    public string HashRefreshToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private SymmetricSecurityKey GetSigningKey()
    {
        // IsNullOrWhiteSpace, not just "?? throw" — see the matching comment
        // in Program.cs: appsettings.Development.json's "" placeholder for
        // this key isn't null, so a plain "??" would let it through and
        // SymmetricSecurityKey would throw its own, much less helpful
        // IDX10703 ("key length is zero") instead of this message.
        var key = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Configuration 'Jwt:SigningKey' was not found or is empty. Set it via user-secrets locally " +
                "(dotnet user-secrets set \"Jwt:SigningKey\" \"...\" — use a random string of at least " +
                "32 characters — run from the LuxuryStore-Backend project folder, where the UserSecretsId " +
                "lives) or the Jwt__SigningKey environment variable in every other environment.");

        return CreateSigningKey(key);
    }

    /// <summary>
    /// Single place that turns the configured secret into a signing key —
    /// used both here (issuing) and by Program.cs (validating), so the two
    /// can never disagree on encoding or the minimum-length rule.
    /// HS256 needs at least 256 bits of key material; a shorter secret is
    /// brute-forceable offline from any single captured token.
    /// </summary>
    public static SymmetricSecurityKey CreateSigningKey(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        if (bytes.Length < 32)
            throw new InvalidOperationException(
                $"Configuration 'Jwt:SigningKey' is too short ({bytes.Length} bytes). HS256 requires at least " +
                "32 bytes — generate one in PowerShell with: [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))");

        return new SymmetricSecurityKey(bytes);
    }
}
