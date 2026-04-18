using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AI.Agents.Microsoft.Client;

/// <summary>
/// A delegating chat client that tracks token usage and makes it available via AsyncLocal context.
/// </summary>
public sealed class TokenUsageTrackingChatClient : DelegatingChatClient
{
    private static readonly AsyncLocal<UsageDetails?> _currentUsage = new();

    /// <summary>
    /// Gets the current token usage for the active request, if available.
    /// </summary>
    public static UsageDetails? CurrentUsage => _currentUsage.Value;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenUsageTrackingChatClient"/>.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    public TokenUsageTrackingChatClient(IChatClient innerClient) : base(innerClient)
    {
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
        
        // Store usage in AsyncLocal for the current context
        if (response.Usage != null)
        {
            _currentUsage.Value = response.Usage;
        }
        
        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        UsageDetails? accumulatedUsage = null;
        
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            // Accumulate usage from streaming updates
            if (update.Usage != null)
            {
                accumulatedUsage = update.Usage;
            }
            
            yield return update;
        }
        
        // Store final usage in AsyncLocal for the current context
        if (accumulatedUsage != null)
        {
            _currentUsage.Value = accumulatedUsage;
        }
    }
}
