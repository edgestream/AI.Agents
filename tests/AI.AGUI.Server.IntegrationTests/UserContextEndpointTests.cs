using System.Net;
using System.Text.Json;

namespace AI.AGUI.Server.IntegrationTests;

[TestClass]
public sealed class UserContextEndpointTests
{
    [TestMethod]
    public async Task GetApiMe_WithoutAuth_ReturnsUnauthenticated()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.IsFalse(json.RootElement.GetProperty("authenticated").GetBoolean());
    }

    [TestMethod]
    public async Task GetApiMe_WithPrincipalHeaders_ReturnsAuthenticated()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-NAME", "test@example.com");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.IsTrue(json.RootElement.GetProperty("authenticated").GetBoolean());
        Assert.AreEqual("user-123", json.RootElement.GetProperty("userId").GetString());
        Assert.AreEqual("test@example.com", json.RootElement.GetProperty("displayName").GetString());
    }

    [TestMethod]
    public async Task GetOAuthStatus_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/oauth/status/github");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetOAuthStatus_WithAuth_ReturnsNotAuthorized()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/oauth/status/github");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.AreEqual("github", json.RootElement.GetProperty("mcpServerName").GetString());
        Assert.IsFalse(json.RootElement.GetProperty("authorized").GetBoolean());
    }

    [TestMethod]
    public async Task DeleteOAuthRevoke_WithAuth_ReturnsRevoked()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/oauth/revoke/github");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.AreEqual("github", json.RootElement.GetProperty("mcpServerName").GetString());
        Assert.IsTrue(json.RootElement.GetProperty("revoked").GetBoolean());
    }

    [TestMethod]
    public async Task GetOAuthAuthorize_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/oauth/authorize/github");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task GetOAuthAuthorize_UnknownServer_ReturnsNotFound()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/oauth/authorize/unknown-server");
        request.Headers.Add("X-MS-CLIENT-PRINCIPAL-ID", "user-123");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetOAuthCallback_WithoutParams_ReturnsBadRequest()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/oauth/callback");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task GetOAuthCallback_WithError_ReturnsBadRequestWithError()
    {
        await using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/oauth/callback?error=access_denied&error_description=User+denied+access");

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.AreEqual("access_denied", json.RootElement.GetProperty("error").GetString());
    }
}
