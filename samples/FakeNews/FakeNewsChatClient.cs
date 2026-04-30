using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

internal sealed class FakeNewsChatClient : IChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Default)
    {
        WriteIndented = true
    };

    private static readonly FakeNewsStory[] Stories =
    [
        new(
            "Moon Opens Boutique Office For Remote Agents",
            "Lunar officials deny rumors that the new workspace has no atmosphere.",
            "Satire Desk",
            "https://fake-news.example/moon-office",
            ["remote-agents", "space", "workplace"]),
        new(
            "Developers Report Configuration File Became Self-Aware",
            "The file allegedly requested clearer names, fewer magic strings, and a quiet weekend.",
            "Engineering Affairs",
            "https://fake-news.example/config-self-aware",
            ["configuration", "developer-experience"]),
        new(
            "Local Mock Agent Wins Award For Always Being Available",
            "Judges praised the agent's deterministic responses and refusal to call external services.",
            "Platform Gazette",
            "https://fake-news.example/mock-agent-award",
            ["testing", "samples", "ag-ui"])
    ];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatResponse(CreateMessage()));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var chunk in CreateResponseText().Chunk(96))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, new string(chunk));
            await Task.Yield();
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(IChatClient) ? this : null;
    }

    public void Dispose()
    {
    }

    private static ChatMessage CreateMessage()
    {
        return new ChatMessage(ChatRole.Assistant, CreateResponseText());
    }

    private static string CreateResponseText()
    {
        return
            """
            # Fake News

            Here are structured mock stories from the remote AG-UI news agent.

            ```json
            """ +
            Environment.NewLine +
            JsonSerializer.Serialize(new { stories = Stories }, JsonOptions) +
            Environment.NewLine +
            """
            ```
            """;
    }
}
