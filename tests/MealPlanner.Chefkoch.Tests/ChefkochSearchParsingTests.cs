using MealPlanner.Chefkoch;

namespace MealPlanner.Chefkoch.Tests;

[TestClass]
public sealed class ChefkochSearchParsingTests
{
    private static string LoadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [TestMethod]
    public void ParseSearchResponse_ReturnsExpectedResults()
    {
        var json = LoadFixture("search-carbonara.json");

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    public void ParseSearchResponse_FirstResult_HasCorrectId()
    {
        var json = LoadFixture("search-carbonara.json");

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.AreEqual("1234567", results[0].Id);
    }

    [TestMethod]
    public void ParseSearchResponse_FirstResult_HasCorrectTitle()
    {
        var json = LoadFixture("search-carbonara.json");

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.AreEqual("Spaghetti Carbonara Original", results[0].Title);
    }

    [TestMethod]
    public void ParseSearchResponse_FirstResult_HasRating()
    {
        var json = LoadFixture("search-carbonara.json");

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.AreEqual(4.75, results[0].Rating);
    }

    [TestMethod]
    public void ParseSearchResponse_FirstResult_HasReviewCount()
    {
        var json = LoadFixture("search-carbonara.json");

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.AreEqual(312, results[0].ReviewCount);
    }

    [TestMethod]
    public void ParseSearchResponse_FirstResult_HasImageUrl()
    {
        var json = LoadFixture("search-carbonara.json");

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.IsNotNull(results[0].Image);
        Assert.IsTrue(results[0].Image!.Contains("crop-360x240"));
    }

    [TestMethod]
    public void ParseSearchResponse_FirstResult_HasSourceUrl()
    {
        var json = LoadFixture("search-carbonara.json");

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.IsTrue(results[0].SourceUrl!.Contains("1234567"));
    }

    [TestMethod]
    public void ParseSearchResponse_SecondResult_HasCorrectTitle()
    {
        var json = LoadFixture("search-carbonara.json");

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.AreEqual("Vegetarische Carbonara", results[1].Title);
    }

    [TestMethod]
    public void ParseSearchResponse_EmptyResults_ReturnsEmptyList()
    {
        var json = """{ "count": 0, "results": [] }""";

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void ParseSearchResponse_MissingResultsProperty_ReturnsEmptyList()
    {
        var json = """{ "count": 0 }""";

        var results = ChefkochRecipeSource.ParseSearchResponse(json);

        Assert.AreEqual(0, results.Count);
    }
}
