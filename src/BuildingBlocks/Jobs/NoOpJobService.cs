using System.Linq.Expressions;

namespace Dental.Framework.Jobs;

/// <summary>
/// Job service for processes that must never enqueue work - notably the one-shot DbMigrator.
/// </summary>
/// <remarks>
/// Every method throws. A seeder that accidentally enqueues a job fails loudly during migration
/// instead of writing a job no running host will ever pick up.
/// </remarks>
public sealed class NoOpJobService : IJobService
{
    /// <inheritdoc />
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        where T : notnull => throw Unsupported();

    /// <inheritdoc />
    public string Enqueue<T>(string queue, Expression<Func<T, Task>> methodCall)
        where T : notnull => throw Unsupported();

    /// <inheritdoc />
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        where T : notnull => throw Unsupported();

    /// <inheritdoc />
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset runAt)
        where T : notnull => throw Unsupported();

    /// <inheritdoc />
    public bool Delete(string jobId) => throw Unsupported();

    private static InvalidOperationException Unsupported() =>
        new("This process does not run background jobs. Enqueueing from here is a bug: the job "
            + "would be written but never executed.");
}
