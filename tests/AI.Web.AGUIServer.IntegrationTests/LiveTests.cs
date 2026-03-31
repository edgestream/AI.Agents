using System.Net;
using System.Net.Http.Json;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Optional live integration tests that call a real Azure OpenAI-backed AG-UI server.
/// These tests are gated by the <c>AGUI_LIVE_TEST</c> environment variable.
/// Set <c>AGUI_LIVE_TEST=true</c> and configure valid Azure OpenAI settings to run.
/// </summary>
[TestClass]
public sealed class LiveTests
{
    private static bool IsLiveTestEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("AGUI_LIVE_TEST"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    [TestMethod]
    public async Task Live_HealthEndpoint_ReturnsOk()
    {
        if (!IsLiveTestEnabled)
        {
            Assert.Inconclusive("Live tests are disabled. Set AGUI_LIVE_TEST=true to enable.");
            return;
        }

        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Live_AGUIEndpoint_ReturnsSSEStream()
    {
        if (!IsLiveTestEnabled)
        {
            Assert.Inconclusive("Live tests are disabled. Set AGUI_LIVE_TEST=true to enable.");
            return;
        }

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
}
