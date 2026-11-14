using LuxuryStore_Backend.Middleware;
using Microsoft.AspNetCore.Http;

namespace LuxuryStore.UnitTests.Api;

public class SecurityHeadersTests
{
    // Typed as the interface: XContentTypeOptions, ContentSecurityPolicy...
    // are default members of IHeaderDictionary, not of the concrete class.
    private static IHeaderDictionary ApplyFor(string path)
    {
        IHeaderDictionary headers = new HeaderDictionary();
        SecurityHeaders.Apply(headers, new PathString(path));
        return headers;
    }

    [Fact]
    public void Api_responses_forbid_sniffing_framing_and_loading_any_resource()
    {
        var headers = ApplyFor("/api/v1/products");

        headers.XContentTypeOptions.ToString().ShouldBe("nosniff");
        headers.XFrameOptions.ToString().ShouldBe("DENY");
        headers.ContentSecurityPolicy.ToString().ShouldBe("default-src 'none'; frame-ancestors 'none'");
        headers["Referrer-Policy"].ToString().ShouldBe("no-referrer");
        headers["Permissions-Policy"].ToString().ShouldContain("camera=()");
    }

    [Fact]
    public void The_docs_page_may_load_its_own_scripts_but_still_cannot_be_framed()
    {
        var headers = ApplyFor("/scalar/v1");

        headers.ContentSecurityPolicy.ToString().ShouldBe("frame-ancestors 'none'");
        headers.XFrameOptions.ToString().ShouldBe("DENY");
    }

    [Fact]
    public void A_path_that_only_starts_with_the_docs_prefix_gets_the_strict_policy()
    {
        var headers = ApplyFor("/scalarevil");

        headers.ContentSecurityPolicy.ToString().ShouldBe("default-src 'none'; frame-ancestors 'none'");
    }
}
