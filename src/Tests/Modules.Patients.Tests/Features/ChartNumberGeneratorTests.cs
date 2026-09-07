using Dental.Framework.Web.Tenancy;
using Dental.Modules.Patients.Data;
using Dental.Modules.Patients.Domain;
using Dental.Modules.Patients.Services;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace Dental.Modules.Patients.Tests.Features;

/// <summary>
/// Chart numbering, over a real (in-memory) context so the tenant scoping is exercised rather
/// than assumed. Each tenant gets its own context over one shared database, which is how the
/// real system behaves: the store is shared, the context is not.
/// </summary>
public sealed class ChartNumberGeneratorTests : IDisposable
{
    private readonly string _database = $"patients-{Guid.CreateVersion7()}";
    private readonly List<PatientsDbContext> _contexts = [];

    #region Happy Path

    [Fact]
    public async Task NextAsync_Should_StartAtOne_When_ThePracticeHasNoPatients()
    {
        ChartNumberGenerator generator = new(ContextFor("root"));

        string number = await generator.NextAsync(TestContext.Current.CancellationToken);

        number.ShouldBe("P-000001");
    }

    [Fact]
    public async Task NextAsync_Should_ContinueTheSequence_When_PatientsExist()
    {
        PatientsDbContext context = ContextFor("root");
        context.Patients.Add(NewPatient("P-000001", "root"));
        context.Patients.Add(NewPatient("P-000002", "root"));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ChartNumberGenerator generator = new(context);

        string number = await generator.NextAsync(TestContext.Current.CancellationToken);

        number.ShouldBe("P-000003");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task NextAsync_Should_CountOnlyTheCurrentTenantsPatients()
    {
        // Numbering is per practice. Counting across tenants would leak how many patients a
        // neighbouring practice has, and would skip numbers for no reason.
        PatientsDbContext other = ContextFor("other-practice");
        other.Patients.Add(NewPatient("P-000001", "other-practice"));
        other.Patients.Add(NewPatient("P-000002", "other-practice"));
        await other.SaveChangesAsync(TestContext.Current.CancellationToken);

        PatientsDbContext root = ContextFor("root");
        root.Patients.Add(NewPatient("P-000001", "root"));
        await root.SaveChangesAsync(TestContext.Current.CancellationToken);

        ChartNumberGenerator generator = new(root);

        string number = await generator.NextAsync(TestContext.Current.CancellationToken);

        number.ShouldBe("P-000002");
    }

    #endregion

    #region Exception Cases

    [Fact]
    public async Task SaveChanges_Should_Throw_When_ARowIsStampedWithAnotherTenant()
    {
        // The guardrail behind the numbering rule: a tenant-scoped context refuses to write rows
        // belonging to somebody else, whatever the calling code sets TenantId to.
        PatientsDbContext root = ContextFor("root");
        root.Patients.Add(NewPatient("P-000001", "other-practice"));

        await Should.ThrowAsync<MultiTenantException>(
            () => root.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (PatientsDbContext context in _contexts)
        {
            context.Dispose();
        }
    }

    private PatientsDbContext ContextFor(string tenantId)
    {
        IMultiTenantContextAccessor accessor = Substitute.For<IMultiTenantContextAccessor>();
        accessor.MultiTenantContext.Returns(
            new MultiTenantContext<DentalTenantInfo>(
                new DentalTenantInfo { Id = tenantId, Identifier = tenantId }));

        DbContextOptions<PatientsDbContext> options =
            new DbContextOptionsBuilder<PatientsDbContext>()
                .UseInMemoryDatabase(_database)
                .Options;

        PatientsDbContext context = new(accessor, options);
        _contexts.Add(context);
        return context;
    }

    private static Patient NewPatient(string chartNumber, string tenantId) => new()
    {
        ChartNumber = chartNumber,
        FirstName = "Test",
        LastName = "Patient",
        DateOfBirth = new DateOnly(1990, 1, 1),
        TenantId = tenantId,
    };
}
