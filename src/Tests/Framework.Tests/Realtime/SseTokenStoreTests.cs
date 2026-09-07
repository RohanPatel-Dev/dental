using Dental.Framework.Web.Realtime;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Dental.Framework.Tests.Realtime;

/// <summary>
/// The token is the only credential on the anonymous SSE stream endpoint, so it has to be
/// single-use and short lived. Both properties are tested against a fake clock rather than by
/// waiting.
/// </summary>
public sealed class SseTokenStoreTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero));

    #region Happy Path

    [Fact]
    public void TryConsume_Should_ReturnTheUser_ForAFreshToken()
    {
        SseTokenStore store = new(_clock);
        Guid userId = Guid.CreateVersion7();

        Guid token = store.Issue(userId);

        store.TryConsume(token, out Guid consumed).ShouldBeTrue();
        consumed.ShouldBe(userId);
    }

    [Fact]
    public void Issue_Should_ProduceADistinctTokenEachTime()
    {
        SseTokenStore store = new(_clock);
        Guid userId = Guid.CreateVersion7();

        store.Issue(userId).ShouldNotBe(store.Issue(userId));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TryConsume_Should_ReturnFalse_TheSecondTime()
    {
        // Single use: a token leaked from a browser history or a proxy log is already spent.
        SseTokenStore store = new(_clock);
        Guid token = store.Issue(Guid.CreateVersion7());

        store.TryConsume(token, out _).ShouldBeTrue();

        store.TryConsume(token, out Guid replayed).ShouldBeFalse();
        replayed.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void TryConsume_Should_ReturnFalse_After30Seconds()
    {
        SseTokenStore store = new(_clock);
        Guid token = store.Issue(Guid.CreateVersion7());

        _clock.Advance(TimeSpan.FromSeconds(31));

        store.TryConsume(token, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryConsume_Should_StillSucceed_JustInsideTheWindow()
    {
        SseTokenStore store = new(_clock);
        Guid token = store.Issue(Guid.CreateVersion7());

        _clock.Advance(TimeSpan.FromSeconds(29));

        store.TryConsume(token, out _).ShouldBeTrue();
    }

    [Fact]
    public void TryConsume_Should_ReturnFalse_ForATokenThatWasNeverIssued() =>
        new SseTokenStore(_clock).TryConsume(Guid.CreateVersion7(), out _).ShouldBeFalse();

    [Fact]
    public void Issue_Should_SweepTokensThatExpiredWhileNobodyConnected()
    {
        // Without the sweep, a tab that requests a token and never connects leaks an entry for the
        // life of the process.
        SseTokenStore store = new(_clock);
        Guid abandoned = store.Issue(Guid.CreateVersion7());

        _clock.Advance(TimeSpan.FromMinutes(5));
        Guid fresh = store.Issue(Guid.CreateVersion7());

        store.TryConsume(abandoned, out _).ShouldBeFalse();
        store.TryConsume(fresh, out _).ShouldBeTrue();
    }

    [Fact]
    public void TryConsume_Should_KeepTokensIndependentAcrossUsers()
    {
        SseTokenStore store = new(_clock);
        Guid first = Guid.CreateVersion7();
        Guid second = Guid.CreateVersion7();

        Guid firstToken = store.Issue(first);
        Guid secondToken = store.Issue(second);

        store.TryConsume(secondToken, out Guid secondUser).ShouldBeTrue();
        secondUser.ShouldBe(second);

        store.TryConsume(firstToken, out Guid firstUser).ShouldBeTrue();
        firstUser.ShouldBe(first);
    }

    #endregion
}
