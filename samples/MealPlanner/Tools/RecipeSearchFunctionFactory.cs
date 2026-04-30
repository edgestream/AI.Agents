using System.Text.Json;
using MealPlanner.Abstractions;
using Microsoft.Extensions.AI;

internal static class RecipeSearchFunctionFactory
{
    private static readonly JsonSerializerOptions A2UIJsonSerializerOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = null,
    };
    public static AIFunction CreateAIFunction(IServiceProvider sp)
    {
        var source = sp.GetRequiredService<IRecipeSource>();
        return AIFunctionFactory.Create(source.SearchRecipes,
            name: "search_recipes",
            description: "Search for cooking recipes.");
            //A2UIJsonSerializerOptions);
    }
}
