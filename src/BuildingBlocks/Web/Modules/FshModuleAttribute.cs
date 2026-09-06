namespace Dental.Framework.Web.Modules;

/// <summary>
/// Declares a module to the loader. Apply at ASSEMBLY level with positional arguments, above the
/// namespace declaration:
/// <code>[assembly: FshModule(typeof(Dental.Modules.Patients.PatientsModule), 900)]</code>
/// </summary>
/// <remarks>
/// A class level attribute is silently ignored - the loader scans assembly attributes only.
/// </remarks>
/// <param name="moduleType">The <see cref="IModule"/> implementation.</param>
/// <param name="order">
/// Load order. Lower runs first; ties break by module type name so ordering is deterministic.
/// Foundational modules (tenancy, identity) use low numbers; business modules use high ones.
/// </param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FshModuleAttribute(Type moduleType, int order) : Attribute
{
    /// <summary>The <see cref="IModule"/> implementation.</summary>
    public Type ModuleType { get; } = moduleType;

    /// <summary>Load order. Lower runs first.</summary>
    public int Order { get; } = order;
}
