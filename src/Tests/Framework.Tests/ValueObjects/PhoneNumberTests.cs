using Dental.Framework.Core.ValueObjects;
using Shouldly;

namespace Dental.Framework.Tests.ValueObjects;

/// <summary>
/// Reminders are sent to whatever this type stores, so normalization and rejection both matter.
/// </summary>
public sealed class PhoneNumberTests
{
    #region Happy Path

    [Theory]
    [InlineData("+1 (555) 123-4567", "+15551234567")]
    [InlineData("+44 20 7946 0958", "+442079460958")]
    [InlineData("555-123-4567", "5551234567")]
    public void Parse_Should_StripFormatting(string raw, string expected) =>
        PhoneNumber.Parse(raw).Value.ShouldBe(expected);

    [Fact]
    public void ToString_Should_ReturnTheNormalizedValue() =>
        PhoneNumber.Parse("+1 555 123 4567").ToString().ShouldBe("+15551234567");

    [Fact]
    public void TryParse_Should_ReturnTrue_And_TheNormalizedNumber()
    {
        PhoneNumber.TryParse("+1-555-123-4567", out PhoneNumber number).ShouldBeTrue();

        number.Value.ShouldBe("+15551234567");
    }

    #endregion

    #region Exception Cases

    [Fact]
    public void Parse_Should_Throw_When_TheValueIsNotPlausible()
    {
        FormatException exception = Should.Throw<FormatException>(() => PhoneNumber.Parse("12345"));

        exception.Message.ShouldContain("12345");
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("0555123456")]
    [InlineData("not a phone number")]
    [InlineData("+1234567890123456")]
    public void TryParse_Should_ReturnFalse_ForImplausibleInput(string? raw)
    {
        PhoneNumber.TryParse(raw, out PhoneNumber number).ShouldBeFalse();

        number.Value.ShouldBeNull();
    }

    [Fact]
    public void TryParse_Should_TreatDifferentFormattingsOfOneNumberAsEqual()
    {
        // Value equality is what lets the projection in Notifications de-duplicate contacts.
        PhoneNumber.TryParse("+1 (555) 123-4567", out PhoneNumber formatted).ShouldBeTrue();
        PhoneNumber.TryParse("+1.555.123.4567", out PhoneNumber dotted).ShouldBeTrue();

        formatted.ShouldBe(dotted);
    }

    #endregion
}
