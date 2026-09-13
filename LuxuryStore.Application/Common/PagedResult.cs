namespace LuxuryStore.Application.Common;

/// <summary>
/// Generic envelope for paginated list responses. Every paginated endpoint
/// (products today, orders/reviews later) returns this same shape, so the
/// frontend can consume all of them with one shared TypeScript type instead
/// of a bespoke response shape per endpoint.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
