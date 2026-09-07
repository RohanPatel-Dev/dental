using System.Net;
using System.Text.Json;
using Dental.Integration.Tests.Fixtures;
using Shouldly;

namespace Dental.Integration.Tests.Api;

/// <summary>
/// The probes an orchestrator makes decisions on. <c>/alive</c> must not depend on the database, or
/// a database blip restarts every replica instead of just taking them out of the load balancer.
/// </summary>
/// <param name="fixture">The shared API and database.</param>
[Collection(DentalApiTestGroup.Name)]
public sealed class HealthEndpointTests(DentalApiFixture fixture)
{
    #region Happy Path

    [Fact]
    public async Task Health_Should_ReportEveryModulesDatabaseCheck()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateTenantClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonElement report = JsonDocument
            .Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement;

        report.GetProperty("status").GetString().ShouldBe("Healthy");

        string[] checks = [.. report.GetProperty("entries").EnumerateObject().Select(e => e.Name)];

        checks.ShouldContain("db:tenancy");
        checks.ShouldContain("db:identity");
        checks.ShouldContain("db:patients");
        checks.ShouldContain("db:scheduling");
        checks.ShouldContain("db:clinical");
        checks.ShouldContain("db:billing");
        checks.ShouldContain("db:auditing");
    }

    [Fact]
    public async Task Alive_Should_AnswerWithoutTouchingTheDatabase()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateTenantClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/alive", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Probes_Should_BeAnonymous()
    {
        // A probe that needs a token cannot run before the identity module is up, which is exactly
        // when the answer matters most.
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage ready = await client.GetAsync(
            new Uri("/ready", UriKind.Relative), TestContext.Current.CancellationToken);

        ready.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion
}
