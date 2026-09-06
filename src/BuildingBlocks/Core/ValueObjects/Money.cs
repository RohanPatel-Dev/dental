using System.Globalization;

namespace Dental.Framework.Core.ValueObjects;

/// <summary>
/// An amount in a single currency. Arithmetic across currencies throws rather than silently
/// producing a wrong total.
/// </summary>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">ISO 4217 currency code, upper case.</param>
public readonly record struct Money(decimal Amount, string Currency)
{
    /// <summary>Zero in the given currency.</summary>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <returns>A zero amount.</returns>
    public static Money Zero(string currency) => new(0m, Normalize(currency));

    /// <summary>Adds two amounts of the same currency.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>The sum.</returns>
    public static Money operator +(Money left, Money right) => left.Add(right);

    /// <summary>Subtracts two amounts of the same currency.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>The difference.</returns>
    public static Money operator -(Money left, Money right) => left.Subtract(right);

    /// <summary>Adds two amounts of the same currency.</summary>
    /// <param name="other">Amount to add.</param>
    /// <returns>The sum.</returns>
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    /// <summary>Subtracts an amount of the same currency.</summary>
    /// <param name="other">Amount to subtract.</param>
    /// <returns>The difference.</returns>
    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount - other.Amount };
    }

    /// <inheritdoc />
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");

    private static string Normalize(string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        return currency.ToUpperInvariant();
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cannot combine amounts in '{Currency}' and '{other.Currency}'.");
        }
    }
}
