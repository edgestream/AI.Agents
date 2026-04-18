using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace AI.Agents.Microsoft.Client;

/// <summary>
/// Stores recent token usage records for display purposes.
/// </summary>
public sealed class TokenUsageStore
{
    private readonly ConcurrentQueue<TokenUsageRecord> _recentUsage = new();
    private const int MaxRecords = 100;

    /// <summary>
    /// Records a token usage event.
    /// </summary>
    public void Record(UsageDetails usage)
    {
        if (usage == null) return;

        _recentUsage.Enqueue(new TokenUsageRecord
        {
            Timestamp = DateTimeOffset.UtcNow,
            InputTokens = usage.InputTokenCount ?? 0,
            OutputTokens = usage.OutputTokenCount ?? 0,
            TotalTokens = usage.TotalTokenCount ?? 0
        });

        // Keep queue size reasonable
        while (_recentUsage.Count > MaxRecords)
        {
            _recentUsage.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Gets the most recent token usage record.
    /// </summary>
    public TokenUsageRecord? GetLatest()
    {
        return _recentUsage.TryPeek(out var record) ? record : null;
    }

    /// <summary>
    /// Gets all recent usage records.
    /// </summary>
    public IEnumerable<TokenUsageRecord> GetRecent(int count = 10)
    {
        return _recentUsage.Reverse().Take(count);
    }
}

/// <summary>
/// Represents a recorded token usage event.
/// </summary>
public sealed record TokenUsageRecord
{
    public required DateTimeOffset Timestamp { get; init; }
    public required long InputTokens { get; init; }
    public required long OutputTokens { get; init; }
    public required long TotalTokens { get; init; }
}
