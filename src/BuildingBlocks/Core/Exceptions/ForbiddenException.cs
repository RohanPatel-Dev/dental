using System.Net;

namespace Dental.Framework.Core.Exceptions;

/// <summary>Maps to HTTP 403 - the caller is known but not allowed.</summary>
public sealed class ForbiddenException : CustomException
{
    /// <summary>Creates the exception with a generic message.</summary>
    public ForbiddenException()
        : base("You are not allowed to perform this action.", null, HttpStatusCode.Forbidden)
    {
    }

    /// <summary>Creates the exception with a specific message.</summary>
    /// <param name="message">Why the action is refused.</param>
    public ForbiddenException(string message)
        : base(message, null, HttpStatusCode.Forbidden)
    {
    }

    /// <summary>Creates the exception wrapping an inner exception.</summary>
    /// <param name="message">Why the action is refused.</param>
    /// <param name="innerException">The wrapped exception.</param>
    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
