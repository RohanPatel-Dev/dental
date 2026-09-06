namespace Dental.Framework.Core.Domain;

/// <summary>
/// Opts an entity out of the automatic tenant query filter. Use for plans, outbox/inbox rows and
/// cross-tenant reference data only - never for anything holding tenant data.
/// </summary>
public interface IGlobalEntity;
