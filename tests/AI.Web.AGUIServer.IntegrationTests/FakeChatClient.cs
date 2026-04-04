using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// A minimal hand-written fake for <see cref="IChatClient"/> that returns
/// deterministic responses without requiring Azure credentials.
/// It can simulate both model-driven tool calls and annotated assistant output.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    private const string CitationToolName = "DisplaySources";

    private const string AnnotatedWeatherResponse = "Today in Guangzhou it is warm and humid with scattered clouds.";

    private static readonly IReadOnlyList<Dictionary<string, object?>> s_sampleSources =
    [
        new Dictionary<string, object?>
        {
            ["title"] = "AG-UI Messages",
            ["url"] = "https://docs.ag-ui.com/concepts/messages",
            ["snippet"] = "AG-UI assistant messages include tool calls, while tool results are emitted as tool messages."
        },
        new Dictionary<string, object?>
        {
            ["title"] = "CopilotKit useRenderTool",
            ["url"] = "https://docs.copilotkit.ai/reference/v2/hooks/useRenderTool",
            ["snippet"] = "CopilotKit can register a named tool renderer and render structured tool results inside the chat."
        }
    ];

    private static readonly IReadOnlyList<FakeCitationAnnotation> s_annotationPayload =
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

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldEmitAnnotatedWeatherAnswer(chatMessages))
        {
            var message = new ChatMessage(ChatRole.Assistant, [CreateAnnotatedWeatherContent()]);
            return Task.FromResult(new ChatResponse(message));
        }

        if (ShouldEmitCitationTool(chatMessages, options, out var toolName))
        {
            var callContent = CreateCitationToolCall(toolName);
            var message = new ChatMessage(ChatRole.Assistant, [callContent]);
            return Task.FromResult(new ChatResponse(message));
        }

        if (HasCitationToolResult(chatMessages))
        {
            var sourcedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Here is a sourced answer from FakeChatClient."));
            return Task.FromResult(sourcedResponse);
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
            yield return new ChatResponseUpdate(ChatRole.Assistant, [CreateAnnotatedWeatherContent()])
            {
                MessageId = "assistant-weather-1",
                ResponseId = "response-weather-1"
            };
            yield break;
        }

        if (ShouldEmitCitationTool(chatMessages, options, out var toolName))
        {
            var callContent = CreateCitationToolCall(toolName);
            yield return new ChatResponseUpdate(ChatRole.Assistant, [callContent]);
            yield break;
        }

        if (HasCitationToolResult(chatMessages))
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Here is ");
            yield return new ChatResponseUpdate(ChatRole.Assistant, "a sourced answer from FakeChatClient.");
            yield break;
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello ");
        yield return new ChatResponseUpdate(ChatRole.Assistant, "from FakeChatClient");
    }

    private static bool HasCitationToolResult(IEnumerable<ChatMessage> chatMessages)
    {
        return chatMessages.Any(static message =>
            message.Role == ChatRole.Tool &&
            message.Contents.OfType<FunctionResultContent>().Any());
    }

    private static bool ShouldEmitAnnotatedWeatherAnswer(IEnumerable<ChatMessage> chatMessages)
    {
        var latestUserMessage = chatMessages.LastOrDefault(static message => message.Role == ChatRole.User)?.Text;
        if (string.IsNullOrWhiteSpace(latestUserMessage))
        {
            return false;
        }

        return latestUserMessage.Contains("weather", StringComparison.OrdinalIgnoreCase) &&
               latestUserMessage.Contains("guangzhou", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldEmitCitationTool(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options,
        out string toolName)
    {
        toolName = options?.Tools?.FirstOrDefault(static tool =>
                string.Equals(tool.Name, CitationToolName, StringComparison.OrdinalIgnoreCase))?.Name
            ?? CitationToolName;

        if (HasCitationToolResult(chatMessages))
            return false;

        var latestUserMessage = chatMessages.LastOrDefault(static message => message.Role == ChatRole.User)?.Text;
        if (string.IsNullOrWhiteSpace(latestUserMessage))
            return false;

        return latestUserMessage.Contains("source", StringComparison.OrdinalIgnoreCase) ||
               latestUserMessage.Contains("citation", StringComparison.OrdinalIgnoreCase) ||
               latestUserMessage.Contains("reference", StringComparison.OrdinalIgnoreCase);
    }

    private static FunctionCallContent CreateCitationToolCall(string toolName)
    {
        return new FunctionCallContent(
            "call_sources_1",
            toolName,
            new Dictionary<string, object?>
            {
                ["sources"] = s_sampleSources
            });
    }

    private static TextContent CreateAnnotatedWeatherContent()
    {
        var content = new TextContent(AnnotatedWeatherResponse)
        {
            Annotations = s_annotationPayload.Select(CreateAnnotation).Cast<AIAnnotation>().ToList()
        };

        return content;
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
