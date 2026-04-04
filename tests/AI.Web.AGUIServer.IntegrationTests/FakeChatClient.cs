using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// A minimal hand-written fake for <see cref="IChatClient"/> that returns
/// deterministic responses without requiring Azure credentials.
/// It can simulate annotated assistant output with citation metadata.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    private const string AnnotatedWeatherResponse = "Today in Guangzhou it is warm and humid with scattered clouds.";

    private static readonly IReadOnlyList<FakeCitationAnnotation> s_weatherAnnotations =
    [
        new(
            "Weather.com Guangzhou Forecast",
            "https://weather.com/weather/today/l/Guangzhou+China",
            "Warm and humid conditions are forecast for Guangzhou today.",
            9,
            18),
        new(
            "Time and Date Guangzhou Weather",
            "https://www.timeanddate.com/weather/china/guangzhou",
            "Recent reports show scattered clouds and muggy conditions in Guangzhou.",
            29,
            55)
    ];

    private static readonly IReadOnlyList<FakeCitationAnnotation> s_sourceAnnotations =
    [
        new(
            "AG-UI Messages",
            "https://docs.ag-ui.com/concepts/messages",
            "AG-UI assistant messages include tool calls, while tool results are emitted as tool messages.",
            0,
            20),
        new(
            "CopilotKit useRenderTool",
            "https://docs.copilotkit.ai/reference/v2/hooks/useRenderTool",
            "CopilotKit can register a named tool renderer and render structured tool results inside the chat.",
            21,
            50)
    ];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldEmitAnnotatedWeatherAnswer(chatMessages))
        {
            var message = new ChatMessage(ChatRole.Assistant, [CreateAnnotatedContent(AnnotatedWeatherResponse, s_weatherAnnotations)]);
            return Task.FromResult(new ChatResponse(message));
        }

        if (ShouldEmitAnnotatedSourceAnswer(chatMessages))
        {
            const string text = "Here are the key AG-UI and CopilotKit concepts you should know about.";
            var message = new ChatMessage(ChatRole.Assistant, [CreateAnnotatedContent(text, s_sourceAnnotations)]);
            return Task.FromResult(new ChatResponse(message));
        }

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello from FakeChatClient"));
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (ShouldEmitAnnotatedWeatherAnswer(chatMessages))
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, [CreateAnnotatedContent(AnnotatedWeatherResponse, s_weatherAnnotations)])
            {
                MessageId = "assistant-weather-1",
                ResponseId = "response-weather-1"
            };
            yield break;
        }

        if (ShouldEmitAnnotatedSourceAnswer(chatMessages))
        {
            const string text = "Here are the key AG-UI and CopilotKit concepts you should know about.";
            yield return new ChatResponseUpdate(ChatRole.Assistant, [CreateAnnotatedContent(text, s_sourceAnnotations)])
            {
                MessageId = "assistant-sources-1",
                ResponseId = "response-sources-1"
            };
            yield break;
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello ");
        yield return new ChatResponseUpdate(ChatRole.Assistant, "from FakeChatClient");
    }

    private static bool ShouldEmitAnnotatedWeatherAnswer(IEnumerable<ChatMessage> chatMessages)
    {
        var text = chatMessages.LastOrDefault(static m => m.Role == ChatRole.User)?.Text;
        return text is not null
            && text.Contains("weather", StringComparison.OrdinalIgnoreCase)
            && text.Contains("guangzhou", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldEmitAnnotatedSourceAnswer(IEnumerable<ChatMessage> chatMessages)
    {
        var text = chatMessages.LastOrDefault(static m => m.Role == ChatRole.User)?.Text;
        return text is not null
            && (text.Contains("source", StringComparison.OrdinalIgnoreCase)
                || text.Contains("citation", StringComparison.OrdinalIgnoreCase)
                || text.Contains("reference", StringComparison.OrdinalIgnoreCase));
    }

    private static TextContent CreateAnnotatedContent(string text, IReadOnlyList<FakeCitationAnnotation> annotations)
    {
        return new TextContent(text)
        {
            Annotations = annotations.Select(CreateAnnotation).Cast<AIAnnotation>().ToList()
        };
    }

    private static AIAnnotation CreateAnnotation(FakeCitationAnnotation citation)
    {
        return new AIAnnotation
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["title"] = citation.Title,
                ["url"] = citation.Url,
                ["snippet"] = citation.Snippet,
            },
            AnnotatedRegions =
            [
                new TextSpanAnnotatedRegion
                {
                    StartIndex = citation.StartIndex,
                    EndIndex = citation.EndIndex,
                }
            ],
            RawRepresentation = new
            {
                citation.Title,
                Uri = new Uri(citation.Url),
                citation.Snippet,
                TextToReplace = citation.Snippet,
            }
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IChatClient))
            return this;

        return null;
    }

    public void Dispose() { }

    private sealed record FakeCitationAnnotation(
        string Title,
        string Url,
        string Snippet,
        int StartIndex,
        int EndIndex);
}
