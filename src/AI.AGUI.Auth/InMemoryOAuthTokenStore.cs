using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AI.AGUI.Auth;

/// <summary>
/// In-memory implementation of <see cref="IOAuthTokenStore"/> for development and single-instance deployments.
/// Tokens are stored in a memory cache with configurable expiration.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is suitable for:
/// - Development and testing
/// - Single-instance deployments
/// - Scenarios where token persistence across restarts is not required
/// </para>
/// <para>
/// For production multi-instance deployments, replace with a distributed store (e.g., Redis, CosmosDB).
/// </para>
/// </remarks>
public sealed class InMemoryOAuthTokenStore : IOAuthTokenStore
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _slidingExpiration;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryOAuthTokenStore"/>.
    /// </summary>
    /// <param name="cache">The memory cache instance.</param>
    /// <param name="options">Token store configuration options.</param>
    public InMemoryOAuthTokenStore(IMemoryCache cache, IOptions<OAuthTokenStoreOptions> options)
    {
        _cache = cache;
        _slidingExpiration = options.Value.SlidingExpiration;
    }

    /// <inheritdoc />
    public ValueTask<OAuthToken?> GetTokenAsync(string userId, string mcpServerName, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, mcpServerName);
        var token = _cache.Get<OAuthToken>(key);
        return new(token);
    }

    /// <inheritdoc />
    public ValueTask SetTokenAsync(string userId, string mcpServerName, OAuthToken token, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, mcpServerName);
        var entryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = _slidingExpiration
        };

        // If the token has an expiration, use the earlier of sliding or absolute expiration
        if (token.ExpiresAt.HasValue)
        {
            entryOptions.AbsoluteExpiration = token.ExpiresAt.Value;
        }

        _cache.Set(key, token, entryOptions);
        return default;
    }

    /// <inheritdoc />
    public ValueTask RemoveTokenAsync(string userId, string mcpServerName, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(userId, mcpServerName);
        _cache.Remove(key);
        return default;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HasValidTokenAsync(string userId, string mcpServerName, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(userId, mcpServerName, cancellationToken);
        return token is not null && !token.IsExpired;
    }

    private static string GetCacheKey(string userId, string mcpServerName) =>
        $"oauth:{userId}:{mcpServerName}";
}

/// <summary>
/// Configuration options for <see cref="InMemoryOAuthTokenStore"/>.
/// </summary>
public sealed class OAuthTokenStoreOptions
{
    /// <summary>
    /// Gets or sets the sliding expiration for cached tokens.
    /// Defaults to 1 hour.
    /// </summary>
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromHours(1);
}
