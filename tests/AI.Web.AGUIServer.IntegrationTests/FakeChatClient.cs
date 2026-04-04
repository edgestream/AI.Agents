using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

// These hosted-content types are experimental in Microsoft.Extensions.AI.
#pragma warning disable MEAI001

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// A minimal hand-written fake for <see cref="IChatClient"/> that returns
/// deterministic responses without requiring Azure credentials.
/// When the request includes tools, simulates a function-call round-trip.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    /// <summary>
    /// When true, the next response will simulate a tool call and this flag will be reset to false.
    /// </summary>
    public bool SimulateToolCall { get; set; }

    /// <summary>
    /// When true, the next response will simulate a web search (returning
    /// <see cref="WebSearchToolCallContent"/> followed by <see cref="WebSearchToolResultContent"/>)
    /// and this flag will be reset to false.
    /// </summary>
    public bool SimulateWebSearch { get; set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (SimulateToolCall && options?.Tools is { Count: > 0 })
        {
            SimulateToolCall = false;
            var toolName = options.Tools[0].Name;
            var callContent = new FunctionCallContent("call_1", toolName, new Dictionary<string, object?> { ["query"] = "test" });
            var message = new ChatMessage(ChatRole.Assistant, [callContent]);
            return Task.FromResult(new ChatResponse(message));
        }

        if (SimulateWebSearch)
        {
            SimulateWebSearch = false;
            var searchCall = new WebSearchToolCallContent("ws_1") { Queries = ["dotnet extensions ai"] };
            var searchResult = new WebSearchToolResultContent("ws_1")
            {
                Results =
                [
                    new UriContent("https://learn.microsoft.com/dotnet/ai/", "text/html"),
                ],
            };
            var message = new ChatMessage(ChatRole.Assistant, [searchCall, searchResult]);
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
        if (SimulateToolCall && options?.Tools is { Count: > 0 })
        {
            SimulateToolCall = false;
            var toolName = options.Tools[0].Name;
            var callContent = new FunctionCallContent("call_1", toolName, new Dictionary<string, object?> { ["query"] = "test" });
            yield return new ChatResponseUpdate(ChatRole.Assistant, [callContent]);
            yield break;
        }

        if (SimulateWebSearch)
        {
            SimulateWebSearch = false;
            var searchCall = new WebSearchToolCallContent("ws_1") { Queries = ["dotnet extensions ai"] };
            yield return new ChatResponseUpdate(ChatRole.Assistant, [searchCall]);

            var searchResult = new WebSearchToolResultContent("ws_1")
            {
                Results =
                [
                    new UriContent("https://learn.microsoft.com/dotnet/ai/", "text/html"),
                ],
            };
            yield return new ChatResponseUpdate(ChatRole.Assistant, [searchResult]);
            yield break;
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello ");
        yield return new ChatResponseUpdate(ChatRole.Assistant, "from FakeChatClient");
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IChatClient))
            return this;

        return null;
    }

    public void Dispose() { }
}
