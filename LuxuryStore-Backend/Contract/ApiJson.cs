using System.Text.Json;
using System.Text.Json.Serialization;

namespace LuxuryStore_Backend.Contract;

/// <summary>
/// JSON rules of the public API, shared by the serializer and by the
/// OpenAPI document (both read their own copy of the options, so the two
/// are configured from this one place and can never disagree).
/// </summary>
public static class ApiJson
{
    public static void Configure(JsonSerializerOptions options)
    {
        // ASP.NET's web defaults also accept numbers written as strings
        // ("1290"), which makes the OpenAPI document describe every number
        // as "number | string". Numbers are numbers: a quoted one is a 400.
        options.NumberHandling = JsonNumberHandling.Strict;

        // Enums travel by name ("priceAsc"), not by position (1): reordering
        // the C# enum can no longer silently change what a client asked for.
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    }
}
