using Dental.Framework.Persistence.Initialization;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Patients.Data;

/// <summary>Migrates the patients schema and, on request, seeds demo records.</summary>
/// <param name="context">The patients context.</param>
/// <param name="logger">Logger.</param>
public sealed class PatientsDbInitializer(
    PatientsDbContext context,
    ILogger<PatientsDbInitializer> logger) : IDbInitializer
{
    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Applied pending migrations for the patients schema.");
        }
    }

    /// <inheritdoc />
    public Task SeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task SeedDemoAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Patients.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        (string First, string Last, int Year, PatientSex Sex)[] samples =
        [
            ("Ada", "Whitfield", 1984, PatientSex.Female),
            ("Marcus", "Okonjo", 1976, PatientSex.Male),
            ("Priya", "Raman", 1998, PatientSex.Female),
            ("Tomas", "Lindqvist", 1961, PatientSex.Male),
            ("Jules", "Moreau", 2005, PatientSex.Other),
        ];

        for (int index = 0; index < samples.Length; index++)
        {
            (string first, string last, int year, PatientSex sex) = samples[index];

            context.Patients.Add(new Patient
            {
                ChartNumber = $"P-{index + 1:D6}",
                FirstName = first,
                LastName = last,
                DateOfBirth = new DateOnly(year, 3, 14),
                Sex = sex,
                Email = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}@example.test",
                PhoneNumber = "+15550100",
                Status = PatientStatus.Active,
                HasReminderConsent = true,
                Allergies = index % 2 == 0 ? ["Penicillin"] : [],
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Seeded {Count} demo patients.", samples.Length);
    }
}
