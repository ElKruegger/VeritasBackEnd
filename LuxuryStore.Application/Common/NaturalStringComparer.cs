using System.Globalization;

namespace LuxuryStore.Application.Common;

/// <summary>
/// Orders strings the way a person reads them: runs of digits compare as
/// numbers, so "UK 7" &lt; "UK 10" and "Collar 15.5" &lt; "Collar 16"
/// (plain ordinal ordering gives "UK 10" &lt; "UK 7"). Text parts compare
/// case-insensitively. Used for the size facet of the catalog filters.
/// </summary>
public sealed class NaturalStringComparer : IComparer<string>
{
    public static readonly NaturalStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                var a = ReadNumber(x, ref i);
                var b = ReadNumber(y, ref j);
                var byNumber = a.CompareTo(b);
                if (byNumber != 0) return byNumber;
            }
            else
            {
                var byChar = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
                if (byChar != 0) return byChar;
                i++;
                j++;
            }
        }

        // One is a prefix of the other: the shorter one first ("M" < "M Tall").
        var byLength = (x.Length - i).CompareTo(y.Length - j);
        return byLength != 0 ? byLength : string.CompareOrdinal(x, y);
    }

    // Digits, optionally followed by ".digits" ("15.5"). Size labels never
    // need more than that.
    private static decimal ReadNumber(string s, ref int index)
    {
        var start = index;
        while (index < s.Length && char.IsDigit(s[index])) index++;

        if (index + 1 < s.Length && s[index] == '.' && char.IsDigit(s[index + 1]))
        {
            index++;
            while (index < s.Length && char.IsDigit(s[index])) index++;
        }

        // Up to 28 digits fit a decimal; a longer run is not a real size and
        // just compares as the largest value.
        return decimal.TryParse(s.AsSpan(start, index - start), NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var value) ? value : decimal.MaxValue;
    }
}
