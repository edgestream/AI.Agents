using System.Net;
using System.Text.Json;
using AI.AGUI.Auth;

namespace AI.AGUI.Server.IntegrationTests;

[TestClass]
public sealed class GraphEnrichmentTests
{
    [TestMethod]
    public async Task GetApiMe_WithGraphEnrichment_ReturnsGraphDisplayNameAndPhoto()
    {
        var mockGraphService = new MockGraphProfileService
        {
            Profile = new GraphUserProfile(
                DisplayName: "John Doe",
                Mail: "john.doe@contoso.com",
                UserPrincipalName: "john.doe@contoso.com",
                GivenName: "John",
                Surname: "Doe",
                Id: "graph-user-id"),
            Photo = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEASABIAAD"
        };

        await using var factory = new AGUIServerFactory()
            .WithGraphService(mockGraphService);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-NAME", "test@example.com");
        request.Headers.Add("X-MS-TOKEN-AAD-ACCESS-TOKEN", "fake-access-token");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.IsTrue(json.RootElement.GetProperty("authenticated").GetBoolean());
        Assert.AreEqual("user-123", json.RootElement.GetProperty("userId").GetString());
        Assert.AreEqual("John Doe", json.RootElement.GetProperty("displayName").GetString());
        Assert.AreEqual("john.doe@contoso.com", json.RootElement.GetProperty("email").GetString());
        Assert.AreEqual("data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEASABIAAD", json.RootElement.GetProperty("picture").GetString());
    }

    [TestMethod]
    public async Task GetApiMe_WithGraphFailure_FallsBackToTokenClaims()
    {
        var mockGraphService = new MockGraphProfileService
        {
            ShouldFail = true
        };

        await using var factory = new AGUIServerFactory()
            .WithGraphService(mockGraphService);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-NAME", "fallback@example.com");
        request.Headers.Add("X-MS-TOKEN-AAD-ACCESS-TOKEN", "fake-access-token");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.IsTrue(json.RootElement.GetProperty("authenticated").GetBoolean());
        Assert.AreEqual("user-123", json.RootElement.GetProperty("userId").GetString());
        // Should fall back to principal name when Graph fails
        Assert.AreEqual("fallback@example.com", json.RootElement.GetProperty("displayName").GetString());
    }

    [TestMethod]
    public async Task GetApiMe_WithoutAccessToken_SkipsGraphEnrichment()
    {
        var mockGraphService = new MockGraphProfileService
        {
            Profile = new GraphUserProfile(
                DisplayName: "Should Not Be Used",
                Mail: "should@not.be.used",
                UserPrincipalName: "should@not.be.used",
                GivenName: "Should",
                Surname: "Not",
                Id: "should-not-be-used")
        };

        await using var factory = new AGUIServerFactory()
            .WithGraphService(mockGraphService);
        using var client = factory.CreateClient();

        // No access token header
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-NAME", "header@example.com");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.IsTrue(json.RootElement.GetProperty("authenticated").GetBoolean());
        // Should use header value, not Graph value (no token means no Graph call)
        Assert.AreEqual("header@example.com", json.RootElement.GetProperty("displayName").GetString());
        Assert.IsFalse(mockGraphService.WasCalled);
    }

    [TestMethod]
    public async Task GetApiMe_WithGraphPhoto_ReturnsDataUrl()
    {
        var mockGraphService = new MockGraphProfileService
        {
            Photo = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
        };

        await using var factory = new AGUIServerFactory()
            .WithGraphService(mockGraphService);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-NAME", "test@example.com");
        request.Headers.Add("X-MS-TOKEN-AAD-ACCESS-TOKEN", "fake-access-token");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        var picture = json.RootElement.GetProperty("picture").GetString();
        Assert.IsNotNull(picture);
        Assert.IsTrue(picture.StartsWith("data:image/"));
    }

    [TestMethod]
    public async Task GetApiMe_WithNoPhoto_ReturnsNullPicture()
    {
        var mockGraphService = new MockGraphProfileService
        {
            Profile = new GraphUserProfile(
                DisplayName: "No Photo User",
                Mail: "nophoto@example.com",
                UserPrincipalName: "nophoto@example.com",
                GivenName: "No",
                Surname: "Photo",
                Id: "no-photo-user"),
            Photo = null
        };

        await using var factory = new AGUIServerFactory()
            .WithGraphService(mockGraphService);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-NAME", "test@example.com");
        request.Headers.Add("X-MS-TOKEN-AAD-ACCESS-TOKEN", "fake-access-token");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.AreEqual("No Photo User", json.RootElement.GetProperty("displayName").GetString());
        Assert.AreEqual(JsonValueKind.Null, json.RootElement.GetProperty("picture").ValueKind);
    }
}

/// <summary>
/// Mock implementation of <see cref="IGraphProfileService"/> for testing.
/// </summary>
internal sealed class MockGraphProfileService : IGraphProfileService
{
    public GraphUserProfile? Profile { get; set; }
    public string? Photo { get; set; }
    public bool ShouldFail { get; set; }
    public bool WasCalled { get; private set; }

    public Task<GraphUserProfile?> GetCurrentUserProfileAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        if (ShouldFail)
        {
            throw new InvalidOperationException("Simulated Graph failure");
        }
        return Task.FromResult(Profile);
    }

    public Task<string?> GetCurrentUserPhotoAsDataUrlAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        if (ShouldFail)
        {
            throw new InvalidOperationException("Simulated Graph failure");
        }
        return Task.FromResult(Photo);
    }
}
