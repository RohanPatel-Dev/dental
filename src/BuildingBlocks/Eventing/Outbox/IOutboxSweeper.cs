namespace Dental.Framework.Eventing.Outbox;

/// <summary>
/// Publishes one module's pending outbox rows. One implementation is registered per module
/// <c>DbContext</c>, and the dispatcher hosted service sweeps all of them each tick.
/// </summary>
public interface IOutboxSweeper
{
    /// <summary>Simple name of the assembly whose outbox table this sweeper drains.</summary>
    string OwnerAssembly { get; }

    /// <summary>Publishes up to one batch of pending messages.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of messages published successfully.</returns>
    Task<int> SweepAsync(CancellationToken cancellationToken = default);
}
