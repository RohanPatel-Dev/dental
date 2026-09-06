using System.Reflection;

namespace Dental.Modules.Auditing.Contracts;

/// <summary>Marker type used to hand this assembly to Mediator's <c>o.Assemblies</c> list.</summary>
public sealed class AuditingContractsMarker
{
    private AuditingContractsMarker()
    {
    }

    /// <summary>This assembly.</summary>
    public static Assembly Assembly => typeof(AuditingContractsMarker).Assembly;
}
