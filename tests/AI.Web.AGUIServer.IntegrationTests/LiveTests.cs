using System.Net;
using System.Net.Http.Json;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Optional live integration tests that call a real Azure OpenAI-backed AG-UI server.
/// </summary>
[TestClass]
[TestCategory("Live")]
public sealed class LiveTests
{
    [TestMethod]
    public async Task Live_HealthEndpoint_ReturnsOk()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Live_AGUIEndpoint_ReturnsSSEStream()
    {
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
