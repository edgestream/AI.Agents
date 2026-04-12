using MealPlanner.Abstractions;
using Microsoft.Extensions.AI;

/// <summary>
/// Factory for the "search_recipes" function, which allows searching for recipes by dish name.
/// </summary>
internal class SearchRecipesFunctionFactory
{
    /// <summary>
    /// Creates the "search_recipes" function using the IRecipeSource service to perform the search.
    /// </summary>
    /// <param name="sp">The service provider used to resolve dependencies.</param>
    /// <returns>An AIFunction that can be used to search for recipes.</returns>
    public static AIFunction CreateFunction(IServiceProvider sp)
    {
        var source = sp.GetRequiredService<IRecipeSource>();
        return AIFunctionFactory.Create(source.SearchRecipes,
            name: "search_recipes",
            description: "Search for a recipe by dish name.");
    }
}