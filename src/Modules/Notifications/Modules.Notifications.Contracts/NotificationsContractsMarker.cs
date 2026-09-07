using System.Reflection;

namespace Dental.Modules.Notifications.Contracts;

/// <summary>Marker type used to hand this assembly to Mediator's <c>o.Assemblies</c> list.</summary>
public sealed class NotificationsContractsMarker
{
    private NotificationsContractsMarker()
    {
    }

    /// <summary>This assembly.</summary>
    public static Assembly Assembly => typeof(NotificationsContractsMarker).Assembly;
}
