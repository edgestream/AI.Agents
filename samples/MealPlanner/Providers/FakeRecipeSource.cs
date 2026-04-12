using MealPlanner.Abstractions;
using Schema.NET;

namespace MealPlanner.Providers;

/// <summary>
/// A fake recipe source that provides a small set of hardcoded recipes.
/// This can be used for testing and development purposes.
/// </summary>
internal class FakeRecipeSource : IRecipeSource
{
    private readonly Recipe[] _recipes = [
        new Recipe
        {
            Url = new Uri("https://fake-recipe/spaghetti-alla-puttanesca.html"),
            Name = "Spaghetti alla puttanesca",
            Description = "Spaghetti mit pikanter Tomatensauce",
            RecipeIngredient = new([
                "3 Zehe/n Knoblauch",
                "10 Sardellenfilet(s)",
                "150 g Oliven, schwarz, entsteinte",
                "0.5 EL Kapern",
                "1 Dose Tomate(n), gestückelt",
                "1 Msp. Peperoncini, scharfe, getrocknete",
                "400 g Spaghetti",
                "7 EL Olivenöl"
            ])
        },
        new Recipe
        {
            Url = new Uri("https://fake-recipe/spaghetti-carbonara.html"),
            Name = "Spaghetti Carbonara",
            Description = "Carbonara wie bei der Mamma in Rom"
        },
        new Recipe
        {
            Url = new Uri("https://fake-recipe/spaghetti-bolognese.html"),
            Name = "Spaghetti Bolognese",
            Description = "aus Bologna"
        }
    ];
    public Task<Recipe> GetRecipe(Uri url)
    {
        foreach (var recipe in _recipes)
        {           
            if (recipe.Url.Any(x => x == url))
            {
                return Task.FromResult(recipe);
            }
        }
        throw new KeyNotFoundException($"Recipe with URL '{url}' not found.");
    }
    public async IAsyncEnumerable<ListItem> SearchRecipes(string query, int offset = 0, int limit = 10, bool randomize = false)
    {
        foreach (var recipe in _recipes)
        {
            if (recipe.Name.Any(x => x.Contains(query, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new ListItem
                {
                    Url = recipe.Url,
                    Name = recipe.Name,
                    Description = recipe.Description
                };
            }
        };
    }
}
