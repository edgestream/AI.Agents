namespace MealPlanner.Abstractions;

/// <summary>
/// Abstraction for pluggable recipe providers (Chefkoch, AllRecipes, local DB, etc.).
/// </summary>
public interface IRecipeSource
{
    /// <summary>
    /// Searches for recipes matching the given <paramref name="query"/>.
    /// </summary>
    Task<IReadOnlyList<RecipeSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves full recipe details by identifier.
    /// </summary>
    Task<Recipe?> GetRecipeAsync(string id, CancellationToken cancellationToken = default);
}
