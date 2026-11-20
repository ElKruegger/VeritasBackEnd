namespace LuxuryStore.Infrastructure.Persistence;

/// <summary>
/// Builds LIKE/ILIKE patterns from user input. In a pattern, % matches any
/// run of characters and _ any single character, so a search for "%" used
/// to match every product. The value always travels as a SQL parameter
/// (no injection either way) — this only makes the user's text literal.
/// Callers must pass EscapeCharacter to EF.Functions.Like/ILike: the Npgsql
/// provider otherwise emits ESCAPE '' and turns escaping off.
/// </summary>
public static class LikePattern
{
    public const string EscapeCharacter = @"\";

    public static string Contains(string text) => $"%{Escape(text.Trim())}%";

    public static string Escape(string text) => text
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");
}
