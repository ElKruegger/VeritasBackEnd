using LuxuryStore.Domain.Common;

namespace LuxuryStore.Domain.Entities;

/// <summary>
/// A registered customer (brief section 8). PasswordHash is never the raw
/// password — it's produced by ASP.NET Core Identity's PasswordHasher
/// (see LuxuryStore.Application/DependencyInjection.cs), which already
/// salts and uses PBKDF2 internally, so no separate salt column is needed.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
