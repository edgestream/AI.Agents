using System.Net;
using System.Net.Http.Json;

namespace AI.Web.AGUIServer.IntegrationTests;

[TestClass]
public sealed class AGUIEndpointTests
{
    private static AGUIServerFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new AGUIServerFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task AGUIEndpoint_ReturnsSSEContentType()
    {
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
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task AGUIEndpoint_ReturnsNonEmptyStream()
    {
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
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);
        var body = await response.Content.ReadAsStringAsync(cts.Token);

        Assert.IsFalse(string.IsNullOrWhiteSpace(body), "SSE stream body should not be empty.");
    }

    /// <summary>
    /// Verifies that <see cref="WebSearchToolCallContent"/> and
    /// <see cref="WebSearchToolResultContent"/> returned by the LLM are converted
    /// to readable Markdown text by <see cref="HostedContentRenderer"/> and appear
    /// in the SSE stream sent to the CopilotKit frontend.
    /// </summary>
    [TestMethod]
    public async Task AGUIEndpoint_WebSearchContent_IsRenderedAsText()
    {
        _factory.FakeChatClient.SimulateWebSearch = true;

        var payload = new
        {
            threadId = "test-thread-ws",
            runId = "test-run-ws",
            messages = Array.Empty<object>(),
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);
        var body = await response.Content.ReadAsStringAsync(cts.Token);

        // The HostedContentRenderer should have converted the WebSearchToolCallContent
        // into a text chunk mentioning the query, and the WebSearchToolResultContent
        // into a text chunk containing the result URL.
        StringAssert.Contains(body, "dotnet extensions ai",
            "The web-search query should appear in the SSE stream.");
        StringAssert.Contains(body, "learn.microsoft.com",
            "The web-search result URL should appear in the SSE stream.");
    }
}
