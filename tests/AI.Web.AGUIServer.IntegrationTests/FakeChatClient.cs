using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

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
