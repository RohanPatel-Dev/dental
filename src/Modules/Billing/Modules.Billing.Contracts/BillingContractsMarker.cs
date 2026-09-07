using System.Reflection;

namespace Dental.Modules.Billing.Contracts;

/// <summary>Marker type used to hand this assembly to Mediator's <c>o.Assemblies</c> list.</summary>
public sealed class BillingContractsMarker
{
    private BillingContractsMarker()
    {
    }

    /// <summary>This assembly.</summary>
    public static Assembly Assembly => typeof(BillingContractsMarker).Assembly;
}
