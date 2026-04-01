using System.Runtime.CompilerServices;

namespace AI.Web.E2ETests;

/// <summary>
/// A minimal fake <see cref="IChatClient"/> that returns deterministic responses
/// without requiring Azure credentials. Used by <see cref="StubBackendFixture"/> to
/// replace the real Azure OpenAI client during E2E test runs.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello from FakeChatClient"));
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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
