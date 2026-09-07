using System.Reflection;

namespace Dental.Modules.Scheduling.Contracts;

/// <summary>Marker type used to hand this assembly to Mediator's <c>o.Assemblies</c> list.</summary>
public sealed class SchedulingContractsMarker
{
    private SchedulingContractsMarker()
    {
    }

    /// <summary>This assembly.</summary>
    public static Assembly Assembly => typeof(SchedulingContractsMarker).Assembly;
}
