namespace Dental.Modules.Clinical.Contracts.Services;

/// <summary>
/// Lets another module add its own rows to a patient's clinical timeline without Clinical having to
/// know that module exists.
/// </summary>
/// <remarks>
/// The contributor pattern inverts the dependency: Clinical owns the interface, other modules
/// register implementations, and Clinical fans out to whatever is registered. Without it, the
/// timeline would force Clinical to reference Billing, Scheduling and everything else added later.
/// </remarks>
public interface IChartContributor
{
    /// <summary>Order this contributor's entries appear in, low first.</summary>
    int Order { get; }

    /// <summary>Reads this contributor's entries for one patient.</summary>
    /// <param name="patientId">Patient identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The timeline entries this module contributes.</returns>
    Task<IReadOnlyList<ChartTimelineEntry>> GetEntriesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}

/// <summary>One row on a patient's clinical timeline.</summary>
/// <param name="OccurredAt">When it happened.</param>
/// <param name="Source">Module that contributed it.</param>
/// <param name="Kind">Short machine-readable kind, e.g. <c>invoice.raised</c>.</param>
/// <param name="Summary">One line describing the entry.</param>
/// <param name="ReferenceId">Identifier of the underlying record, for a deep link.</param>
public sealed record ChartTimelineEntry(
    DateTimeOffset OccurredAt,
    string Source,
    string Kind,
    string Summary,
    Guid? ReferenceId);
