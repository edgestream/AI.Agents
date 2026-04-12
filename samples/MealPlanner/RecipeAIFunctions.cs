using System.ComponentModel;
using System.Text.Json;
using MealPlanner.Abstractions;
using Microsoft.Extensions.AI;

namespace MealPlanner;

/// <summary>
/// Creates AI functions that enable the agent to search for recipes and retrieve
/// recipe details from an <see cref="IRecipeSource"/>. The <c>search_recipes</c>
/// function returns A2UI surface operations that render a recipe card in the chat.
/// </summary>
public sealed class RecipeAIFunctions
{
    // PropertyNamingPolicy = null keeps property names exactly as written in the
    // anonymous-object literals below. The discriminator keys ("Card", "Tabs", …)
    // are C# property names and must stay PascalCase, which they do with null policy.
    private static readonly JsonSerializerOptions _options = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = null,
    };

    private readonly IRecipeSource _recipeSource;

    public RecipeAIFunctions(IRecipeSource recipeSource)
    {
        _recipeSource = recipeSource ?? throw new ArgumentNullException(nameof(recipeSource));
    }

    /// <summary>
    /// Creates the <c>search_recipes</c> AI function that searches for recipes via
    /// the configured <see cref="IRecipeSource"/> and returns an A2UI card.
    /// </summary>
    public AIFunction CreateSearchFunction() =>
        AIFunctionFactory.Create(
            SearchAsync,
            name: "search_recipes",
            description: "Search for a recipe by dish name or ingredient. Returns an A2UI card that is rendered automatically in the chat.",
            _options);

    /// <summary>
    /// Creates the <c>get_recipe</c> AI function that retrieves full recipe details
    /// from the configured <see cref="IRecipeSource"/>.
    /// </summary>
    public AIFunction CreateGetRecipeFunction() =>
        AIFunctionFactory.Create(
            GetRecipeAsync,
            name: "get_recipe",
            description: "Get full details for a recipe by its ID.",
            _options);

    [Description("Search for a recipe by dish name or ingredient.")]
    private async Task<object[]> SearchAsync(
        [Description("The search query, e.g. a dish name such as 'pasta' or 'carbonara'.")]
        string query)
    {
        var results = await _recipeSource.SearchAsync(query);

        // Take the first result for the A2UI card (or provide a fallback).
        Recipe? recipe = null;
        if (results.Count > 0)
        {
            recipe = await _recipeSource.GetRecipeAsync(results[0].Id);
        }

        recipe ??= CreateFallbackRecipe(query);

        return BuildA2UIOperations(recipe);
    }

    [Description("Get full details for a recipe by its ID.")]
    private async Task<object?> GetRecipeAsync(
        [Description("The recipe ID to retrieve.")]
        string id)
    {
        return await _recipeSource.GetRecipeAsync(id);
    }

    /// <summary>
    /// Builds A2UI surface operations for rendering a recipe card.
    /// </summary>
    internal static object[] BuildA2UIOperations(Recipe recipe)
    {
        var image = recipe.Image ?? "";
        var title = recipe.Title;
        var rating = recipe.Rating?.ToString("0.0") ?? "N/A";
        var reviewCountLabel = recipe.ReviewCount.HasValue ? $"({recipe.ReviewCount:N0} reviews)" : "";
        var prepTime = recipe.PrepTime.HasValue ? $"{(int)recipe.PrepTime.Value.TotalMinutes} min prep" : "";
        var cookTime = recipe.CookTime.HasValue ? $"{(int)recipe.CookTime.Value.TotalMinutes} min cook" : "";
        var servings = recipe.Servings.HasValue ? $"Serves {recipe.Servings}" : "";
        var ingredients = recipe.Ingredients.Select(i => new { text = i }).ToArray();
        var instructions = recipe.Instructions.Select(i => new { text = i }).ToArray();

        object[] components =
        [
            new { id = "root",
                  component = new { Card = new { child = "tabs-container" } } },

            new { id = "tabs-container",
                  component = new { Tabs = new { tabItems = new object[]
                  {
                      new { title = new { literalString = "Overview"     }, child = "overview-col"       },
                      new { title = new { literalString = "Ingredients"  }, child = "ingredients-list"   },
                      new { title = new { literalString = "Instructions" }, child = "instructions-list"  },
                  }}}},

            new { id = "overview-col",
                  component = new { Column = new { children = new { explicitList = new[] { "recipe-image", "overview-content" } } } } },

            new { id = "recipe-image",
                  component = new { Image = new { url = new { path = "/image" }, usageHint = "mediumFeature", fit = "cover" } } },

            new { id = "overview-content",
                  component = new { Column = new { children = new { explicitList = new[] { "title", "rating-row", "times-row", "servings" } } } } },

            new { id = "title",
                  component = new { Text = new { text = new { path = "/title" }, usageHint = "h3" } } },

            new { id = "rating-row",
                  component = new { Row = new { children = new { explicitList = new[] { "star-icon", "rating", "review-count" } } } } },

            new { id = "star-icon",
                  component = new { Icon = new { name = new { literalString = "star" } } } },

            new { id = "rating",
                  component = new { Text = new { text = new { path = "/rating" }, usageHint = "body" } } },

            new { id = "review-count",
                  component = new { Text = new { text = new { path = "/reviewCountLabel" }, usageHint = "caption" } } },

            new { id = "times-row",
                  component = new { Row = new { children = new { explicitList = new[] { "prep-time", "cook-time" } } } } },

            new { id = "prep-time",
                  component = new { Row = new { children = new { explicitList = new[] { "prep-icon", "prep-text" } } } } },

            new { id = "prep-icon",
                  component = new { Icon = new { name = new { literalString = "calendarToday" } } } },

            new { id = "prep-text",
                  component = new { Text = new { text = new { path = "/prepTime" }, usageHint = "caption" } } },

            new { id = "cook-time",
                  component = new { Row = new { children = new { explicitList = new[] { "cook-icon", "cook-text" } } } } },

            new { id = "cook-icon",
                  component = new { Icon = new { name = new { literalString = "timer" } } } },

            new { id = "cook-text",
                  component = new { Text = new { text = new { path = "/cookTime" }, usageHint = "caption" } } },

            new { id = "servings",
                  component = new { Text = new { text = new { path = "/servings" }, usageHint = "caption" } } },

            new { id = "ingredients-list",
                  component = new { Column = new { children = new { template = new { componentId = "item-template", dataBinding = "/ingredients" } } } } },

            new { id = "instructions-list",
                  component = new { Column = new { children = new { template = new { componentId = "item-template", dataBinding = "/instructions" } } } } },

            new { id = "item-template",
                  component = new { Text = new { text = new { path = "text" }, usageHint = "body" } } },
        ];

        object[] operations =
        [
            new { surfaceUpdate   = new { surfaceId = "recipe-surface", components } },
            new { dataModelUpdate = new { surfaceId = "recipe-surface", path = "/", contents = new { image, title, rating, reviewCountLabel, prepTime, cookTime, servings, ingredients, instructions } } },
            new { beginRendering  = new { surfaceId = "recipe-surface", root = "root", styles = new { } } },
        ];

        return operations;
    }

    private static Recipe CreateFallbackRecipe(string query) => new()
    {
        Id = "fallback",
        Title = $"Recipe for '{query}' (no results found)",
        Image = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=300&h=180&fit=crop",
        Ingredients = ["No ingredients available — try a different search term."],
        Instructions = ["No instructions available — try a different search term."],
    };
}
