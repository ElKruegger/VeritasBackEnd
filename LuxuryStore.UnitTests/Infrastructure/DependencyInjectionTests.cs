using LuxuryStore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LuxuryStore.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    private static IConfiguration ConfigWithConnectionString(string? value) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = value })
            .Build();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_or_empty_connection_string_fails_at_startup_with_a_clear_message(string? value)
    {
        var ex = Should.Throw<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(ConfigWithConnectionString(value)));

        ex.Message.ShouldContain("ConnectionStrings__DefaultConnection");
    }

    [Fact]
    public void A_configured_connection_string_is_accepted()
    {
        Should.NotThrow(() =>
            new ServiceCollection().AddInfrastructure(ConfigWithConnectionString("Host=localhost;Database=x")));
    }
}
