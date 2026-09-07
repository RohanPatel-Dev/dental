using System.Net;
using Dental.Framework.Shared.Http;
using Dental.Framework.Shared.Tenancy;
using Dental.Framework.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dental.Integration.Middleware.Tests.Middleware;

/// <summary>
/// Replay protection. A patient double-tapping "Book" must not create two appointments, and a
/// key from one practice must never replay another practice's response.
/// </summary>
public sealed class IdempotencyMiddlewareTests
{
    private readonly Handler _handler = new();

    #region Happy Path

    [Fact]
    public async Task Repeat_Should_ReplayTheRecordedResponse_WithoutRunningTheHandlerAgain()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage first = await PostAsync(host, "/appointments", "key-1");
        using HttpResponseMessage second = await PostAsync(host, "/appointments", "key-1");

        first.Headers.Contains(HeaderNames.IdempotencyReplayed).ShouldBeFalse();
        second.Headers.GetValues(HeaderNames.IdempotencyReplayed).ShouldBe(["true"]);

        (await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldBe(await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        _handler.Invocations.ShouldBe(1);
    }

    [Fact]
    public async Task DifferentKeys_Should_EachRunTheHandler()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage first = await PostAsync(host, "/appointments", "key-1");
        using HttpResponseMessage second = await PostAsync(host, "/appointments", "key-2");

        _handler.Invocations.ShouldBe(2);
        (await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldNotBe(await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Endpoint_Should_RunTwice_When_ItDidNotOptIn()
    {
        // Opt-in, not blanket: recording every POST would replay responses for endpoints whose
        // second call is meant to do something different.
        await using PipelineHost host = await StartAsync();

        await (await PostAsync(host, "/not-idempotent", "key-1")).Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken);
        await (await PostAsync(host, "/not-idempotent", "key-1")).Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken);

        _handler.Invocations.ShouldBe(2);
    }

    [Fact]
    public async Task Request_Should_RunNormally_When_NoKeyIsSupplied()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage first = await PostAsync(host, "/appointments", key: null);
        using HttpResponseMessage second = await PostAsync(host, "/appointments", key: null);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        _handler.Invocations.ShouldBe(2);
    }

    [Fact]
    public async Task Key_Should_BeScopedToTheTenant()
    {
        // Client generated keys collide across tenants. Sharing the cache entry would hand one
        // practice another practice's response body.
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage first = await PostAsync(host, "/appointments", "key-1", tenant: "acme");
        using HttpResponseMessage second = await PostAsync(host, "/appointments", "key-1", tenant: "other");

        second.Headers.Contains(HeaderNames.IdempotencyReplayed).ShouldBeFalse();
        _handler.Invocations.ShouldBe(2);
    }

    [Fact]
    public async Task Failure_Should_NotBeRecorded()
    {
        // Only 2xx responses are replayable: caching a 500 would make a transient outage permanent
        // for that key.
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage failed = await PostAsync(host, "/flaky", "key-1");
        failed.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        using HttpResponseMessage retried = await PostAsync(host, "/flaky", "key-1");

        retried.StatusCode.ShouldBe(HttpStatusCode.OK);
        retried.Headers.Contains(HeaderNames.IdempotencyReplayed).ShouldBeFalse();
    }

    #endregion

    #region Exception Cases

    [Fact]
    public async Task Request_Should_BeRejected_When_TheKeyIsOverlyLong()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await PostAsync(host, "/appointments", new string('k', 129));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _handler.Invocations.ShouldBe(0);
    }

    #endregion

    private static async Task<HttpResponseMessage> PostAsync(
        PipelineHost host,
        string path,
        string? key,
        string tenant = "acme")
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path);
        request.Headers.Add(TenantConstants.Header, tenant);

        if (key is not null)
        {
            request.Headers.Add(HeaderNames.IdempotencyKey, key);
        }

        return await host.Client.SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }

    private Task<PipelineHost> StartAsync()
    {
        Handler handler = _handler;

        return PipelineHost.StartAsync(
            services => services.AddHybridCache(),
            app =>
            {
                app.UseMiddleware<IdempotencyMiddleware>();

                app.MapPost("/appointments", handler.Handle).WithIdempotency();
                app.MapPost("/not-idempotent", handler.Handle);
                app.MapPost("/flaky", handler.HandleFlaky).WithIdempotency();
            });
    }

    /// <summary>Counts executions so a replay is distinguishable from a re-run.</summary>
    private sealed class Handler
    {
        private int _invocations;

        internal int Invocations => Volatile.Read(ref _invocations);

        internal IResult Handle()
        {
            Interlocked.Increment(ref _invocations);
            return Results.Ok(new { appointmentId = Guid.CreateVersion7() });
        }

        internal IResult HandleFlaky() =>
            Interlocked.Increment(ref _invocations) == 1
                ? Results.StatusCode(StatusCodes.Status500InternalServerError)
                : Results.Ok(new { recovered = true });
    }
}
