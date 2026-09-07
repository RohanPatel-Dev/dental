using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dental.Framework.Core.Exceptions;
using Dental.Framework.Shared.Http;
using Dental.Framework.Web.Exceptions;
using Dental.Framework.Web.Middleware;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dental.Integration.Middleware.Tests.Middleware;

/// <summary>
/// The single place exceptions become status codes. Every response the SPAs handle is shaped here,
/// so the contract is pinned rather than assumed.
/// </summary>
public sealed class GlobalExceptionHandlerTests
{
    #region Happy Path

    [Fact]
    public async Task ValidationFailure_Should_Be400_WithPerFieldErrors()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri("/invalid", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        JsonElement problem = await ReadAsync(response);
        problem.GetProperty("title").GetString().ShouldBe("One or more validation errors occurred.");
        problem.GetProperty("errors").GetProperty("Email")[0].GetString()
            .ShouldBe("'Email' is required.");
    }

    [Theory]
    [InlineData("/not-found", HttpStatusCode.NotFound)]
    [InlineData("/conflict", HttpStatusCode.Conflict)]
    [InlineData("/forbidden", HttpStatusCode.Forbidden)]
    [InlineData("/unauthorized", HttpStatusCode.Unauthorized)]
    public async Task DomainException_Should_MapToItsOwnStatusCode(string path, HttpStatusCode expected)
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expected);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task UnexpectedFailure_Should_Be500_WithoutLeakingTheMessage()
    {
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri("/boom", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        JsonElement problem = await ReadAsync(response);
        problem.GetProperty("title").GetString().ShouldBe("An unexpected error occurred.");
        problem.ToString().ShouldNotContain("connection string");
    }

    [Fact]
    public async Task MalformedBody_Should_Be400_NotAServerError()
    {
        // With ThrowOnBadRequest - the default in Development - a body that cannot be bound reaches
        // the handler as an exception. Without the BadHttpRequestException arm it falls through to
        // the catch-all and reports a 500 for what is squarely the caller's mistake.
        await using PipelineHost host = await StartAsync();
        using StringContent body = new("{ this is not json", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await host.Client.PostAsync(
            new Uri("/bind", UriKind.Relative), body, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ReadAsync(response)).GetProperty("title").GetString()
            .ShouldBe("The request could not be read.");
    }

    [Fact]
    public async Task Problem_Should_CarryTheCorrelationId_InBothTheBodyAndTheHeader()
    {
        // Support pastes one identifier into the log search. The handler re-stamps the header
        // because the failing response is written from scratch.
        await using PipelineHost host = await StartAsync();
        using HttpRequestMessage request = new(HttpMethod.Get, "/boom");
        request.Headers.Add(HeaderNames.CorrelationId, "corr-9876");

        using HttpResponseMessage response = await host.Client.SendAsync(
            request, TestContext.Current.CancellationToken);

        response.Headers.GetValues(HeaderNames.CorrelationId).ShouldBe(["corr-9876"]);
        (await ReadAsync(response)).GetProperty("correlationId").GetString().ShouldBe("corr-9876");
    }

    [Fact]
    public async Task ConflictProblem_Should_CarryTheDomainMessageAsTheTitle()
    {
        // The SPAs show this text directly, so it is part of the contract, not an internal detail.
        await using PipelineHost host = await StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync(
            new Uri("/conflict", UriKind.Relative), TestContext.Current.CancellationToken);

        (await ReadAsync(response)).GetProperty("title").GetString()
            .ShouldBe("That slot is already booked.");
    }

    #endregion

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

    private static Task<PipelineHost> StartAsync() =>
        PipelineHost.StartAsync(
            services =>
            {
                services.AddProblemDetails();
                services.AddExceptionHandler<GlobalExceptionHandler>();

                // Explicit rather than inherited: the framework turns this on in Development only,
                // and the arm under test exists for exactly that path.
                services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
            },
            app =>
            {
                app.UseExceptionHandler();
                app.UseMiddleware<CorrelationIdMiddleware>();

                app.MapGet("/invalid", void () => throw new ValidationException(
                    [new ValidationFailure("Email", "'Email' is required.")]));
                app.MapGet("/not-found", void () => throw NotFoundException.For("Patient", Guid.Empty));
                app.MapGet("/conflict", void () => throw new ConflictException("That slot is already booked."));
                app.MapGet("/forbidden", void () => throw new ForbiddenException("Not your practice."));
                app.MapGet("/unauthorized", void () => throw new UnauthorizedException("Sign in first."));
                app.MapGet("/boom", void () => throw new InvalidOperationException(
                    "connection string Host=db;Password=hunter2"));
                app.MapPost("/bind", (Payload payload) => Results.Ok(payload));
            });
}

/// <summary>Body shape used only to provoke a binding failure.</summary>
/// <param name="Name">Any property; the test never sends valid JSON for it.</param>
public sealed record Payload(string Name);
