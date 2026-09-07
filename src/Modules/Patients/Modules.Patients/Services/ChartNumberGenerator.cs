using System.Globalization;
using Dental.Modules.Patients.Data;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Patients.Services;

/// <summary>Allocates the next practice-visible chart number for a tenant.</summary>
/// <param name="context">The patients context.</param>
public sealed class ChartNumberGenerator(PatientsDbContext context)
{
    /// <summary>Produces the next chart number, of the form <c>P-000123</c>.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chart number.</returns>
    /// <remarks>
    /// The unique index on (TenantId, ChartNumber) is the real guarantee here. Two concurrent
    /// registrations can compute the same number; one of them loses the insert and retries, which
    /// is a better trade than serializing every registration behind a lock.
    /// </remarks>
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        int count = await context.Patients
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == context.CurrentTenantId)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        return string.Create(CultureInfo.InvariantCulture, $"P-{count + 1:D6}");
    }
}
