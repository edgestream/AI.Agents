using MealPlanner.Chefkoch;

namespace MealPlanner.Chefkoch.Tests;

[TestClass]
public class ChefkochRecipeParsingTests
{
    private static string LoadFixture(string name)
    {
        var path = Path.Combine("Fixtures", name);
        return File.ReadAllText(path);
    }

    [TestMethod]
    public void ParseSearchResults_ExtractsItemListEntries()
    {
        var html = LoadFixture("search-hauptgerichte.html");
        var (items, totalCount) = ChefkochRecipeSource.ParseSearchResultsFromHtml(html);

        Assert.AreEqual(3, items.Count);
        Assert.AreEqual(933, totalCount);

        // Verify first item
        Assert.IsTrue(items[0].Name.Any(n => n == "Spaghetti Carbonara"));
        Assert.IsTrue(items[0].Url.Any(u => u.ToString().Contains("1234567")));
        Assert.IsTrue(items[0].Description.Any(d => d.ToString()!.Contains("Klassische italienische")));

        // Verify second item
        Assert.IsTrue(items[1].Name.Any(n => n == "Wiener Schnitzel"));

        // Verify third item
        Assert.IsTrue(items[2].Name.Any(n => n == "Kartoffelpuffer"));
    }

    [TestMethod]
    public void ParseSearchResults_ZeroResults_ReturnsEmptyList()
    {
        var html = LoadFixture("search-zero-results.html");
        var (items, totalCount) = ChefkochRecipeSource.ParseSearchResultsFromHtml(html);

        Assert.AreEqual(0, items.Count);
        Assert.AreEqual(0, totalCount);
    }

    [TestMethod]
    public void ParseSearchResults_NoItemList_ThrowsInvalidOperationException()
    {
        var html = LoadFixture("search-no-itemlist.html");

        Assert.ThrowsException<InvalidOperationException>(() =>
            ChefkochRecipeSource.ParseSearchResultsFromHtml(html));
    }

    [TestMethod]
    public void ParseRecipe_ExtractsRecipeJsonLd()
    {
        var html = LoadFixture("recipe-1234567.html");
        var recipe = ChefkochRecipeSource.ParseRecipeFromHtml(html);

        Assert.IsTrue(recipe.Name.Any(n => n == "Spaghetti Carbonara"));
        Assert.IsTrue(recipe.Description.Any(d => d.ToString()!.Contains("Klassische italienische")));
        Assert.IsTrue(recipe.Url.Any(u => u.ToString().Contains("1234567")));
        Assert.IsTrue(recipe.Image.Any(i => i.ToString()!.Contains("chefkoch-cdn.de")));

        // Verify timings
        Assert.IsTrue(recipe.PrepTime.Any(t => t == TimeSpan.FromMinutes(15)));
        Assert.IsTrue(recipe.CookTime.Any(t => t == TimeSpan.FromMinutes(20)));
        Assert.IsTrue(recipe.TotalTime.Any(t => t == TimeSpan.FromMinutes(35)));

        // Verify yield
        Assert.IsTrue(recipe.RecipeYield.Any(y => y.ToString()!.Contains("4 Portionen")));

        // Verify ingredients
        var ingredients = recipe.RecipeIngredient.ToList();
        Assert.IsTrue(ingredients.Count >= 6);
        Assert.IsTrue(ingredients.Any(i => i.ToString()!.Contains("Spaghetti")));
        Assert.IsTrue(ingredients.Any(i => i.ToString()!.Contains("Guanciale")));
        Assert.IsTrue(ingredients.Any(i => i.ToString()!.Contains("Eigelb")));

        // Verify rating
        Assert.IsTrue(recipe.AggregateRating.Any());
    }

    [TestMethod]
    public void ParseRecipe_NoRecipeJsonLd_ThrowsInvalidOperationException()
    {
        var html = LoadFixture("recipe-no-jsonld.html");

        Assert.ThrowsException<InvalidOperationException>(() =>
            ChefkochRecipeSource.ParseRecipeFromHtml(html));
    }

    [TestMethod]
    public void ExtractJsonLdBlocks_ReturnsAllNonEmptyBlocks()
    {
        var html = LoadFixture("search-hauptgerichte.html");
        var blocks = ChefkochRecipeSource.ExtractJsonLdBlocks(html);

        // The fixture has 4 script tags: 1 empty, 1 ItemList, 1 BreadcrumbList, 1 WebSite
        // Empty ones are filtered out
        Assert.AreEqual(3, blocks.Count);
    }

    [TestMethod]
    public void ExtractRawRecipeJsonLd_ReturnsExactBlock()
    {
        var html = LoadFixture("recipe-1234567.html");
        var rawJson = ChefkochRecipeSource.ExtractRawRecipeJsonLd(html);

        Assert.IsNotNull(rawJson);
        Assert.IsTrue(rawJson.Contains("\"@type\": \"Recipe\""));
        Assert.IsTrue(rawJson.Contains("Spaghetti Carbonara"));

        // Verify it does NOT contain WebSite data
        Assert.IsFalse(rawJson.Contains("\"@type\": \"WebSite\""));
    }

    [TestMethod]
    public void ExtractRawRecipeJsonLd_NoRecipe_ReturnsNull()
    {
        var html = LoadFixture("recipe-no-jsonld.html");
        var rawJson = ChefkochRecipeSource.ExtractRawRecipeJsonLd(html);

        Assert.IsNull(rawJson);
    }

    [TestMethod]
    public void BuildSearchUrl_FormatsCorrectly()
    {
        var url = ChefkochRecipeSource.BuildSearchUrl(0, "hauptgerichte");
        Assert.AreEqual("https://www.chefkoch.de/rs/s0/hauptgerichte/Rezepte.html", url);

        var url2 = ChefkochRecipeSource.BuildSearchUrl(5, "Wiener Schnitzel");
        Assert.AreEqual("https://www.chefkoch.de/rs/s5/Wiener%20Schnitzel/Rezepte.html", url2);
    }

    [TestMethod]
    public void ParseTotalCount_Extracts933FromH1()
    {
        var html = LoadFixture("search-hauptgerichte.html");
        var count = ChefkochRecipeSource.ParseTotalCountFromHtml(html);

        Assert.AreEqual(933, count);
    }

    [TestMethod]
    public void ParseTotalCount_ZeroResults_Returns0()
    {
        var html = LoadFixture("search-zero-results.html");
        var count = ChefkochRecipeSource.ParseTotalCountFromHtml(html);

        Assert.AreEqual(0, count);
    }
}
