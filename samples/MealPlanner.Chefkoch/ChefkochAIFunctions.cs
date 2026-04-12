using System.ComponentModel;
using MealPlanner.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.Chefkoch;

/// <summary>
/// Factory for creating AI functions that wrap the Chefkoch recipe source.
/// </summary>
public static class ChefkochAIFunctions
{
    /// <summary>
    /// Creates the "chefkoch_search_recipes" AI function.
    /// </summary>
    public static AIFunction CreateSearchFunction(IServiceProvider sp)
    {
        var source = sp.GetRequiredService<ChefkochRecipeSource>();
        return AIFunctionFactory.Create(
            source.SearchRecipes,
            name: "chefkoch_search_recipes",
            description: "Search for recipes on Chefkoch.de by keyword or category. Returns a list of recipe summaries from the German recipe site.");
    }

    /// <summary>
    /// Creates the "chefkoch_get_recipe" AI function.
    /// </summary>
    public static AIFunction CreateGetRecipeFunction(IServiceProvider sp)
    {
        var source = sp.GetRequiredService<ChefkochRecipeSource>();
        return AIFunctionFactory.Create(
            source.GetRecipe,
            name: "chefkoch_get_recipe",
            description: "Get full recipe details from a Chefkoch.de recipe URL. Returns ingredients, instructions, timings, and ratings.");
    }
}
