using LuxuryStore.Domain.Common;

namespace LuxuryStore.Domain.Entities;

/// <summary>
/// A shipping/billing address belonging to a User (brief section 8). Its
/// own entity rather than fields on User, since a user can have more than
/// one (home, work, a gift address) and Order will eventually need to
/// reference "the address used for this order" independently of whichever
/// address is currently the user's default.
/// </summary>
public class Address : BaseEntity
{
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
