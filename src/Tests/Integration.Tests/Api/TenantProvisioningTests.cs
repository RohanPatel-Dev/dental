using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dental.Framework.Shared.Tenancy;
using Dental.Integration.Tests.Fixtures;
using Shouldly;

namespace Dental.Integration.Tests.Api;

/// <summary>
/// Provisioning a practice end to end, and the isolation that follows from it.
/// </summary>
/// <remarks>
/// This is the one test that exercises the whole two-tier eventing path: the command writes an
/// outbox row, the dispatcher picks it up, Identity's handler runs in a restored tenant context and
/// seeds the practice's roles and administrator. Nothing here is stubbed, so it waits for the
/// dispatcher rather than asserting immediately.
/// </remarks>
/// <param name="fixture">The shared API and database.</param>
[Collection(DentalApiTestGroup.Name)]
public sealed class TenantProvisioningTests(DentalApiFixture fixture)
{
    private static readonly TimeSpan ProvisioningTimeout = TimeSpan.FromSeconds(60);

    #region Happy Path

    [Fact]
    public async Task CreateTenant_Should_SeedTheAdministratorThroughTheOutbox()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient root = await fixture.CreateAuthenticatedClientAsync();
        string identifier = $"clinic-{Guid.CreateVersion7():N}"[..20];

        JsonElement tenant = await CreateTenantAsync(root, identifier);
        tenant.GetProperty("identifier").GetString().ShouldBe(identifier);

        // The administrator does not exist yet - the handler has not run. Poll rather than sleep.
        HttpStatusCode signIn = await WaitForSignInAsync(identifier);

        signIn.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Patients_Should_BeInvisibleToAnotherPractice()
    {
        // The query filter, proved rather than assumed: same table, same connection, different
        // tenant header.
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient root = await fixture.CreateAuthenticatedClientAsync();
        string identifier = $"clinic-{Guid.CreateVersion7():N}"[..20];

        await CreateTenantAsync(root, identifier);
        (await WaitForSignInAsync(identifier)).ShouldBe(HttpStatusCode.OK);

        using HttpResponseMessage registered = await root.PostAsJsonAsync(
            new Uri("/api/v1/patients", UriKind.Relative),
            new
            {
                firstName = "Root",
                lastName = "OnlyPatient",
                dateOfBirth = "1979-02-02",
                sex = "Other",
                email = $"{Guid.CreateVersion7():N}@example.test",
                phoneNumber = (string?)null,
                preferredProviderId = (Guid?)null,
                allergies = Array.Empty<string>(),
                hasMarketingConsent = false,
                hasReminderConsent = false,
            },
            TestContext.Current.CancellationToken);
        registered.EnsureSuccessStatusCode();

        Guid patientId = (await registered.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();

        using HttpClient neighbour = await SignInAsync(identifier);

        using HttpResponseMessage crossTenant = await neighbour.GetAsync(
            new Uri($"/api/v1/patients/{patientId}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        crossTenant.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnknownTenant_Should_BeRejected_RatherThanFallingBackToRoot()
    {
        // Finbuckle's strategy chain ends in a static "root" strategy, so an unrecognised header
        // would otherwise be served root's data. Naming a practice that does not exist is an error,
        // not a request for the operator tenant.
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateTenantClient("no-such-practice");

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tokens", UriKind.Relative),
            new { email = DentalApiFixture.AdminEmail, password = DentalApiFixture.AdminPassword },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingTenantHeader_Should_StillReachTheOperatorTenant()
    {
        // The other half of the same rule: no header at all is how the operator app and the probes
        // talk to the API, and that has to keep working.
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region Exception Cases

    [Fact]
    public async Task CreateTenant_Should_Be409_ForADuplicateIdentifier()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient root = await fixture.CreateAuthenticatedClientAsync();

        using HttpResponseMessage response = await root.PostAsJsonAsync(
            new Uri("/api/v1/tenants", UriKind.Relative),
            NewTenant(TenantConstants.RootTenant),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    #endregion

    private static object NewTenant(string identifier) => new
    {
        identifier,
        name = $"Practice {identifier}",
        adminEmail = $"admin@{identifier}.test",
        plan = "solo",
        timeZone = "Europe/London",
        validUntil = (DateTimeOffset?)null,
    };

    private static async Task<JsonElement> CreateTenantAsync(HttpClient root, string identifier)
    {
        using HttpResponseMessage response = await root
            .PostAsJsonAsync(
                new Uri("/api/v1/tenants", UriKind.Relative),
                NewTenant(identifier),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<HttpStatusCode> WaitForSignInAsync(string identifier)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(ProvisioningTimeout);
        HttpStatusCode last = HttpStatusCode.Unauthorized;

        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpClient client = fixture.CreateTenantClient(identifier);
            using HttpResponseMessage response = await client
                .PostAsJsonAsync(
                    new Uri("/api/v1/tokens", UriKind.Relative),
                    new { email = $"admin@{identifier}.test", password = DentalApiFixture.AdminPassword },
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(false);

            last = response.StatusCode;

            if (last == HttpStatusCode.OK)
            {
                return last;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken)
                .ConfigureAwait(false);
        }

        return last;
    }

    private async Task<HttpClient> SignInAsync(string identifier)
    {
        HttpClient client = fixture.CreateTenantClient(identifier);

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                new Uri("/api/v1/tokens", UriKind.Relative),
                new { email = $"admin@{identifier}.test", password = DentalApiFixture.AdminPassword },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        JsonElement token = await response.Content
            .ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                token.GetProperty("accessToken").GetString());

        return client;
    }
}
