using System.Reflection;
using Dental.Framework.Web.Modules;

namespace Dental.Architecture.Tests;

/// <summary>
/// The assemblies under test, resolved through types rather than by scanning a bin directory.
/// </summary>
/// <remarks>
/// Loading by path is how these tests quietly stop covering a new module: the assembly is simply
/// absent from the folder and every rule passes vacuously. Naming a type per assembly means a
/// deleted or renamed module breaks the build here instead.
/// </remarks>
public static class ArchitectureFixture
{
    /// <summary>Every runtime module assembly.</summary>
    public static IReadOnlyList<Assembly> ModuleAssemblies { get; } =
    [
        typeof(Modules.Tenancy.TenancyModule).Assembly,
        typeof(Modules.Identity.IdentityModule).Assembly,
        typeof(Modules.Auditing.AuditingModule).Assembly,
        typeof(Modules.Patients.PatientsModule).Assembly,
        typeof(Modules.Scheduling.SchedulingModule).Assembly,
        typeof(Modules.Clinical.ClinicalModule).Assembly,
        typeof(Modules.Billing.BillingModule).Assembly,
        typeof(Modules.Notifications.NotificationsModule).Assembly,
    ];

    /// <summary>Every module Contracts assembly.</summary>
    public static IReadOnlyList<Assembly> ContractsAssemblies { get; } =
    [
        typeof(Modules.Tenancy.Contracts.TenancyContractsMarker).Assembly,
        typeof(Modules.Identity.Contracts.IdentityContractsMarker).Assembly,
        typeof(Modules.Auditing.Contracts.AuditingContractsMarker).Assembly,
        typeof(Modules.Patients.Contracts.PatientsContractsMarker).Assembly,
        typeof(Modules.Scheduling.Contracts.SchedulingContractsMarker).Assembly,
        typeof(Modules.Clinical.Contracts.ClinicalContractsMarker).Assembly,
        typeof(Modules.Billing.Contracts.BillingContractsMarker).Assembly,
        typeof(Modules.Notifications.Contracts.NotificationsContractsMarker).Assembly,
    ];

    /// <summary>Every BuildingBlocks assembly.</summary>
    public static IReadOnlyList<Assembly> FrameworkAssemblies { get; } =
    [
        typeof(Framework.Core.Domain.BaseEntity).Assembly,
        typeof(Framework.Shared.Identity.PermissionConstants).Assembly,
        typeof(Framework.Persistence.Contexts.BaseDbContext).Assembly,
        typeof(Framework.Eventing.Abstractions.IIntegrationEvent).Assembly,
        typeof(Framework.Eventing.Outbox.OutboxMessage).Assembly,
        typeof(Framework.Caching.CachingOptions).Assembly,
        typeof(Framework.Jobs.IJobService).Assembly,
        typeof(Framework.Mailing.IMailService).Assembly,
        typeof(Framework.Storage.IStorageService).Assembly,
        typeof(Framework.Quota.IQuotaService).Assembly,
        typeof(IModule).Assembly,
    ];

    /// <summary>Simple names of every runtime module assembly.</summary>
    public static IReadOnlyList<string> ModuleAssemblyNames { get; } =
        [.. ModuleAssemblies.Select(a => a.GetName().Name!)];

    /// <summary>Describes an assembly's dependencies, for failure messages that name the culprit.</summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The referenced assembly simple names.</returns>
    public static IReadOnlyList<string> ReferencedAssemblyNames(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return [.. assembly.GetReferencedAssemblies().Select(a => a.Name!)];
    }
}
