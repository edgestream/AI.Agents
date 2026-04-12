using System.Text.Json;
using MealPlanner.Abstractions;
using Microsoft.Extensions.AI;

internal static class RecipeFunctionFactory
{
    private static readonly JsonSerializerOptions A2UIJsonSerializerOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = null,
    };

    public static AIFunction CreateGetFunction(IServiceProvider sp)
    {
        var source = sp.GetRequiredService<IRecipeSource>();
        return AIFunctionFactory.Create(source.GetRecipe,
            name: "get_recipe",
            description: "Get a recipe by URL and return a compact summary.");
    }

    public static AIFunction CreateRenderFunction(IServiceProvider sp)
    {
        var source = sp.GetRequiredService<IRecipeSource>();
        var renderer = sp.GetRequiredKeyedService<IRecipeRenderer>("a2ui");
        
        async Task<object[]> FetchAndRenderRecipe(string url) {
            var recipe = await source.FetchRecipe(url);
            return renderer.RenderRecipe(recipe);
        }

        return AIFunctionFactory.Create(FetchAndRenderRecipe,
            name: "render_recipe",
            description: "Fetch a recipe by URL and render it as an interactive card in the UI.",
            A2UIJsonSerializerOptions);
    }

    public static AIFunction CreateSearchFunction(IServiceProvider sp)
    {
        var source = sp.GetRequiredService<IRecipeSource>();
        return AIFunctionFactory.Create(source.SearchRecipes,
            name: "search_recipes",
            description: "Search for recipes by dish name. Returns a list of recipe URLs.");
    }
}