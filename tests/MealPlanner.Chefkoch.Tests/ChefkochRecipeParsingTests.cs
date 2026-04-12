using MealPlanner.Chefkoch;

namespace MealPlanner.Chefkoch.Tests;

[TestClass]
public sealed class ChefkochRecipeParsingTests
{
    private static string LoadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [TestMethod]
    public void ParseRecipePage_ExtractsTitle()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.AreEqual("Spaghetti Carbonara", recipe.Title);
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsImage()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.IsNotNull(recipe.Image);
        Assert.IsTrue(recipe.Image.Contains("spaghetti-carbonara.jpg"));
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsRating()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(4.75, recipe.Rating);
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsReviewCount()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(312, recipe.ReviewCount);
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsPrepTime()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(TimeSpan.FromMinutes(15), recipe.PrepTime);
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsCookTime()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(TimeSpan.FromMinutes(20), recipe.CookTime);
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsServings()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(2, recipe.Servings);
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsIngredients()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(5, recipe.Ingredients.Count);
        Assert.AreEqual("200 g Spaghetti", recipe.Ingredients[0]);
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsInstructions()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(4, recipe.Instructions.Count);
        Assert.IsTrue(recipe.Instructions[0].Contains("Spaghetti"));
    }

    [TestMethod]
    public void ParseRecipePage_ExtractsNutrition()
    {
        var html = LoadFixture("recipe-1234567.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "1234567");

        Assert.IsNotNull(recipe);
        Assert.IsNotNull(recipe.Nutrition);
        Assert.AreEqual(550, recipe.Nutrition.Calories);
        Assert.AreEqual(22, recipe.Nutrition.ProteinGrams);
        Assert.AreEqual(28, recipe.Nutrition.FatGrams);
        Assert.AreEqual(48, recipe.Nutrition.CarbGrams);
        Assert.AreEqual(2, recipe.Nutrition.FiberGrams);
    }

    [TestMethod]
    public void ParseRecipePage_ArrayJsonLd_ExtractsRecipeType()
    {
        var html = LoadFixture("recipe-array-jsonld.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "arr-1");

        Assert.IsNotNull(recipe);
        Assert.AreEqual("Mediterranean Quinoa Bowl", recipe.Title);
    }

    [TestMethod]
    public void ParseRecipePage_ArrayJsonLd_ParsesArrayImage()
    {
        var html = LoadFixture("recipe-array-jsonld.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "arr-1");

        Assert.IsNotNull(recipe);
        Assert.IsNotNull(recipe.Image);
        Assert.IsTrue(recipe.Image.Contains("quinoa"));
    }

    [TestMethod]
    public void ParseRecipePage_ArrayJsonLd_FallsBackToCookTimeFromTotalTime()
    {
        var html = LoadFixture("recipe-array-jsonld.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "arr-1");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(TimeSpan.FromMinutes(10), recipe.PrepTime);
        Assert.AreEqual(TimeSpan.FromMinutes(25), recipe.CookTime); // totalTime fallback
    }

    [TestMethod]
    public void ParseRecipePage_ArrayJsonLd_ParsesStringInstructions()
    {
        var html = LoadFixture("recipe-array-jsonld.html");

        var recipe = ChefkochRecipeSource.ParseRecipePage(html, "arr-1");

        Assert.IsNotNull(recipe);
        Assert.AreEqual(1, recipe.Instructions.Count); // single string → one instruction
    }

    [TestMethod]
    public void ParseRecipePage_InvalidHtml_ReturnsNull()
    {
        var recipe = ChefkochRecipeSource.ParseRecipePage("<html><body>No JSON-LD here</body></html>", "invalid");

        Assert.IsNull(recipe);
    }
}
