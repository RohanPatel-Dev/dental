using System.Net;

namespace Dental.Framework.Core.Exceptions;

/// <summary>Maps to HTTP 409 - the request collides with the current state of the resource.</summary>
public sealed class ConflictException : CustomException
{
    /// <summary>Creates the exception with a generic message.</summary>
    public ConflictException()
        : base("The request conflicts with the current state of the resource.", null, HttpStatusCode.Conflict)
    {
    }

    /// <summary>Creates the exception with a specific message.</summary>
    /// <param name="message">What conflicts.</param>
    public ConflictException(string message)
        : base(message, null, HttpStatusCode.Conflict)
    {
    }

    /// <summary>Creates the exception wrapping an inner exception.</summary>
    /// <param name="message">What conflicts.</param>
    /// <param name="innerException">The wrapped exception.</param>
    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
