using Dental.Framework.Core.ValueObjects;
using Shouldly;

namespace Dental.Framework.Tests.ValueObjects;

/// <summary>
/// The half-open interval every scheduling rule is built on. If <c>Overlaps</c> were inclusive at
/// the end, back-to-back appointments would report a double booking.
/// </summary>
public sealed class DateRangeTests
{
    private static readonly DateTimeOffset Nine = new(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);

    #region Happy Path

    [Fact]
    public void Overlaps_Should_BeTrue_When_TheIntervalsIntersect()
    {
        DateRange first = new(Nine, Nine.AddMinutes(30));
        DateRange second = new(Nine.AddMinutes(15), Nine.AddMinutes(45));

        first.Overlaps(second).ShouldBeTrue();
        second.Overlaps(first).ShouldBeTrue();
    }

    [Fact]
    public void Duration_Should_BeTheDistanceBetweenTheEnds() =>
        new DateRange(Nine, Nine.AddMinutes(45)).Duration.ShouldBe(TimeSpan.FromMinutes(45));

    [Fact]
    public void Contains_Should_BeTrue_ForAnInstantInsideTheInterval() =>
        new DateRange(Nine, Nine.AddHours(1)).Contains(Nine.AddMinutes(30)).ShouldBeTrue();

    #endregion

    #region Edge Cases

    [Fact]
    public void Overlaps_Should_BeFalse_ForBackToBackIntervals()
    {
        // 09:00-09:30 and 09:30-10:00 are adjacent, not clashing. This is the single most
        // load-bearing property of the type.
        DateRange earlier = new(Nine, Nine.AddMinutes(30));
        DateRange later = new(Nine.AddMinutes(30), Nine.AddHours(1));

        earlier.Overlaps(later).ShouldBeFalse();
        later.Overlaps(earlier).ShouldBeFalse();
    }

    [Fact]
    public void Contains_Should_ExcludeTheEndInstant()
    {
        DateRange range = new(Nine, Nine.AddMinutes(30));

        range.Contains(Nine).ShouldBeTrue();
        range.Contains(Nine.AddMinutes(30)).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_Should_BeFalse_ForAnEmptyOrInvertedInterval()
    {
        new DateRange(Nine, Nine).IsValid.ShouldBeFalse();
        new DateRange(Nine.AddHours(1), Nine).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Overlaps_Should_TreatAnEmptyIntervalAsThePointItStartsAt()
    {
        // A zero-length interval is not a valid booking - callers gate on IsValid first - but the
        // comparison still has to be defined, and it behaves as the single instant at Start.
        DateRange inside = new(Nine.AddMinutes(15), Nine.AddMinutes(15));
        DateRange atTheEnd = new(Nine.AddMinutes(30), Nine.AddMinutes(30));
        DateRange surrounding = new(Nine, Nine.AddMinutes(30));

        inside.Overlaps(surrounding).ShouldBeTrue();
        surrounding.Overlaps(inside).ShouldBeTrue();

        atTheEnd.Overlaps(surrounding).ShouldBeFalse();
        surrounding.Overlaps(atTheEnd).ShouldBeFalse();
    }

    [Fact]
    public void Overlaps_Should_CompareAcrossOffsets()
    {
        // The type stores offsets, so 09:00+00:00 and 10:00+01:00 are the same instant.
        DateRange utc = new(Nine, Nine.AddMinutes(30));
        DateRange shifted = new(
            new DateTimeOffset(2026, 9, 7, 10, 15, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 9, 7, 10, 45, 0, TimeSpan.FromHours(1)));

        utc.Overlaps(shifted).ShouldBeTrue();
    }

    #endregion
}
