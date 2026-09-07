namespace Dental.Framework.Tests.Identity;

/// <summary>
/// Keeps the registry tests off the parallel path: they mutate process-wide static state, so
/// running them alongside anything else that reads it would be flaky.
/// </summary>
[CollectionDefinition(nameof(PermissionConstantsTests), DisableParallelization = true)]
public sealed class PermissionConstantsTestGroup;
