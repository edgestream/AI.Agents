using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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

    [TestMethod]
    public async Task AGUIEndpoint_WhenSourcesAreRequested_EmitsFootnotesInTextStream()
    {
        var payload = new
        {
            threadId = "test-thread",
            runId = "test-run",
            messages = new[]
            {
                new
                {
                    id = "user-1",
                    role = "user",
                    content = "Please answer with sources."
                }
            },
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);
        var body = await response.Content.ReadAsStringAsync(cts.Token);
        var events = ParseSseEvents(body);

        // All text content events concatenated should include the footnotes section.
        string fullText = string.Concat(events
            .Where(e => e.GetProperty("type").GetString() == "TEXT_MESSAGE_CONTENT")
            .Select(e => e.GetProperty("delta").GetString()));

        StringAssert.Contains(fullText, "**Sources:**");
        StringAssert.Contains(fullText, "https://docs.ag-ui.com/concepts/messages");
        StringAssert.Contains(fullText, "https://docs.copilotkit.ai/reference/v2/hooks/useRenderTool");

        // No tool events should be emitted for citations.
        Assert.IsFalse(events.Any(e => e.GetProperty("type").GetString() == "TOOL_CALL_START"),
            "Citations should appear as markdown footnotes, not tool events.");
    }

    [TestMethod]
    public async Task AGUIEndpoint_WhenAssistantReturnsAnnotations_AppendsMarkdownFootnotes()
    {
        var payload = new
        {
            threadId = "test-thread",
            runId = "test-run",
            messages = new[]
            {
                new
                {
                    id = "user-1",
                    role = "user",
                    content = "What is today's date and weather in Guangzhou?"
                }
            },
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await _client.PostAsJsonAsync("/", payload, cts.Token);
        var body = await response.Content.ReadAsStringAsync(cts.Token);
        var events = ParseSseEvents(body);

        string fullText = string.Concat(events
            .Where(e => e.GetProperty("type").GetString() == "TEXT_MESSAGE_CONTENT")
            .Select(e => e.GetProperty("delta").GetString()));

        // The original assistant text should be present.
        StringAssert.Contains(fullText, "Today in Guangzhou it is warm and humid");

        // The footnotes section should follow with the annotated sources.
        StringAssert.Contains(fullText, "**Sources:**");
        StringAssert.Contains(fullText, "Weather.com Guangzhou Forecast");
        StringAssert.Contains(fullText, "https://www.timeanddate.com/weather/china/guangzhou");
    }

    private static List<JsonElement> ParseSseEvents(string responseBody)
    {
        var events = new List<JsonElement>();
        using var reader = new StringReader(responseBody);
        var currentData = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                AddEventIfComplete(events, currentData);
                currentData.Clear();
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
                currentData.Add(line[5..].Trim());
        }

        AddEventIfComplete(events, currentData);
        return events;
    }

    private static void AddEventIfComplete(List<JsonElement> events, List<string> currentData)
    {
        if (currentData.Count == 0)
            return;

        using var document = JsonDocument.Parse(string.Join(Environment.NewLine, currentData));
        events.Add(document.RootElement.Clone());
    }
}
