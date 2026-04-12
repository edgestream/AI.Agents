using System.Text;
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

    public Task<Recipe> FetchRecipe(string url)
    {
        foreach (var recipe in _recipes)
        {           
            if (recipe.Url.Any(x => x.ToString() == url))
            {
                return Task.FromResult(recipe);
            }
        }
        throw new KeyNotFoundException($"Recipe with URL '{url}' not found.");
    }

    /// <inheritdoc />
    public async Task<string> GetRecipe(string url)
    {
        var recipe = await FetchRecipe(url);

        var sb = new StringBuilder();

        var title = recipe.Name.OfType<string>().FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(title))
            sb.AppendLine($"Title: {title}");

        var description = recipe.Description.OfType<string>().FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(description))
            sb.AppendLine($"Description: {description}");

        var originUrl = recipe.Url.FirstOrDefault()
               ?? recipe.MainEntityOfPage.OfType<Uri>().FirstOrDefault();
        if (originUrl is not null)
            sb.AppendLine($"URL: {originUrl}");

        return sb.ToString().TrimEnd();
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
