using MealPlanner;
using MealPlanner.Abstractions;
using MealPlanner.Chefkoch;
using Azure.AI.Projects;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);
builder.AddAIClient();
builder.Services.AddHttpClient<ChefkochRecipeSource>();
builder.Services.AddSingleton<IRecipeSource, ChefkochRecipeSource>();
builder.AddAGUIApplication("meal-planner", "Meal Planner", (sp, name) =>
{
    var projectClient = sp.GetRequiredService<AIProjectClient>();
    var recipeSource = sp.GetRequiredService<IRecipeSource>();
    var recipeFunctions = new RecipeAIFunctions(recipeSource);
    return projectClient.AsAIAgent(
        builder.Configuration["Foundry:Model"] ?? "gpt-5.3-chat",
        """
        You are a meal planning assistant that helps users discover recipes, compose balanced
        meal plans, and review nutritional information. You have two roles:

        **Chef role:** Assemble selected recipes into coherent meal plans (courses, timing, portions).
        **Nutrition role:** Evaluate nutritional balance, flag allergens, and suggest alternatives.

        When a user searches for recipes, use the search_recipes tool to find and display recipes.
        When reviewing a meal plan, provide nutritional feedback and chef recommendations.
        """,
        name,
        "An agent that helps users plan meals, find recipes, and get nutritional guidance.",
        [recipeFunctions.CreateSearchFunction(), recipeFunctions.CreateGetRecipeFunction()]
    );
});
var app = builder.Build();
app.MapGet("/health", () => "OK");
app.MapAGUI();
await app.RunAsync();