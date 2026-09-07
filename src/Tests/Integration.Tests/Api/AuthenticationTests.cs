using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dental.Framework.Shared.Tenancy;
using Dental.Integration.Tests.Fixtures;
using Shouldly;

namespace Dental.Integration.Tests.Api;

/// <summary>
/// Sign-in and the shape of an authenticated identity. Permissions are resolved per request rather
/// than carried in the token, so "the token works" and "the caller is authorized" are separate
/// facts and both are checked here.
/// </summary>
/// <param name="fixture">The shared API and database.</param>
[Collection(DentalApiTestGroup.Name)]
public sealed class AuthenticationTests(DentalApiFixture fixture)
{
    #region Happy Path

    [Fact]
    public async Task Tokens_Should_IssueAPair_ForTheSeededAdministrator()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateTenantClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tokens", UriKind.Relative),
            new { email = DentalApiFixture.AdminEmail, password = DentalApiFixture.AdminPassword },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonElement token = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        token.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        token.GetProperty("refreshToken").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Me_Should_DescribeTheSignedInAdministrator()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = await fixture.CreateAuthenticatedClientAsync();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/api/v1/account/me", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonElement me = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        me.GetProperty("user").GetProperty("email").GetString().ShouldBe(DentalApiFixture.AdminEmail);
        me.GetProperty("tenantId").GetString().ShouldBe(TenantConstants.RootTenant);

        // Permissions come from the role's claims at request time, not from the token, so an
        // administrator whose role changed does not have to sign in again.
        me.GetProperty("permissions").GetArrayLength().ShouldBeGreaterThan(0);
    }

    #endregion

    #region Exception Cases

    [Fact]
    public async Task ProtectedEndpoint_Should_Be401_WithoutAToken()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateTenantClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/api/v1/patients", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tokens_Should_Be401_ForAWrongPassword()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateTenantClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tokens", UriKind.Relative),
            new { email = DentalApiFixture.AdminEmail, password = "not-the-password" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tokens_Should_Be400_ForAMissingPassword()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = fixture.CreateTenantClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tokens", UriKind.Relative),
            new { email = DentalApiFixture.AdminEmail, password = string.Empty },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        problem.GetProperty("errors").GetProperty("Password").GetArrayLength().ShouldBeGreaterThan(0);
        problem.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    #endregion
}
