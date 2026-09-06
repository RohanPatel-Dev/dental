using System.Reflection;

namespace Dental.Modules.Identity.Contracts;

/// <summary>Marker type used to hand this assembly to Mediator's <c>o.Assemblies</c> list.</summary>
public sealed class IdentityContractsMarker
{
    private IdentityContractsMarker()
    {
    }

    /// <summary>This assembly.</summary>
    public static Assembly Assembly => typeof(IdentityContractsMarker).Assembly;
}
