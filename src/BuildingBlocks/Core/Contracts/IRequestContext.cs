namespace Dental.Framework.Core.Contracts;

/// <summary>Transport level facts about the current request, independent of authentication.</summary>
public interface IRequestContext
{
    /// <summary>Correlation identifier, from <c>X-Correlation-ID</c> or the trace identifier.</summary>
    string CorrelationId { get; }

    /// <summary>Caller IP address, when one could be determined.</summary>
    string? IpAddress { get; }

    /// <summary>Caller user agent string.</summary>
    string? UserAgent { get; }
}
