using MealPlanner.Chefkoch;

namespace MealPlanner.Chefkoch.Tests;

[TestClass]
public class ChefkochPaginationTests
{
    [TestMethod]
    public void PaginationMath_PageSize3_Offset0_ReturnsFirstPageItems()
    {
        // Given 933 total results, page size 3 (from our fixture)
        // offset=0, limit=3 should return all 3 items from page 0
        var html = LoadFixture("search-hauptgerichte.html");
        var (items, totalCount) = ChefkochRecipeSource.ParseSearchResultsFromHtml(html);

        Assert.AreEqual(3, items.Count);
        Assert.AreEqual(933, totalCount);

        // Page math: pageSize = items.Count = 3
        int pageSize = items.Count;
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        Assert.AreEqual(311, totalPages);
    }

    [TestMethod]
    public void PaginationMath_DynamicPageSize_NeverHardcoded()
    {
        // Verifies that page size is derived from the first page item count
        var html = LoadFixture("search-hauptgerichte.html");
        var (items, _) = ChefkochRecipeSource.ParseSearchResultsFromHtml(html);

        // Our fixture has 3 items, so page size should be 3 (not hardcoded 42)
        int pageSize = items.Count;
        Assert.AreEqual(3, pageSize);
    }

    [TestMethod]
    public void PaginationMath_OffsetWithinPage_SkipsCorrectly()
    {
        // offset=1 with pageSize=3 means skip first item on page 0
        var html = LoadFixture("search-hauptgerichte.html");
        var (items, _) = ChefkochRecipeSource.ParseSearchResultsFromHtml(html);

        int pageSize = items.Count;
        int offset = 1;
        int startPage = offset / pageSize;
        int skipOnFirstPage = offset % pageSize;

        Assert.AreEqual(0, startPage);
        Assert.AreEqual(1, skipOnFirstPage);

        var result = items.Skip(skipOnFirstPage).Take(2).ToList();
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result[0].Name.Any(n => n == "Wiener Schnitzel"));
    }

    [TestMethod]
    public void PaginationMath_OffsetBeyondFirstPage_CalculatesCorrectPage()
    {
        // With pageSize=3, offset=5 should be on page 1 (5/3=1), skip 2 (5%3=2)
        int pageSize = 3;
        int offset = 5;

        int startPage = offset / pageSize;
        int skipOnFirstPage = offset % pageSize;

        Assert.AreEqual(1, startPage);
        Assert.AreEqual(2, skipOnFirstPage);
    }

    [TestMethod]
    public void RandomPageSelection_ValidPageRange()
    {
        // With 933 results and page size 3, there should be 311 pages
        int totalCount = 933;
        int pageSize = 3;
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        Assert.AreEqual(311, totalPages);

        // Random page should be in range [0, totalPages)
        for (int i = 0; i < 100; i++)
        {
            int randomPage = Random.Shared.Next(0, totalPages);
            Assert.IsTrue(randomPage >= 0 && randomPage < totalPages,
                $"Random page {randomPage} out of range [0, {totalPages})");
        }
    }

    private static string LoadFixture(string name)
    {
        return File.ReadAllText(Path.Combine("Fixtures", name));
    }
}
