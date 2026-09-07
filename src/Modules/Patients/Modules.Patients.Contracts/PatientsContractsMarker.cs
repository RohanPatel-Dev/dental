using System.Reflection;

namespace Dental.Modules.Patients.Contracts;

/// <summary>Marker type used to hand this assembly to Mediator's <c>o.Assemblies</c> list.</summary>
public sealed class PatientsContractsMarker
{
    private PatientsContractsMarker()
    {
    }

    /// <summary>This assembly.</summary>
    public static Assembly Assembly => typeof(PatientsContractsMarker).Assembly;
}
