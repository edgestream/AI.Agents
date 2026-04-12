using MealPlanner.Chefkoch;
using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.E2ETests;

/// <summary>
/// Live integration tests for ChefkochRecipeSource.
/// These tests hit the real Chefkoch.de website and are excluded from normal CI runs.
/// Run with: dotnet test --filter "TestCategory=Live"
/// </summary>
[TestClass]
[TestCategory("Live")]
public class ChefkochLiveTests
{
    private static ChefkochRecipeSource CreateSource()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("chefkoch");
        var sp = services.BuildServiceProvider();
        return new ChefkochRecipeSource(sp.GetRequiredService<IHttpClientFactory>());
    }

    [TestMethod]
    public async Task SearchRecipes_Hauptgerichte_ReturnsResults()
    {
        var source = CreateSource();
        var results = new List<Schema.NET.ListItem>();

        await foreach (var item in source.SearchRecipes("hauptgerichte", limit: 5))
        {
            results.Add(item);
        }

        Assert.IsTrue(results.Count > 0, "Expected at least one result for 'hauptgerichte'");
        Assert.IsTrue(results.Count <= 5, "Expected at most 5 results");

        foreach (var item in results)
        {
            Assert.IsTrue(item.Name.Any(), "Name should be present");
            Assert.IsTrue(item.Url.Any(), "URL should be present");
        }
    }

    [TestMethod]
    public async Task SearchRecipes_WithOffset_ReturnsSubsequentResults()
    {
        var source = CreateSource();
        var firstPage = new List<Schema.NET.ListItem>();
        var secondPage = new List<Schema.NET.ListItem>();

        await foreach (var item in source.SearchRecipes("pasta", limit: 3))
        {
            firstPage.Add(item);
        }

        await foreach (var item in source.SearchRecipes("pasta", offset: 3, limit: 3))
        {
            secondPage.Add(item);
        }

        Assert.IsTrue(firstPage.Count > 0, "First page should have results");
        Assert.IsTrue(secondPage.Count > 0, "Second page should have results");

        // Results should be different
        var firstUrls = firstPage.SelectMany(x => x.Url).Select(u => u.ToString()).ToHashSet();
        var secondUrls = secondPage.SelectMany(x => x.Url).Select(u => u.ToString()).ToHashSet();
        Assert.IsFalse(firstUrls.SetEquals(secondUrls), "First and second page results should differ");
    }

    [TestMethod]
    public async Task SearchRecipes_Randomize_ReturnsResults()
    {
        var source = CreateSource();
        var results = new List<Schema.NET.ListItem>();

        await foreach (var item in source.SearchRecipes("kuchen", randomize: true, limit: 5))
        {
            results.Add(item);
        }

        Assert.IsTrue(results.Count > 0, "Expected at least one random result");
    }

    [TestMethod]
    public async Task GetRecipe_ReturnsFullRecipeDetails()
    {
        // First search for a recipe to get a valid URL
        var source = CreateSource();
        Schema.NET.ListItem? firstResult = null;

        await foreach (var item in source.SearchRecipes("spaghetti", limit: 1))
        {
            firstResult = item;
            break;
        }

        Assert.IsNotNull(firstResult, "Need at least one search result");
        var recipeUrl = firstResult.Url.First();

        var recipe = await source.GetRecipe(recipeUrl);

        Assert.IsTrue(recipe.Name.Any(), "Recipe name should be present");
        Assert.IsTrue(recipe.RecipeIngredient.Any(), "Recipe ingredients should be present");
    }
}
