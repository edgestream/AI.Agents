using Microsoft.Extensions.Caching.Memory;

namespace AI.Agents.OAuth;

/// <summary>
/// In-memory implementation of <see cref="IOAuthStateStore"/> for storing OAuth authorization state.
/// States automatically expire after a short period (10 minutes by default).
/// </summary>
public sealed class InMemoryOAuthStateStore : IOAuthStateStore
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _stateLifetime = TimeSpan.FromMinutes(10);

    public InMemoryOAuthStateStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public ValueTask StoreStateAsync(OAuthState state, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(state.StateId);
        _cache.Set(key, state, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _stateLifetime
        });
        return default;
    }

    /// <inheritdoc />
    public ValueTask<OAuthState?> ConsumeStateAsync(string stateId, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(stateId);
        if (_cache.TryGetValue<OAuthState>(key, out var state))
        {
            _cache.Remove(key);
            return new(state);
        }

        return new((OAuthState?)null);
    }

    private static string GetCacheKey(string stateId) => $"oauth_state:{stateId}";
}