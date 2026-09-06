using System.Net;

namespace Dental.Framework.Core.Exceptions;

/// <summary>Maps to HTTP 404. Also thrown for rows that exist but belong to another tenant.</summary>
public sealed class NotFoundException : CustomException
{
    /// <summary>Creates the exception with a generic message.</summary>
    public NotFoundException()
        : base("The requested resource was not found.", null, HttpStatusCode.NotFound)
    {
    }

    /// <summary>Creates the exception with a specific message.</summary>
    /// <param name="message">What could not be found.</param>
    public NotFoundException(string message)
        : base(message, null, HttpStatusCode.NotFound)
    {
    }

    /// <summary>Creates the exception wrapping an inner exception.</summary>
    /// <param name="message">What could not be found.</param>
    /// <param name="innerException">The wrapped exception.</param>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Builds a message of the form <c>"Patient with id {id} was not found."</c>.</summary>
    /// <param name="entityName">Entity type name.</param>
    /// <param name="id">Identifier that was looked up.</param>
    /// <returns>The exception.</returns>
    public static NotFoundException For(string entityName, object id) =>
        new($"{entityName} with id '{id}' was not found.");
}
