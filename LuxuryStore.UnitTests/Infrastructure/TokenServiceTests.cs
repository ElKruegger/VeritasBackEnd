using System.IdentityModel.Tokens.Jwt;
using LuxuryStore.Domain.Entities;
using LuxuryStore.Domain.Enums;
using LuxuryStore.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace LuxuryStore.UnitTests.Infrastructure;

public class TokenServiceTests
{
    private const string SigningKey = "unit-test-signing-key-with-more-than-32-bytes!!";

    private static TokenService Service(string signingKey = SigningKey) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "LuxuryStore.Api",
                ["Jwt:Audience"] = "LuxuryStore.Client",
                ["Jwt:SigningKey"] = signingKey,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14"
            })
            .Build());

    private static readonly User Admin = new()
    {
        Email = "admin@example.com",
        FirstName = "Erick",
        LastName = "Krug",
        Role = UserRole.Admin
    };

    [Fact]
    public void Access_token_carries_identity_and_role_claims_with_short_names()
    {
        var (token, _) = Service().GenerateAccessToken(Admin);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Single(c => c.Type == "sub").Value.ShouldBe(Admin.Id.ToString());
        jwt.Claims.Single(c => c.Type == "email").Value.ShouldBe("admin@example.com");
        jwt.Claims.Single(c => c.Type == "given_name").Value.ShouldBe("Erick");
        jwt.Claims.Single(c => c.Type == "family_name").Value.ShouldBe("Krug");
        jwt.Claims.Single(c => c.Type == JwtClaimTypes.Role).Value.ShouldBe("Admin");
        jwt.Issuer.ShouldBe("LuxuryStore.Api");
        jwt.Audiences.ShouldContain("LuxuryStore.Client");
        jwt.Header.Alg.ShouldBe("HS256");
    }

    [Fact]
    public void Access_token_expires_after_the_configured_minutes()
    {
        var (_, expiresAt) = Service().GenerateAccessToken(Admin);

        expiresAt.ShouldBe(DateTime.UtcNow.AddMinutes(15), tolerance: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Signing_key_shorter_than_32_bytes_is_rejected()
    {
        var ex = Should.Throw<InvalidOperationException>(() => Service("too-short").GenerateAccessToken(Admin));

        ex.Message.ShouldContain("too short");
    }

    [Fact]
    public void Refresh_tokens_are_random_and_expire_after_the_configured_days()
    {
        var service = Service();

        var (first, expiresAt) = service.GenerateRefreshToken();
        var (second, _) = service.GenerateRefreshToken();

        first.ShouldNotBe(second);
        Convert.FromBase64String(first).Length.ShouldBe(64);
        expiresAt.ShouldBe(DateTime.UtcNow.AddDays(14), tolerance: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Refresh_token_hash_is_a_deterministic_sha256_hex_string()
    {
        var service = Service();

        var hash = service.HashRefreshToken("raw-token");

        hash.ShouldBe(service.HashRefreshToken("raw-token"));
        hash.ShouldNotBe(service.HashRefreshToken("other-token"));
        hash.Length.ShouldBe(64);
        hash.ShouldMatch("^[0-9A-F]+$");
    }
}
