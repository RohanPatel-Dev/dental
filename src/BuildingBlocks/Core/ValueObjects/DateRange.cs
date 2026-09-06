namespace Dental.Framework.Core.ValueObjects;

/// <summary>A half open UTC interval - <see cref="Start"/> inclusive, <see cref="End"/> exclusive.</summary>
/// <param name="Start">Inclusive start.</param>
/// <param name="End">Exclusive end.</param>
public readonly record struct DateRange(DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>Length of the interval.</summary>
    public TimeSpan Duration => End - Start;

    /// <summary>True when <see cref="Start"/> is strictly before <see cref="End"/>.</summary>
    public bool IsValid => Start < End;

    /// <summary>Tests whether this interval overlaps another.</summary>
    /// <param name="other">Interval to compare with.</param>
    /// <returns><see langword="true"/> when the two intervals share at least one instant.</returns>
    public bool Overlaps(DateRange other) => Start < other.End && other.Start < End;

    /// <summary>Tests whether an instant falls inside the interval.</summary>
    /// <param name="instant">The instant to test.</param>
    /// <returns><see langword="true"/> when the instant is within the half open interval.</returns>
    public bool Contains(DateTimeOffset instant) => instant >= Start && instant < End;
}
