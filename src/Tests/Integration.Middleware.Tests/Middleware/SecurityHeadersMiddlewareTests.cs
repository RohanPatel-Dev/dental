using Dental.Framework.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Dental.Integration.Middleware.Tests.Middleware;

/// <summary>The hardening headers, and the one documented hole in them.</summary>
public sealed class SecurityHeadersMiddlewareTests
{
    #region Happy Path

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    [InlineData("Permissions-Policy", "camera=(), microphone=(), geolocation=()")]
    public async Task Response_Should_CarryTheHardeningHeaders(string header, string expected)
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri("/patients", UriKind.Relative), TestContext.Current.CancellationToken);

        response.Headers.GetValues(header).ShouldBe([expected]);
    }

    [Fact]
    public async Task Response_Should_CarryAContentSecurityPolicy()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri("/patients", UriKind.Relative), TestContext.Current.CancellationToken);

        string policy = response.Headers.GetValues("Content-Security-Policy").ShouldHaveSingleItem();
        policy.ShouldContain("default-src 'self'");
        policy.ShouldContain("frame-ancestors 'none'");
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("/scalar")]
    [InlineData("/scalar/v1")]
    [InlineData("/openapi/v1.json")]
    public async Task ApiReference_Should_BeExemptFromTheContentSecurityPolicy(string path)
    {
        // Scalar loads and executes its own scripts. Under the strict policy the page renders blank
        // with nothing in the server log to explain it.
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.Headers.Contains("Content-Security-Policy").ShouldBeFalse();
        response.Headers.Contains("X-Content-Type-Options").ShouldBeTrue();
    }

    [Fact]
    public async Task Hsts_Should_BeOmittedOverPlainHttp()
    {
        // Sending HSTS over http is meaningless, and pinning it from a local dev host would lock a
        // developer's browser out of the plain-http port.
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri("/patients", UriKind.Relative), TestContext.Current.CancellationToken);

        response.Headers.Contains("Strict-Transport-Security").ShouldBeFalse();
    }

    [Fact]
    public async Task Hsts_Should_BeSentOverHttps()
    {
        await using PipelineHost host = await StartAsync();
        using HttpRequestMessage request = new(HttpMethod.Get, "https://localhost/patients");

        using HttpResponseMessage response = await host.Client.SendAsync(
            request, TestContext.Current.CancellationToken);

        response.Headers.GetValues("Strict-Transport-Security")
            .ShouldBe(["max-age=31536000; includeSubDomains"]);
    }

    #endregion

    private static Task<PipelineHost> StartAsync() =>
        PipelineHost.StartAsync(
            _ => { },
            app =>
            {
                app.UseMiddleware<SecurityHeadersMiddleware>();
                app.MapGet("/{**path}", () => Results.Ok());
            });
}
