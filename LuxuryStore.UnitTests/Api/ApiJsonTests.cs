using System.Text.Json;
using LuxuryStore.Application.DTOs;
using LuxuryStore_Backend.Contract;

namespace LuxuryStore.UnitTests.Api;

public class ApiJsonTests
{
    private static readonly JsonSerializerOptions Options = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJson.Configure(options);
        return options;
    }

    private sealed record Price(decimal Amount);

    [Fact]
    public void Enums_are_written_by_their_camel_case_name()
    {
        JsonSerializer.Serialize(ProductSort.PriceAsc, Options).ShouldBe("\"priceAsc\"");
    }

    [Fact]
    public void Enums_sent_as_numbers_are_rejected()
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<ProductSort>("1", Options));
    }

    [Fact]
    public void Numbers_sent_as_strings_are_rejected()
    {
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Price>("""{"amount":"1290"}""", Options));
    }

    [Fact]
    public void Numbers_are_read_as_numbers()
    {
        JsonSerializer.Deserialize<Price>("""{"amount":1290.5}""", Options)!.Amount.ShouldBe(1290.5m);
    }
}
