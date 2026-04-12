using Schema.NET;

namespace MealPlanner.Abstractions;

/// <summary>
/// An interface for a recipe source, which can be used to retrieve recipes by URL or search for recipes by query. This allows for flexibility in how recipes are sourced, and enables the use of multiple recipe sources if desired.
/// </summary>
public interface IRecipeSource
{
    /// <summary>
    /// Fetches a recipe by URL and returns a structured <see cref="Recipe"/> object.
    /// This is used internally by the provider to retrieve recipe data,
    /// which can then be transformed into different formats (e.g. summary string, A2UI card)
    /// for use by the LLM and UI. The full Recipe object is never returned to the model,
    /// only internal summaries or renderings.
    /// </summary>
    /// <param name="url">The URL of the recipe.</param>
    /// <returns>A structured <see cref="Recipe"/> object.</returns>
    public Task<Recipe> FetchRecipe(string url);

    /// <summary>
    /// Fetches a recipe by URL and returns a compact, human-readable summary the LLM can reason about (title, description, timing, yield).
    /// Use this before deciding which action to take next (e.g. render card, get nutrition, build shopping list).
    /// </summary>
    /// <param name="url">The URL of the recipe.</param>
    /// <returns>A compact, human-readable summary of the recipe.</returns>
    public Task<string> GetRecipe(string url);

    /// <summary>
    /// Search for recipes matching the query. Results should be returned in order of relevance, but may be randomized if the randomize parameter is set to true.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="offset">The number of results to skip.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="randomize">Whether to randomize the order of the results.</param>
    /// <returns>An asynchronous stream of recipe summaries.</returns>
    public IAsyncEnumerable<ListItem> SearchRecipes(string query, int offset = 0, int limit = 10, bool randomize = false);
}