using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace MealPlanner;

public sealed class SearchRecipesFunction
{
    // PropertyNamingPolicy = null keeps property names exactly as written in the
    // anonymous-object literals below. The discriminator keys ("Card", "Tabs", …)
    // are C# property names and must stay PascalCase, which they do with null policy.
    // Copy from Default so the TypeInfoResolver (reflection) is inherited.
    private static readonly JsonSerializerOptions _options = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = null,
    };

    public static AIFunction CreateAIFunction() =>
        AIFunctionFactory.Create(
            SearchAsync,
            name: "search_recipes",
            description: "Search for a recipe by dish name or ingredient. Returns an A2UI card that is rendered automatically in the chat.",
            _options);

    [Description("Search for a recipe by dish name or ingredient.")]
    private static Task<object[]> SearchAsync(
        [Description("The search query, e.g. a dish name such as 'pasta' or 'carbonara'.")]
        string query
    )
    {
        var normalizedQuery = query.ToLowerInvariant();

        string title, image, rating, prepTime, cookTime, servings, reviewCountLabel;
        object[] ingredients, instructions;

        if (normalizedQuery.Contains("pasta") || normalizedQuery.Contains("carbonara") || normalizedQuery.Contains("spaghetti"))
        {
            title            = "Spaghetti Carbonara";
            image            = "https://images.unsplash.com/photo-1612874742237-6526221588e3?w=300&h=180&fit=crop";
            rating           = "4.8";
            reviewCountLabel = "(2,341 reviews)";
            prepTime         = "15 min prep";
            cookTime         = "20 min cook";
            servings         = "Serves 2";
            ingredients =
            [
                new { text = "200g spaghetti" },
                new { text = "100g pancetta" },
                new { text = "2 eggs" },
                new { text = "50g Parmesan, grated" },
                new { text = "Black pepper" },
            ];
            instructions =
            [
                new { text = "1. Cook spaghetti in salted boiling water until al dente." },
                new { text = "2. Fry pancetta in a dry pan until crispy." },
                new { text = "3. Whisk eggs with Parmesan and black pepper." },
                new { text = "4. Drain pasta, reserving a cup of pasta water." },
                new { text = "5. Toss pasta with pancetta off heat and mix quickly with egg mixture." },
                new { text = "6. Add pasta water gradually until creamy. Serve immediately." },
            ];
        }
        else
        {
            title            = "Mediterranean Quinoa Bowl";
            image            = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=300&h=180&fit=crop";
            rating           = "4.9";
            reviewCountLabel = "(1,247 reviews)";
            prepTime         = "15 min prep";
            cookTime         = "20 min cook";
            servings         = "Serves 4";
            ingredients =
            [
                new { text = "1 cup quinoa" },
                new { text = "2 cups water" },
                new { text = "1 cucumber, diced" },
                new { text = "1 cup cherry tomatoes, halved" },
            ];
            instructions =
            [
                new { text = "1. Rinse quinoa and bring to a boil in water." },
                new { text = "2. Reduce heat and simmer for 15 minutes." },
                new { text = "3. Fluff with a fork and let cool." },
                new { text = "4. Mix with diced vegetables." },
            ];
        }

        // v0.8 format: component is a discriminated-union object { TypeName: { ...props } }
        // StringValue = { path: "..." } or { literalString: "..." }
        // children (Column/Row/List) = { explicitList: [...] } or { template: { componentId, dataBinding } }
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

        return Task.FromResult(operations);
    }
}
