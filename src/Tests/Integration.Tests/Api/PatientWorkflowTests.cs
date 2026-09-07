using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dental.Framework.Shared.Http;
using Dental.Integration.Tests.Fixtures;
using Shouldly;

namespace Dental.Integration.Tests.Api;

/// <summary>
/// Registering and finding a patient through the real stack: validation, the tenant stamped by the
/// context, paging, and replay protection on the create.
/// </summary>
/// <param name="fixture">The shared API and database.</param>
[Collection(DentalApiTestGroup.Name)]
public sealed class PatientWorkflowTests(DentalApiFixture fixture)
{
    #region Happy Path

    [Fact]
    public async Task Register_Should_ReturnThePatient_WithAGeneratedChartNumber()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = await fixture.CreateAuthenticatedClientAsync();

        JsonElement patient = await RegisterAsync(client, "Ada", "Lovelace");

        patient.GetProperty("firstName").GetString().ShouldBe("Ada");
        patient.GetProperty("chartNumber").GetString().ShouldStartWith("P-");
        patient.GetProperty("id").GetGuid().ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Get_Should_ReturnAPatientRegisteredEarlier()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = await fixture.CreateAuthenticatedClientAsync();
        JsonElement registered = await RegisterAsync(client, "Grace", "Hopper");

        using HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/v1/patients/{registered.GetProperty("id").GetGuid()}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement fetched = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        fetched.GetProperty("lastName").GetString().ShouldBe("Hopper");
    }

    [Fact]
    public async Task Search_Should_PageAndFilter()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = await fixture.CreateAuthenticatedClientAsync();
        await RegisterAsync(client, "Katherine", "Johnson");

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/api/v1/patients?searchTerm=Johnson&pageNumber=1&pageSize=10", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        JsonElement page = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        page.GetProperty("pageNumber").GetInt32().ShouldBe(1);
        page.GetProperty("items").EnumerateArray()
            .ShouldContain(item => item.GetProperty("lastName").GetString() == "Johnson");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Register_Should_ReplayTheFirstResponse_ForARepeatedIdempotencyKey()
    {
        // A patient double-tapping "Register" on a flaky connection must end up on the books once.
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = await fixture.CreateAuthenticatedClientAsync();
        string key = Guid.CreateVersion7().ToString();

        JsonElement first = await RegisterAsync(client, "Rosalind", "Franklin", key);
        JsonElement second = await RegisterAsync(client, "Rosalind", "Franklin", key);

        second.GetProperty("id").GetGuid().ShouldBe(first.GetProperty("id").GetGuid());
        second.GetProperty("chartNumber").GetString().ShouldBe(first.GetProperty("chartNumber").GetString());
    }

    #endregion

    #region Exception Cases

    [Fact]
    public async Task Register_Should_Be400_WithPerFieldErrors_ForAnEmptyName()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = await fixture.CreateAuthenticatedClientAsync();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/patients", UriKind.Relative),
            NewPatient(string.Empty, "Nobody"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        problem.GetProperty("errors").GetProperty("FirstName").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Get_Should_Be404_ForAnUnknownPatient()
    {
        Assert.SkipUnless(TestDatabase.IsAvailable, TestDatabase.SkipReason);
        using HttpClient client = await fixture.CreateAuthenticatedClientAsync();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri($"/api/v1/patients/{Guid.CreateVersion7()}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    private static async Task<JsonElement> RegisterAsync(
        HttpClient client,
        string firstName,
        string lastName,
        string? idempotencyKey = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/patients")
        {
            Content = JsonContent.Create(NewPatient(firstName, lastName)),
        };

        if (idempotencyKey is not null)
        {
            request.Headers.Add(HeaderNames.IdempotencyKey, idempotencyKey);
        }

        using HttpResponseMessage response = await client
            .SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }

    private static object NewPatient(string firstName, string lastName) => new
    {
        firstName,
        lastName,
        dateOfBirth = "1985-04-12",
        sex = "Female",
        email = $"{Guid.CreateVersion7():N}@example.test",
        phoneNumber = "+15551234567",
        preferredProviderId = (Guid?)null,
        allergies = new[] { "Penicillin" },
        hasMarketingConsent = false,
        hasReminderConsent = true,
    };
}
