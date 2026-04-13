using Schema.NET;

namespace MealPlanner.Abstractions;

/// <summary>
/// An interface for a recipe source, which can be used to retrieve recipes by URL or search for recipes by query. This allows for flexibility in how recipes are sourced, and enables the use of multiple recipe sources if desired.
/// </summary>
public interface IRecipeSource
{
    /// <summary>
    /// Search for recipes matching the query. Results should be returned in order of relevance, but may be randomized if the randomize parameter is set to true.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="offset">The number of results to skip.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="randomize">Whether to randomize the order of the results.</param>
    /// <returns>An asynchronous stream of recipes.</returns>
    public IAsyncEnumerable<Recipe> SearchRecipes(string query, int offset = 0, int limit = 10, bool randomize = false);
}