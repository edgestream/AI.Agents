using System.Net;
using System.Net.Http.Json;

namespace AI.AGUI.Server.IntegrationTests;

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
        var response = await _client.PostAsJsonAsync("/agents/agui-agent", payload, cts.Token);

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
        var response = await _client.PostAsJsonAsync("/agents/agui-agent", payload, cts.Token);
        var body = await response.Content.ReadAsStringAsync(cts.Token);

        Assert.IsFalse(string.IsNullOrWhiteSpace(body), "SSE stream body should not be empty.");
    }
}
