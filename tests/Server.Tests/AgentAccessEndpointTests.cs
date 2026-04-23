using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AI.Agents.Abstractions;
using AI.Agents.Microsoft.Auth;

namespace AI.Agents.Server.Tests;

/// <summary>
/// Integration tests for <see cref="AgentAccessEndpointFilter"/>.
/// </summary>
[TestClass]
public sealed class AgentAccessEndpointTests
{
    [TestMethod]
    public async Task AgentEndpoint_Returns401_WhenRequireAuthenticationForAgent_IsTrue_AndUserIsAnonymous()
    {
        var factory = new AGUIServerFactory()
            .WithRequireAuthenticationForAgent(true);

        using var client = factory.CreateClient();
        var payload = new
        {
            threadId = "test-thread",
            runId = "test-run",
            messages = Array.Empty<object>(),
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cts.Token));
        Assert.IsTrue(json.RootElement.TryGetProperty("error", out var errorProp));
        StringAssert.Contains(errorProp.GetString(), "sign in", StringComparison.OrdinalIgnoreCase);

        factory.Dispose();
    }

    [TestMethod]
    public async Task AgentEndpoint_Returns200_WhenRequireAuthenticationForAgent_IsFalse_AndUserIsAnonymous()
    {
        var factory = new AGUIServerFactory()
            .WithRequireAuthenticationForAgent(false);

        using var client = factory.CreateClient();
        var payload = new
        {
            threadId = "test-thread",
            runId = "test-run",
            messages = Array.Empty<object>(),
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        factory.Dispose();
    }

    [TestMethod]
    public async Task AgentEndpoint_Returns200_WhenRequireAuthenticationForAgent_IsTrue_AndUserIsAuthenticated()
    {
        var factory = new AGUIServerFactory()
            .WithRequireAuthenticationForAgent(true)
            .WithUserProfileService(new StubUserProfileService());

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(EntraAuthMiddleware.MS_CLIENT_PRINCIPAL_ID, "user-123");
        client.DefaultRequestHeaders.Add(EntraAuthMiddleware.MS_TOKEN_AAD_ACCESS_TOKEN, "test-token");

        var payload = new
        {
            threadId = "test-thread",
            runId = "test-run",
            messages = Array.Empty<object>(),
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        factory.Dispose();
    }

    /// <summary>
    /// Stubs <see cref="IUserProfileService"/> to return a fixed authenticated context.
    /// </summary>
    private sealed class StubUserProfileService : IUserProfileService
    {
        public Task<IUserContext?> GetCurrentUserProfileAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IUserContext?>(
                new GraphUserContext(
                    userId: "user-123",
                    displayName: "Test User",
                    email: "test@example.com",
                    accessToken: accessToken));
        }
    }
}
