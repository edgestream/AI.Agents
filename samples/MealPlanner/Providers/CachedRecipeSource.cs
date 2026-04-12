using MealPlanner.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Schema.NET;

namespace MealPlanner.Providers;

/// <summary>
/// Decorator that caches <see cref="IRecipeSource.GetRecipe"/> results in an
/// <see cref="IMemoryCache"/> so that multiple tools (render card, nutrition, etc.)
/// called for the same URL within a conversation turn share a single HTTP fetch.
/// <see cref="SearchRecipes"/> results are not cached because search queries are
/// inherently varied and pagination-sensitive.
/// </summary>
internal sealed class CachedRecipeSource : IRecipeSource
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(10);
    private const string CacheKeyPrefix = "recipe:";

    private readonly IRecipeSource _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration;

    public CachedRecipeSource(IRecipeSource inner, IMemoryCache cache, TimeSpan? cacheDuration = null)
    {
        _inner = inner;
        _cache = cache;
        _cacheDuration = cacheDuration ?? DefaultCacheDuration;
    }

    /// <inheritdoc />
    public Task<Recipe> FetchRecipe(string url)
    {
        var uri = new Uri(url);
        var key = CacheKeyPrefix + uri.AbsoluteUri;
        return _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            entry.Size = 1;
            return _inner.FetchRecipe(url);
        })!;
    }

    /// <inheritdoc />
    public Task<string> GetRecipe(string url)
        => _inner.GetRecipe(url);
    
    /// <inheritdoc />
    public IAsyncEnumerable<ListItem> SearchRecipes(string query, int offset = 0, int limit = 10, bool randomize = false)
        => _inner.SearchRecipes(query, offset, limit, randomize);
}
