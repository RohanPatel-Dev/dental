using System.Linq.Expressions;
using Hangfire;

namespace Dental.Framework.Jobs;

/// <summary>Hangfire backed implementation of <see cref="IJobService"/>.</summary>
/// <param name="backgroundJobClient">Hangfire client.</param>
public sealed class HangfireJobService(IBackgroundJobClient backgroundJobClient) : IJobService
{
    /// <inheritdoc />
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        where T : notnull =>
        backgroundJobClient.Enqueue(methodCall);

    /// <inheritdoc />
    public string Enqueue<T>(string queue, Expression<Func<T, Task>> methodCall)
        where T : notnull =>
        backgroundJobClient.Enqueue(queue, methodCall);

    /// <inheritdoc />
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        where T : notnull =>
        backgroundJobClient.Schedule(methodCall, delay);

    /// <inheritdoc />
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset runAt)
        where T : notnull =>
        backgroundJobClient.Schedule(methodCall, runAt);

    /// <inheritdoc />
    public bool Delete(string jobId) => backgroundJobClient.Delete(jobId);
}
