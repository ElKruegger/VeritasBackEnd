namespace LuxuryStore.Domain.Common;

/// <summary>
/// Base class for every domain entity. Centralizing Id/CreatedAt/UpdatedAt
/// here means each entity gets them "for free" and Infrastructure can
/// configure/query them generically (see GenericRepository&lt;T&gt;) instead
/// of repeating the same three properties and mappings on every class.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
