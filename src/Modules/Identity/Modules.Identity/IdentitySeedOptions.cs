using System.ComponentModel.DataAnnotations;

namespace Dental.Modules.Identity;

/// <summary>
/// Seeding configuration, bound from the <c>IdentitySeedOptions</c> section.
/// </summary>
/// <remarks>
/// The administrator password is configuration, never a constant in source: a literal here would be
/// identical in every deployment that cloned this repository. Supply it through user secrets in
/// development and through the environment in every other case.
/// </remarks>
public sealed class IdentitySeedOptions
{
    /// <summary>Password given to a newly seeded practice administrator.</summary>
    [Required]
    [MinLength(12)]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>Address of the operator tenant's administrator.</summary>
    [Required]
    [EmailAddress]
    public string OperatorEmail { get; set; } = "operator@dental.local";
}
