using System.Reflection;

namespace Dental.Modules.Tenancy.Contracts;

/// <summary>
/// Marker type used to hand this assembly to Mediator's <c>o.Assemblies</c> list.
/// </summary>
/// <remarks>
/// Mediator needs BOTH markers for a module - this one, where the messages live, and the module
/// type, where the handlers live. Supplying only one leaves handlers undiscovered, with no error at
/// startup and a runtime "no handler registered" on the first request that needs them.
/// </remarks>
public sealed class TenancyContractsMarker
{
    private TenancyContractsMarker()
    {
    }

    /// <summary>This assembly.</summary>
    public static Assembly Assembly => typeof(TenancyContractsMarker).Assembly;
}
