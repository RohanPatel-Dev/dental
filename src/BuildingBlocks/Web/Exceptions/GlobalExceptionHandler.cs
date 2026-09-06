using System.Diagnostics;
using System.Net;
using Dental.Framework.Core.Exceptions;
using Dental.Framework.Shared.Http;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Web.Exceptions;

/// <summary>
/// Turns every unhandled exception into an RFC 9457 ProblemDetails response.
/// </summary>
/// <remarks>
/// This is the ONLY place exceptions become HTTP status codes. Feature code throws the domain
/// exception and stops; it never catches broadly to convert an error into a response.
/// </remarks>
/// <param name="environment">Host environment, so stack traces stay out of production responses.</param>
/// <param name="logger">Logger.</param>
public sealed class GlobalExceptionHandler(
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client hung up. Nothing to report and nothing to write to.
            return true;
        }

        string correlationId = ResolveCorrelationId(httpContext);
        ProblemDetails problem = Build(exception, correlationId);

        logger.LogError(
            exception,
            "Request {Method} {Path} failed with {StatusCode} (correlation {CorrelationId}).",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            problem.Status,
            correlationId);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.Headers[HeaderNames.CorrelationId] = correlationId;

        await httpContext.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json",
                cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    private ProblemDetails Build(Exception exception, string correlationId)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => new ProblemDetails
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Extensions =
                {
                    ["errors"] = validation.Errors
                        .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray(),
                            StringComparer.Ordinal),
                },
            },
            CustomException custom => new ProblemDetails
            {
                Title = custom.Message,
                Status = (int)custom.StatusCode,
                Extensions = custom.ErrorMessages.Count > 0
                    ? new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["errors"] = custom.ErrorMessages.ToArray(),
                    }
                    : new Dictionary<string, object?>(StringComparer.Ordinal),
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Title = "Authentication is required.",
                Status = (int)HttpStatusCode.Unauthorized,
            },
            _ => new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
            },
        };

        problem.Extensions["correlationId"] = correlationId;
        problem.Extensions["traceId"] = Activity.Current?.Id;

        if (environment.IsDevelopment())
        {
            problem.Detail = exception.ToString();
        }

        return problem;
    }

    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        string? header = httpContext.Request.Headers[HeaderNames.CorrelationId];
        return string.IsNullOrWhiteSpace(header) ? httpContext.TraceIdentifier : header;
    }
}
