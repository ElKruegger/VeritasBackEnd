namespace LuxuryStore.Application.Interfaces;

/// <summary>
/// Turns a stored ProductImage.Path into the URL the frontend should load.
/// Implemented in Infrastructure (it needs configuration — the Supabase
/// Storage public base URL), consumed by the entity -> DTO mapping, so no
/// caller above the API ever deals with raw bucket paths.
/// </summary>
public interface IImageUrlResolver
{
    string Resolve(string path, string altText);
}
