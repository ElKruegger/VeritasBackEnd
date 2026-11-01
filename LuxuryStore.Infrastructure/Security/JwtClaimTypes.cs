namespace LuxuryStore.Infrastructure.Security;

/// <summary>
/// Claim names this API issues that JwtRegisteredClaimNames doesn't cover.
/// "role" is the conventional short name (RFC 9068 / OIDC style); Program.cs
/// points TokenValidationParameters.RoleClaimType at it.
/// </summary>
public static class JwtClaimTypes
{
    public const string Role = "role";
}
