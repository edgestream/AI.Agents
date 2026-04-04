using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Optional live integration tests that boot the real application against real cloud credentials.
/// Provider selection is automatic: Foundry is used when <c>Foundry:ProjectEndpoint</c> is set
/// in the local configuration; otherwise Azure OpenAI is used.
/// Tests are skipped when neither provider is configured (e.g. in CI without credentials).
/// </summary>
[TestClass]
[TestCategory("Live")]
public sealed class LiveTests
{
    /// <summary>
    /// Returns true when at least one AI provider is configured in local settings,
    /// indicating that real cloud calls can be made.
    /// </summary>
    private static bool HasProviderConfig()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return !string.IsNullOrWhiteSpace(config["Foundry:ProjectEndpoint"])
            || !string.IsNullOrWhiteSpace(config["AzureOpenAI:Endpoint"]);
    }

    [TestMethod]
    public async Task Live_HealthEndpoint_ReturnsOk()
    {
        if (!HasProviderConfig())
            Assert.Inconclusive("No AI provider configured — skipping live test.");

        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Live_AGUIEndpoint_ReturnsSSEStream()
    {
        if (!HasProviderConfig())
            Assert.Inconclusive("No AI provider configured — skipping live test.");

        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var payload = new
        {
            threadId = "live-test-thread",
            runId = "live-test-run",
            messages = new[]
            {
                new { role = "user", content = "Say hello in one word." }
            },
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var response = await client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync(cts.Token);
        Assert.IsFalse(string.IsNullOrWhiteSpace(body), "SSE stream body should not be empty.");
    }

    [TestMethod]
    public async Task Live_McpLearnServer_ToolRoundTrip()
    {
        if (!HasProviderConfig())
            Assert.Inconclusive("No AI provider configured — skipping live test.");

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Configure only the MS Learn MCP server for this test (dict format).
                builder.UseSetting("McpServers:learn:Type", "http");
                builder.UseSetting("McpServers:learn:Url", "https://learn.microsoft.com/api/mcp");
            });
        using var client = factory.CreateClient();
        
        // Wait a moment for the server to start and register the MCP server
        await Task.Delay(5000); 

        var payload = new
        {
            threadId = "live-mcp-test",
            runId = "live-mcp-run",
            messages = new[]
            {
                new { role = "user", content = "Search Microsoft Learn for 'Azure Functions overview' and summarize the first result." }
            },
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var response = await client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync(cts.Token);
        Assert.IsFalse(string.IsNullOrWhiteSpace(body), "SSE stream body should not be empty.");
        Assert.IsTrue(body.Contains("learn__"), "SSE stream should contain a tool call for a 'learn__' prefixed tool.");
    }
}
