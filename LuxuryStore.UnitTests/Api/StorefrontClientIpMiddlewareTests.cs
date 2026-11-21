using System.Net;
using LuxuryStore_Backend.Middleware;
using Microsoft.AspNetCore.Http;

namespace LuxuryStore.UnitTests.Api;

public class StorefrontClientIpMiddlewareTests
{
    private const string Secret = "a-test-secret-that-is-long-enough-123";
    private static readonly IPAddress ProxyIp = IPAddress.Parse("10.0.0.5");

    private static async Task<HttpContext> Send(string? configuredSecret, Dictionary<string, string> headers)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = ProxyIp;
        foreach (var (name, value) in headers) context.Request.Headers[name] = value;

        var middleware = new StorefrontClientIpMiddleware(_ => Task.CompletedTask, configuredSecret);
        await middleware.InvokeAsync(context);
        return context;
    }

    private static Dictionary<string, string> Relayed(string secret, string clientIp) => new()
    {
        [StorefrontClientIpMiddleware.SecretHeader] = secret,
        [StorefrontClientIpMiddleware.ClientIpHeader] = clientIp
    };

    [Fact]
    public async Task The_storefront_can_say_who_the_visitor_is()
    {
        var context = await Send(Secret, Relayed(Secret, "203.0.113.7"));

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse("203.0.113.7"));
    }

    [Fact]
    public async Task An_ipv6_visitor_is_understood_too()
    {
        var context = await Send(Secret, Relayed(Secret, "2001:db8::1"));

        context.Connection.RemoteIpAddress.ShouldBe(IPAddress.Parse("2001:db8::1"));
    }

    [Theory]
    [InlineData("wrong-secret-that-is-long-enough-12345")]
    [InlineData("")]
    public async Task Without_the_secret_the_ip_header_is_ignored(string secret)
    {
        var context = await Send(Secret, Relayed(secret, "203.0.113.7"));

        context.Connection.RemoteIpAddress.ShouldBe(ProxyIp);
    }

    [Fact]
    public async Task A_malformed_ip_is_ignored()
    {
        var context = await Send(Secret, Relayed(Secret, "not-an-ip"));

        context.Connection.RemoteIpAddress.ShouldBe(ProxyIp);
    }

    [Fact]
    public async Task Nothing_is_trusted_when_no_secret_is_configured()
    {
        var context = await Send(null, Relayed("", "203.0.113.7"));

        context.Connection.RemoteIpAddress.ShouldBe(ProxyIp);
    }

    [Theory]
    [InlineData(Secret)]
    [InlineData(null)]
    public async Task Both_headers_are_removed_before_the_rest_of_the_pipeline(string? configured)
    {
        var context = await Send(configured, Relayed(Secret, "203.0.113.7"));

        context.Request.Headers.ContainsKey(StorefrontClientIpMiddleware.SecretHeader).ShouldBeFalse();
        context.Request.Headers.ContainsKey(StorefrontClientIpMiddleware.ClientIpHeader).ShouldBeFalse();
    }

    [Fact]
    public void A_short_secret_is_refused_at_startup()
    {
        Should.Throw<InvalidOperationException>(
            () => new StorefrontClientIpMiddleware(_ => Task.CompletedTask, "too-short"));
    }
}
