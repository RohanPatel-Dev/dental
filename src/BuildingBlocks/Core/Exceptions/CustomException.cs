using System.Net;

namespace Dental.Framework.Core.Exceptions;

/// <summary>
/// Base application exception. The global handler turns it into an RFC 9457 ProblemDetails using
/// <see cref="StatusCode"/> and <see cref="ErrorMessages"/>.
/// </summary>
/// <remarks>
/// Overloads are written out in full rather than using optional parameters, so that adding a
/// status code to a call site is a visible change instead of a silent default.
/// </remarks>
public class CustomException : Exception
{
    /// <summary>Creates an exception with the default 400 status.</summary>
    public CustomException()
        : this("An application error occurred.")
    {
    }

    /// <summary>Creates an exception with a message and the default 400 status.</summary>
    /// <param name="message">Human readable summary.</param>
    public CustomException(string message)
        : this(message, null, HttpStatusCode.BadRequest)
    {
    }

    /// <summary>Creates an exception wrapping an inner exception.</summary>
    /// <param name="message">Human readable summary.</param>
    /// <param name="innerException">The wrapped exception.</param>
    public CustomException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorMessages = [];
        StatusCode = HttpStatusCode.BadRequest;
    }

    /// <summary>Creates an exception with an explicit status.</summary>
    /// <param name="message">Human readable summary.</param>
    /// <param name="statusCode">HTTP status the handler should emit.</param>
    public CustomException(string message, HttpStatusCode statusCode)
        : this(message, null, statusCode)
    {
    }

    /// <summary>Creates a fully specified exception.</summary>
    /// <param name="message">Human readable summary.</param>
    /// <param name="errors">Per field or per rule messages surfaced in the problem document.</param>
    /// <param name="statusCode">HTTP status the handler should emit.</param>
    public CustomException(string message, IReadOnlyCollection<string>? errors, HttpStatusCode statusCode)
        : base(message)
    {
        ErrorMessages = errors ?? [];
        StatusCode = statusCode;
    }

    /// <summary>Detail messages surfaced alongside the problem document.</summary>
    public IReadOnlyCollection<string> ErrorMessages { get; }

    /// <summary>HTTP status the global handler should emit.</summary>
    public HttpStatusCode StatusCode { get; }
}
