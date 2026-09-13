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

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
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

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }
}
