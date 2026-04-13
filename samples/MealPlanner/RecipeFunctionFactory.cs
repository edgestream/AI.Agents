using System.Text.Json;
using MealPlanner.Abstractions;
using Microsoft.Extensions.AI;

internal static class RecipeFunctionFactory
{
    private static readonly JsonSerializerOptions A2UIJsonSerializerOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = null,
    };

    public static AIFunction CreateSearchFunction(IServiceProvider sp)
    {
        var source = sp.GetRequiredService<IRecipeSource>();
        var renderer = sp.GetRequiredService<IRecipeRenderer>();
        
        async Task<object[]> SearchAndRenderRecipes(string query, int offset = 0, int limit = 10, bool randomize = false)
        {
            var cards = new List<object>();
            await foreach (var recipe in source.SearchRecipes(query, offset, limit, randomize))
            {
                cards.AddRange(renderer.RenderRecipe(recipe));
            }
            return cards.ToArray();
        }

        return AIFunctionFactory.Create(SearchAndRenderRecipes,
            name: "search_and_render_recipes",
            description: "Search for recipes and render them as rich user interface components.",
            A2UIJsonSerializerOptions);
    }
}
