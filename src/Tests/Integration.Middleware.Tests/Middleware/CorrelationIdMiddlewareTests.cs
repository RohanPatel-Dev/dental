using Dental.Framework.Shared.Http;
using Dental.Framework.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace Dental.Integration.Middleware.Tests.Middleware;

/// <summary>
/// Correlation runs first in the pipeline so that everything downstream - including a failure
/// inside authentication - can be tied back to one request in the logs.
/// </summary>
public sealed class CorrelationIdMiddlewareTests
{
    #region Happy Path

    [Fact]
    public async Task Response_Should_EchoTheCallersCorrelationId()
    {
        await using PipelineHost host = await StartAsync();
        using HttpRequestMessage request = new(HttpMethod.Get, "/ping");
        request.Headers.Add(HeaderNames.CorrelationId, "spa-generated-1234");

        using HttpResponseMessage response = await host.Client.SendAsync(
            request, TestContext.Current.CancellationToken);

        response.Headers.GetValues(HeaderNames.CorrelationId).ShouldBe(["spa-generated-1234"]);
    }

    [Fact]
    public async Task Response_Should_CarryACorrelationId_When_TheCallerSendsNone()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri("/ping", UriKind.Relative), TestContext.Current.CancellationToken);

        response.Headers.GetValues(HeaderNames.CorrelationId).ShouldHaveSingleItem().ShouldNotBeNullOrWhiteSpace();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Response_Should_GenerateACorrelationId_When_TheHeaderIsBlank()
    {
        // A proxy that adds the header unconditionally can send an empty one. Echoing that back
        // would leave the request untraceable.
        await using PipelineHost host = await StartAsync();
        using HttpRequestMessage request = new(HttpMethod.Get, "/ping");
        request.Headers.TryAddWithoutValidation(HeaderNames.CorrelationId, "   ");

        using HttpResponseMessage response = await host.Client.SendAsync(
            request, TestContext.Current.CancellationToken);

        response.Headers.GetValues(HeaderNames.CorrelationId)
            .ShouldHaveSingleItem()
            .Trim()
            .ShouldNotBeEmpty();
    }

    [Fact]
    public async Task CorrelationIds_Should_DifferBetweenRequests()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage first = await host.Client.GetAsync(
            new Uri("/ping", UriKind.Relative), TestContext.Current.CancellationToken);
        using HttpResponseMessage second = await host.Client.GetAsync(
            new Uri("/ping", UriKind.Relative), TestContext.Current.CancellationToken);

        first.Headers.GetValues(HeaderNames.CorrelationId)
            .ShouldNotBe(second.Headers.GetValues(HeaderNames.CorrelationId));
    }

    #endregion

    private static Task<PipelineHost> StartAsync() =>
        PipelineHost.StartAsync(
            _ => { },
            app =>
            {
                app.UseMiddleware<CorrelationIdMiddleware>();
                app.MapGet("/ping", () => Results.Ok());
            });
}
