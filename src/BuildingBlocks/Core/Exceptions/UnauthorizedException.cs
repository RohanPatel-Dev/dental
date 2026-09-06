using System.Net;

namespace Dental.Framework.Core.Exceptions;

/// <summary>Maps to HTTP 401 - the caller could not be authenticated.</summary>
public sealed class UnauthorizedException : CustomException
{
    /// <summary>Creates the exception with a generic message.</summary>
    public UnauthorizedException()
        : base("Authentication is required.", null, HttpStatusCode.Unauthorized)
    {
    }

    /// <summary>Creates the exception with a specific message.</summary>
    /// <param name="message">Why authentication failed.</param>
    public UnauthorizedException(string message)
        : base(message, null, HttpStatusCode.Unauthorized)
    {
    }

    /// <summary>Creates the exception wrapping an inner exception.</summary>
    /// <param name="message">Why authentication failed.</param>
    /// <param name="innerException">The wrapped exception.</param>
    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
