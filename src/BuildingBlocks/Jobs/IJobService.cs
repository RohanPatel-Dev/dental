using System.Linq.Expressions;

namespace Dental.Framework.Jobs;

/// <summary>
/// The only supported way to schedule background work. Never call Hangfire's static
/// <c>BackgroundJob</c> from feature code - it cannot be substituted in a test and it bypasses the
/// migrator's no-op guard.
/// </summary>
/// <remarks>
/// Recurring jobs deliberately have no API here: register them from the module's
/// <c>MapEndpoints</c> using <c>IRecurringJobManager.AddOrUpdate&lt;T&gt;</c>.
/// </remarks>
public interface IJobService
{
    /// <summary>Enqueues work on the default queue.</summary>
    /// <typeparam name="T">Job class, resolved from DI when it runs.</typeparam>
    /// <param name="methodCall">The call to make.</param>
    /// <returns>The Hangfire job identifier.</returns>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        where T : notnull;

    /// <summary>Enqueues work on a named queue.</summary>
    /// <typeparam name="T">Job class, resolved from DI when it runs.</typeparam>
    /// <param name="queue">Queue name from <see cref="JobQueues"/>.</param>
    /// <param name="methodCall">The call to make.</param>
    /// <returns>The Hangfire job identifier.</returns>
    string Enqueue<T>(string queue, Expression<Func<T, Task>> methodCall)
        where T : notnull;

    /// <summary>Schedules work to run after a delay.</summary>
    /// <typeparam name="T">Job class, resolved from DI when it runs.</typeparam>
    /// <param name="methodCall">The call to make.</param>
    /// <param name="delay">How long to wait.</param>
    /// <returns>The Hangfire job identifier.</returns>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        where T : notnull;

    /// <summary>Schedules work to run at an absolute time.</summary>
    /// <typeparam name="T">Job class, resolved from DI when it runs.</typeparam>
    /// <param name="methodCall">The call to make.</param>
    /// <param name="runAt">When to run.</param>
    /// <returns>The Hangfire job identifier.</returns>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset runAt)
        where T : notnull;

    /// <summary>Cancels a previously scheduled job.</summary>
    /// <param name="jobId">Identifier returned when the job was created.</param>
    /// <returns><see langword="true"/> when the job was found and deleted.</returns>
    bool Delete(string jobId);
}
