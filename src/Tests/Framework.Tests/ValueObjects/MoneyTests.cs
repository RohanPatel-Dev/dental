using Dental.Framework.Core.ValueObjects;
using Shouldly;

namespace Dental.Framework.Tests.ValueObjects;

/// <summary>
/// Money exists so that a currency mismatch is a crash rather than a wrong total on an invoice.
/// </summary>
public sealed class MoneyTests
{
    #region Happy Path

    [Fact]
    public void Add_Should_SumAmounts_When_TheCurrenciesMatch()
    {
        Money sum = new Money(30m, "USD") + new Money(12.50m, "USD");

        sum.Amount.ShouldBe(42.50m);
        sum.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Subtract_Should_ProduceTheDifference_When_TheCurrenciesMatch()
    {
        Money difference = new Money(100m, "USD") - new Money(65m, "USD");

        difference.Amount.ShouldBe(35m);
    }

    [Fact]
    public void Zero_Should_NormalizeTheCurrencyToUpperCase()
    {
        Money zero = Money.Zero("usd");

        zero.Amount.ShouldBe(0m);
        zero.Currency.ShouldBe("USD");
    }

    #endregion

    #region Exception Cases

    [Fact]
    public void Add_Should_Throw_When_TheCurrenciesDiffer()
    {
        InvalidOperationException exception =
            Should.Throw<InvalidOperationException>(() => new Money(30m, "USD") + new Money(30m, "EUR"));

        exception.Message.ShouldContain("USD");
        exception.Message.ShouldContain("EUR");
    }

    [Fact]
    public void Subtract_Should_Throw_When_TheCurrenciesDiffer() =>
        Should.Throw<InvalidOperationException>(() => new Money(30m, "USD") - new Money(30m, "GBP"));

    [Fact]
    public void Zero_Should_Throw_When_TheCurrencyIsBlank() =>
        Should.Throw<ArgumentException>(() => Money.Zero("  "));

    #endregion

    #region Edge Cases

    [Fact]
    public void Add_Should_TreatCurrencyCaseInsensitively()
    {
        // The comparison is ordinal-ignore-case, so a lower case code from a payment gateway does
        // not look like a different currency.
        Money sum = new Money(10m, "USD").Add(new Money(5m, "usd"));

        sum.Amount.ShouldBe(15m);
    }

    [Fact]
    public void Subtract_Should_AllowANegativeResult()
    {
        // Money is not a balance type: going negative is the caller's business rule, not this one's.
        Money difference = new Money(10m, "USD") - new Money(25m, "USD");

        difference.Amount.ShouldBe(-15m);
    }

    [Fact]
    public void ToString_Should_UseInvariantFormatting()
    {
        // A comma decimal separator from the ambient culture would corrupt logs and audit records.
        new Money(1234.5m, "USD").ToString().ShouldBe("1234.50 USD");
    }

    #endregion
}
