using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AI.Agents.Microsoft.Client;

/// <summary>
/// A delegating chat client that tracks token usage and records it in a usage store.
/// </summary>
public sealed class TokenUsageTrackingChatClient : DelegatingChatClient
{
    private readonly TokenUsageStore _usageStore;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenUsageTrackingChatClient"/>.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="usageStore">The store to record usage in.</param>
    public TokenUsageTrackingChatClient(IChatClient innerClient, TokenUsageStore usageStore) : base(innerClient)
    {
        _usageStore = usageStore;
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
        
        // Record usage
        if (response.Usage != null)
        {
            _usageStore.Record(response.Usage);
        }
        
        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Note: ChatResponseUpdate doesn't expose Usage in the current version of Microsoft.Extensions.AI
        // We'll track usage from non-streaming calls only for now
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }
}
