using MealPlanner.Chefkoch;

namespace MealPlanner.Chefkoch.Tests;

[TestClass]
public sealed class Iso8601DurationTests
{
    [TestMethod]
    public void ParseIso8601Duration_MinutesOnly()
    {
        var result = ChefkochRecipeSource.ParseIso8601Duration("PT15M");
        Assert.AreEqual(TimeSpan.FromMinutes(15), result);
    }

    [TestMethod]
    public void ParseIso8601Duration_HoursAndMinutes()
    {
        var result = ChefkochRecipeSource.ParseIso8601Duration("PT1H30M");
        Assert.AreEqual(new TimeSpan(1, 30, 0), result);
    }

    [TestMethod]
    public void ParseIso8601Duration_HoursOnly()
    {
        var result = ChefkochRecipeSource.ParseIso8601Duration("PT2H");
        Assert.AreEqual(TimeSpan.FromHours(2), result);
    }

    [TestMethod]
    public void ParseIso8601Duration_SecondsOnly()
    {
        var result = ChefkochRecipeSource.ParseIso8601Duration("PT45S");
        Assert.AreEqual(TimeSpan.FromSeconds(45), result);
    }

    [TestMethod]
    public void ParseIso8601Duration_Null_ReturnsNull()
    {
        var result = ChefkochRecipeSource.ParseIso8601Duration(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseIso8601Duration_Empty_ReturnsNull()
    {
        var result = ChefkochRecipeSource.ParseIso8601Duration("");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseIso8601Duration_NonPTPrefix_ReturnsNull()
    {
        var result = ChefkochRecipeSource.ParseIso8601Duration("P1D");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseIso8601Duration_CaseInsensitive()
    {
        var result = ChefkochRecipeSource.ParseIso8601Duration("pt10m");
        Assert.AreEqual(TimeSpan.FromMinutes(10), result);
    }
}
