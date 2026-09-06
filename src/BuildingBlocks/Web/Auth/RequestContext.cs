using Dental.Framework.Core.Contracts;
using Dental.Framework.Shared.Http;
using Microsoft.AspNetCore.Http;

namespace Dental.Framework.Web.Auth;

/// <summary>Transport level facts about the current request.</summary>
/// <param name="httpContextAccessor">Supplies the current request.</param>
public sealed class RequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    /// <inheritdoc />
    public string CorrelationId
    {
        get
        {
            HttpContext? context = httpContextAccessor.HttpContext;
            if (context is null)
            {
                return string.Empty;
            }

            string? header = context.Request.Headers[HeaderNames.CorrelationId];
            return string.IsNullOrWhiteSpace(header) ? context.TraceIdentifier : header;
        }
    }

    /// <inheritdoc />
    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent;
}
