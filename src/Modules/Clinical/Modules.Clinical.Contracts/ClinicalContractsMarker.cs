using System.Reflection;

namespace Dental.Modules.Clinical.Contracts;

/// <summary>Marker type used to hand this assembly to Mediator's <c>o.Assemblies</c> list.</summary>
public sealed class ClinicalContractsMarker
{
    private ClinicalContractsMarker()
    {
    }

    /// <summary>This assembly.</summary>
    public static Assembly Assembly => typeof(ClinicalContractsMarker).Assembly;
}
