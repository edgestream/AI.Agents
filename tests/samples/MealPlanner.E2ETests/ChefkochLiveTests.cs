using MealPlanner.Providers;
using Microsoft.Extensions.DependencyInjection;
using Schema.NET;

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
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();
        return new ChefkochRecipeSource(sp.GetRequiredService<IHttpClientFactory>());
    }

    [TestMethod]
    public async Task SearchRecipes_Hauptgerichte_ReturnsResults()
    {
        var source = CreateSource();
        var results = new List<Recipe>();

        await foreach (var recipe in source.SearchRecipes("hauptgerichte", limit: 5))
        {
            results.Add(recipe);
        }

        Assert.IsTrue(results.Count > 0, "Expected at least one result for 'hauptgerichte'");
        Assert.IsTrue(results.Count <= 5, "Expected at most 5 results");

        foreach (var recipe in results)
        {
            Assert.IsTrue(recipe.Name.Any(), "Name should be present");
        }
    }

    [TestMethod]
    public async Task SearchRecipes_WithOffset_ReturnsSubsequentResults()
    {
        var source = CreateSource();
        var firstPage = new List<Recipe>();
        var secondPage = new List<Recipe>();

        await foreach (var recipe in source.SearchRecipes("pasta", limit: 3))
        {
            firstPage.Add(recipe);
        }

        await foreach (var recipe in source.SearchRecipes("pasta", offset: 3, limit: 3))
        {
            secondPage.Add(recipe);
        }

        Assert.IsTrue(firstPage.Count > 0, "First page should have results");
        Assert.IsTrue(secondPage.Count > 0, "Second page should have results");

        // Results should be different
        var firstNames = firstPage.SelectMany(x => x.Name).ToHashSet();
        var secondNames = secondPage.SelectMany(x => x.Name).ToHashSet();
        Assert.IsFalse(firstNames.SetEquals(secondNames), "First and second page results should differ");
    }

    [TestMethod]
    public async Task SearchRecipes_Randomize_ReturnsResults()
    {
        var source = CreateSource();
        var results = new List<Recipe>();

        await foreach (var recipe in source.SearchRecipes("kuchen", randomize: true, limit: 5))
        {
            results.Add(recipe);
        }

        Assert.IsTrue(results.Count > 0, "Expected at least one random result");
    }

    [TestMethod]
    public async Task SearchRecipes_ReturnsFullRecipeDetails()
    {
        var source = CreateSource();
        Recipe? firstResult = null;

        await foreach (var recipe in source.SearchRecipes("spaghetti", limit: 1))
        {
            firstResult = recipe;
            break;
        }

        Assert.IsNotNull(firstResult, "Need at least one search result");
        Assert.IsTrue(firstResult.Name.Any(), "Recipe name should be present");
        Assert.IsTrue(firstResult.RecipeIngredient.Any(), "Recipe ingredients should be present");
    }
}
