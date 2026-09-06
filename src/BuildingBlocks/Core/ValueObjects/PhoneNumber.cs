using System.Text.RegularExpressions;

namespace Dental.Framework.Core.ValueObjects;

/// <summary>A loosely validated E.164-ish phone number. Storage keeps the normalized digits.</summary>
public readonly partial record struct PhoneNumber
{
    private PhoneNumber(string value) => Value = value;

    /// <summary>The normalized value, e.g. <c>+15551234567</c>.</summary>
    public string Value { get; }

    /// <summary>Parses and normalizes a phone number.</summary>
    /// <param name="raw">User supplied text.</param>
    /// <returns>The normalized number.</returns>
    /// <exception cref="FormatException">The value is not a plausible phone number.</exception>
    public static PhoneNumber Parse(string raw)
    {
        if (!TryParse(raw, out PhoneNumber result))
        {
            throw new FormatException($"'{raw}' is not a valid phone number.");
        }

        return result;
    }

    /// <summary>Attempts to parse and normalize a phone number.</summary>
    /// <param name="raw">User supplied text.</param>
    /// <param name="result">The normalized number when parsing succeeds.</param>
    /// <returns><see langword="true"/> when the value is a plausible phone number.</returns>
    public static bool TryParse(string? raw, out PhoneNumber result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string stripped = NonDialCharacters().Replace(raw, string.Empty);
        if (!E164().IsMatch(stripped))
        {
            return false;
        }

        result = new PhoneNumber(stripped);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex(@"[^\d+]")]
    private static partial Regex NonDialCharacters();

    [GeneratedRegex(@"^\+?[1-9]\d{6,14}$")]
    private static partial Regex E164();
}
