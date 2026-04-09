using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AI.AGUI.Server;

public sealed class SearchRecipesFunction
{
    // PropertyNamingPolicy = null preserves PascalCase on anonymous-object keys such as
    // "Column", "Text", "List" — the A2UI renderer is case-sensitive on these discriminators.
    // Copy from Default so the TypeInfoResolver (reflection) is inherited.
    private static readonly JsonSerializerOptions _pascalCaseOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = null,
    };

    public static AIFunction CreateAIFunction() =>
        AIFunctionFactory.Create(
            SearchAsync,
            name: "search_recipes",
            description: "Search for a recipe by dish name or ingredient. Returns an A2UI card that is rendered automatically in the chat.",
            _pascalCaseOptions);

    [Description("Search for a recipe by dish name or ingredient.")]
    private static Task<object[]> SearchAsync(
        [Description("The search query, e.g. a dish name such as 'pasta' or 'carbonara'.")]
        string query
    )
    {
        var normalizedQuery = query.ToLowerInvariant();

        string title;
        string prepTime;
        object[] ingredients;
        object[] steps;

        if (normalizedQuery.Contains("pasta") || normalizedQuery.Contains("carbonara") || normalizedQuery.Contains("spaghetti"))
        {
            title = "Spaghetti Carbonara";
            prepTime = "20 min";
            ingredients = ["200g spaghetti", "100g pancetta", "2 eggs", "50g Parmesan", "Black pepper"];
            steps =
            [
                "Bring a large pot of salted water to the boil and cook spaghetti al dente.",
                "Fry pancetta in a dry pan over medium heat until crispy.",
                "Whisk eggs with grated Parmesan and a generous pinch of black pepper.",
                "Drain pasta, reserve a cup of pasta water.",
                "Remove pan from heat, add pasta and pancetta, pour egg mixture over, toss quickly.",
                "Add pasta water a splash at a time until glossy. Serve immediately.",
            ];
        }
        else
        {
            title = $"Classic {query} Recipe";
            prepTime = "30 min";
            ingredients = [$"200g {query}", "Salt", "Pepper", "2 tbsp olive oil", "1 garlic clove"];
            steps =
            [
                "Prepare all ingredients.",
                "Heat olive oil in a pan over medium-high heat.",
                "Cook following the standard method for the dish.",
                "Season to taste and serve hot.",
            ];
        }

        object[] components =
        [
            new { id = "root-col",         component = new { Column = new { children = new { explicitList = new[] { "recipe-title", "prep-time", "ingredients-list", "steps-list" } } } } },
            new { id = "recipe-title",     component = new { Text   = new { usageHint = "h1",       text = new { path = "title"       } } } },
            new { id = "prep-time",        component = new { Text   = new { usageHint = "label",    text = new { path = "prepTime"    } } } },
            new { id = "ingredients-list", component = new { List   = new { direction = "vertical", children = new { template = new { componentId = "ingredient-item", dataBinding = "/ingredients" } } } } },
            new { id = "ingredient-item",  component = new { Text   = new { usageHint = "body",     text = new { path = "name"        } } } },
            new { id = "steps-list",       component = new { List   = new { direction = "vertical", children = new { template = new { componentId = "step-item",       dataBinding = "/steps"       } } } } },
            new { id = "step-item",        component = new { Text   = new { usageHint = "body",     text = new { path = "instruction" } } } },
        ];

        object[] ingredientItems = ingredients.Select(i => (object)new { name = (string)i }).ToArray();
        object[] stepItems       = steps.Select(s => (object)new { instruction = (string)s }).ToArray();

        object[] operations =
        [
            new { surfaceUpdate   = new { surfaceId = "recipe-surface", components } },
            new { dataModelUpdate = new { surfaceId = "recipe-surface", path = "/", contents = new { title, prepTime, ingredients = ingredientItems, steps = stepItems } } },
            new { beginRendering  = new { surfaceId = "recipe-surface", root = "root-col", styles = new { } } },
        ];

        return Task.FromResult(operations);
    }
}
