using HtmlAgilityPack;
using MealPlanner.Abstractions;
using Schema.NET;

namespace MealPlanner.Providers;

/// <summary>
/// A recipe source that extracts recipe data from Chefkoch.de using JSON-LD structured data
/// and Schema.NET deserialization.
/// </summary>
internal class ChefkochRecipeSource : IRecipeSource
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ChefkochRecipeSource(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<Recipe> GetRecipe(Uri url)
    {
        using var httpClient = _httpClientFactory.CreateClient("chefkoch");
        var html = await httpClient.GetStringAsync(url);
        return ParseRecipeFromHtml(html);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ListItem> SearchRecipes(string query, int offset = 0, int limit = 10, bool randomize = false)
    {
        using var httpClient = _httpClientFactory.CreateClient("chefkoch");

        // Fetch the first page to determine page size and total count
        var firstPageHtml = await httpClient.GetStringAsync(BuildSearchUrl(0, query));
        var (firstPageItems, totalCount) = ParseSearchResultsFromHtml(firstPageHtml);

        if (totalCount == 0 || firstPageItems.Count == 0)
        {
            yield break;
        }

        int pageSize = firstPageItems.Count;

        if (randomize)
        {
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            int randomPage = Random.Shared.Next(0, totalPages);

            if (randomPage == 0)
            {
                foreach (var item in firstPageItems.Take(limit))
                {
                    yield return item;
                }
            }
            else
            {
                var randomPageHtml = await httpClient.GetStringAsync(BuildSearchUrl(randomPage, query));
                var (randomPageItems, _) = ParseSearchResultsFromHtml(randomPageHtml);
                foreach (var item in randomPageItems.Take(limit))
                {
                    yield return item;
                }
            }
            yield break;
        }

        // Calculate which page the offset falls on and skip within that page
        int startPage = offset / pageSize;
        int skipOnFirstPage = offset % pageSize;
        int remaining = limit;

        // If offset is on the first page (page 0), use already-fetched data
        if (startPage == 0)
        {
            foreach (var item in firstPageItems.Skip(skipOnFirstPage).Take(remaining))
            {
                yield return item;
                remaining--;
            }
            if (remaining <= 0) yield break;
            startPage = 1;
            skipOnFirstPage = 0;
        }

        // Fetch subsequent pages as needed
        for (int page = startPage; remaining > 0; page++)
        {
            var pageHtml = await httpClient.GetStringAsync(BuildSearchUrl(page, query));
            var (pageItems, _) = ParseSearchResultsFromHtml(pageHtml);
            if (pageItems.Count == 0) break;

            int skip = page == startPage && skipOnFirstPage > 0 ? skipOnFirstPage : 0;
            foreach (var item in pageItems.Skip(skip).Take(remaining))
            {
                yield return item;
                remaining--;
            }
        }
    }

    internal static string BuildSearchUrl(int pageIndex, string query)
    {
        return $"https://www.chefkoch.de/rs/s{pageIndex}/{Uri.EscapeDataString(query)}/Rezepte.html";
    }

    internal static Recipe ParseRecipeFromHtml(string html)
    {
        foreach (var block in ExtractJsonLdBlocks(html))
        {
            if (!IsJsonLdType(block, "Recipe")) continue;

            var recipe = SchemaSerializer.DeserializeObject<Recipe>(block);
            if (recipe != null)
            {
                return recipe;
            }
        }

        throw new InvalidOperationException("No Recipe JSON-LD block found on the page.");
    }

    internal static (List<ListItem> Items, int TotalCount) ParseSearchResultsFromHtml(string html)
    {
        ItemList? itemList = null;

        foreach (var block in ExtractJsonLdBlocks(html))
        {
            if (!IsJsonLdType(block, "ItemList")) continue;

            itemList = SchemaSerializer.DeserializeObject<ItemList>(block);
            if (itemList != null) break;
        }

        if (itemList == null)
        {
            throw new InvalidOperationException("No ItemList JSON-LD block found on the result page.");
        }

        var items = itemList.ItemListElement.OfType<ListItem>().ToList();
        int totalCount = ParseTotalCountFromHtml(html);

        return (items, totalCount);
    }

    internal static List<string> ExtractJsonLdBlocks(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scripts == null) return [];

        return scripts
            .Select(s => s.InnerText.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();
    }

    internal static int ParseTotalCountFromHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Look for result count text like "933 Rezepte"
        var resultCountNode = doc.DocumentNode.SelectSingleNode("//h1")
            ?? doc.DocumentNode.SelectSingleNode("//*[contains(@class,'search-result-count')]")
            ?? doc.DocumentNode.SelectSingleNode("//*[contains(@class,'result-count')]");

        if (resultCountNode != null)
        {
            var text = resultCountNode.InnerText.Trim();
            var match = System.Text.RegularExpressions.Regex.Match(text, @"([\d.]+)\s+Rezepte?");
            if (match.Success)
            {
                var numberStr = match.Groups[1].Value.Replace(".", "");
                if (int.TryParse(numberStr, out int count))
                {
                    return count;
                }
            }
        }

        // Fallback: scan all text for "N Rezepte" pattern
        var allText = doc.DocumentNode.InnerText;
        var fallbackMatch = System.Text.RegularExpressions.Regex.Match(allText, @"([\d.]+)\s+Rezepte?");
        if (fallbackMatch.Success)
        {
            var numberStr = fallbackMatch.Groups[1].Value.Replace(".", "");
            if (int.TryParse(numberStr, out int count))
            {
                return count;
            }
        }

        return 0;
    }

    /// <summary>
    /// Returns the raw JSON-LD block for the Recipe as a string (preserving original data exactly).
    /// </summary>
    internal static string? ExtractRawRecipeJsonLd(string html)
    {
        foreach (var block in ExtractJsonLdBlocks(html))
        {
            if (IsJsonLdType(block, "Recipe"))
            {
                return block;
            }
        }
        return null;
    }

    private static bool IsJsonLdType(string json, string expectedType)
    {
        try
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(json);
            return node is System.Text.Json.Nodes.JsonObject obj
                && obj["@type"]?.GetValue<string>() == expectedType;
        }
        catch
        {
            return false;
        }
    }
}
