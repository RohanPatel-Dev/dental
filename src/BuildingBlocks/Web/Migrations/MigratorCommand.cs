namespace Dental.Framework.Web.Migrations;

/// <summary>What the operator asked the migrator to do.</summary>
public enum MigratorVerb
{
    /// <summary>Not a recognised verb.</summary>
    Unknown = 0,

    /// <summary>Apply pending migrations.</summary>
    Apply = 1,

    /// <summary>Apply migrations and run the required seeders.</summary>
    Seed = 2,

    /// <summary>Apply migrations and run the required plus demo seeders.</summary>
    SeedDemo = 3,

    /// <summary>Report what would be applied, changing nothing.</summary>
    ListPending = 4,
}

/// <summary>Parsed command line for the migrator.</summary>
/// <param name="Verb">What to do.</param>
/// <param name="Seed">Whether <c>--seed</c> was supplied.</param>
/// <param name="CatalogOnly">Whether <c>--catalog-only</c> was supplied.</param>
/// <param name="TenantId">The tenant named by <c>--tenant</c>, if any.</param>
/// <param name="ShowHelp">Whether help was requested.</param>
public sealed record MigratorCommand(
    MigratorVerb Verb,
    bool Seed,
    bool CatalogOnly,
    string? TenantId,
    bool ShowHelp)
{
    /// <summary>Usage text.</summary>
    public const string HelpText = """
        Dental database migrator

        Usage:
          dotnet run --project src/Host/Dental.DbMigrator -- <verb> [options]

        Verbs:
          apply           Apply pending migrations.
          seed            Apply migrations, then run the required seeders.
          seed-demo       Apply migrations, then run the required and demo seeders.
          list-pending    Report what would be applied, changing nothing.

        Options:
          --seed              With 'apply', also run the required seeders.
          --catalog-only      Stop after the tenant catalog; do not touch tenant schemas.
          --tenant <id>       Migrate only the named tenant.
          -h, --help          Show this text.

        The whole run is serialized by a Postgres advisory lock, so two migrators started at once
        will queue rather than race.
        """;

    /// <summary>Parses the command line.</summary>
    /// <param name="args">Raw arguments.</param>
    /// <returns>The parsed command.</returns>
    public static MigratorCommand Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            return new MigratorCommand(MigratorVerb.Unknown, false, false, null, ShowHelp: true);
        }

        MigratorVerb verb = args[0] switch
        {
            "apply" => MigratorVerb.Apply,
            "seed" => MigratorVerb.Seed,
            "seed-demo" => MigratorVerb.SeedDemo,
            "list-pending" => MigratorVerb.ListPending,
            _ => MigratorVerb.Unknown,
        };

        string? tenantId = null;
        int tenantFlag = Array.IndexOf(args, "--tenant");
        if (tenantFlag >= 0 && tenantFlag + 1 < args.Length)
        {
            tenantId = args[tenantFlag + 1];
        }

        return new MigratorCommand(
            verb,
            args.Contains("--seed"),
            args.Contains("--catalog-only"),
            tenantId,
            ShowHelp: false);
    }

    /// <summary>Projects the command onto the runner's options.</summary>
    /// <returns>The run options.</returns>
    public MigrationRunOptions ToRunOptions() =>
        new(Seed, SeedDemo: false, CatalogOnly, TenantId);
}
