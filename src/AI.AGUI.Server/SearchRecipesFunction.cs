using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace AI.AGUI.Server;

public sealed class SearchRecipesFunction
{
    public static AIFunction CreateAIFunction() =>
        AIFunctionFactory.Create(
            SearchAsync,
            name: "search_recipes",
            description: "Search for a recipe by dish name or ingredient and return structured recipe data."
        );

    [Description("Search for a recipe by dish name or ingredient.")]
    private static Task<RecipeResult> SearchAsync(
        [Description("The search query, e.g. a dish name such as 'pasta' or 'carbonara'.")]
        string query
    )
    {
        // Stub data — demonstrates the A2UI rendering pipeline without a live API.
        var q = query.ToLowerInvariant();
        var recipe = q.Contains("pasta") || q.Contains("carbonara") || q.Contains("spaghetti")
            ? new RecipeResult(
                Title: "Spaghetti Carbonara",
                PrepTimeMinutes: 20,
                Ingredients: ["200g spaghetti", "100g pancetta", "2 eggs", "50g Parmesan", "Black pepper"],
                Steps: [
                    "Bring a large pot of salted water to the boil and cook spaghetti al dente.",
                    "Fry pancetta in a dry pan over medium heat until crispy.",
                    "Whisk eggs with grated Parmesan and a generous pinch of black pepper.",
                    "Drain pasta, reserve a cup of pasta water.",
                    "Remove pan from heat, add pasta and pancetta, pour egg mixture over, toss quickly.",
                    "Add pasta water a splash at a time until glossy. Serve immediately."
                ]
            )
            : new RecipeResult(
                Title: $"Classic {query} Recipe",
                PrepTimeMinutes: 30,
                Ingredients: [$"200g {query}", "Salt", "Pepper", "2 tbsp olive oil", "1 garlic clove"],
                Steps: [
                    "Prepare all ingredients.",
                    "Heat olive oil in a pan over medium-high heat.",
                    "Cook following the standard method for the dish.",
                    "Season to taste and serve hot."
                ]
            );

        return Task.FromResult(recipe);
    }
}

public sealed record RecipeResult(
    string Title,
    int PrepTimeMinutes,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<string> Steps
);
