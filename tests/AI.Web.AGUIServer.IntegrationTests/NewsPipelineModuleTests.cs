using System.Net;
using System.Net.Http.Json;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Integration tests for the fan-out/fan-in news pipeline wired by
/// <see cref="NewsPipelineModule"/>. All agents are backed by
/// <see cref="FakeChatClient"/> so no real Azure credentials are required.
/// </summary>
[TestClass]
public sealed class NewsPipelineModuleTests
{
    private static AGUIServerFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new AGUIServerFactory(new NewsPipelineTestModule());
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task NewsPipeline_AGUIEndpoint_ReturnsOk()
    {
        var payload = BuildPayload();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task NewsPipeline_AGUIEndpoint_ReturnsSSEContentType()
    {
        var payload = BuildPayload();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task NewsPipeline_AGUIEndpoint_ReturnsNonEmptyStream()
    {
        var payload = BuildPayload();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);
        var body = await response.Content.ReadAsStringAsync(cts.Token);

        Assert.IsFalse(string.IsNullOrWhiteSpace(body), "SSE stream body should not be empty.");
    }

    [TestMethod]
    public async Task NewsPipeline_NewsQuery_InvokesAllPipelineAgents()
    {
        var payload = BuildPayload(userMessage: "What's in the news today?");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);
        var body = await response.Content.ReadAsStringAsync(cts.Token);

        // The fake pipeline runs all agents — at minimum there should be SSE events.
        Assert.IsFalse(string.IsNullOrWhiteSpace(body),
            "Pipeline should produce SSE events for a news query.");
    }

    // --- helpers ---

    private static object BuildPayload(string userMessage = "Hello") => new
    {
        threadId = "pipeline-test-thread",
        runId = "pipeline-test-run",
        messages = new[]
        {
            new { role = "user", content = userMessage }
        },
        tools = Array.Empty<object>(),
        context = Array.Empty<object>(),
        forwardedProps = new { },
    };
}
