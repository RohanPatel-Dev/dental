using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dental.Framework.Web.Resilience;

/// <summary>
/// JSON options for MANUAL <c>ReadFromJsonAsync</c> / <c>PostAsJsonAsync</c> calls against third
/// party APIs.
/// </summary>
/// <remarks>
/// The server's minimal-API JSON configuration does not apply to an arbitrary
/// <see cref="System.Net.Http.HttpClient"/>. A missing
/// <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> does not throw - it silently
/// leaves properties at their default, so a response deserializes into a record full of
/// <see cref="Guid.Empty"/> and zeroes. Always pass these options explicitly.
/// </remarks>
public static class HeroJsonSerializerOptions
{
    /// <summary>Case insensitive, string enums, web naming.</summary>
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
