using System.Text.Json;
using System.Text.Json.Nodes;
using HtmlAgilityPack;
using MealPlanner.Abstractions;
using Schema.NET;

namespace MealPlanner.Chefkoch;

/// <summary>
/// A recipe source that extracts recipe data from Chefkoch.de using JSON-LD structured data.
/// </summary>
public class ChefkochRecipeSource : IRecipeSource
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
                // Already have first page
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
        var jsonLdBlocks = ExtractJsonLdBlocks(html);

        foreach (var block in jsonLdBlocks)
        {
            var node = JsonNode.Parse(block);
            if (node is JsonObject obj && obj["@type"]?.GetValue<string>() == "Recipe")
            {
                return MapJsonLdToRecipe(obj);
            }
        }

        throw new InvalidOperationException("No Recipe JSON-LD block found on the page.");
    }

    internal static (List<ListItem> Items, int TotalCount) ParseSearchResultsFromHtml(string html)
    {
        var jsonLdBlocks = ExtractJsonLdBlocks(html);
        JsonObject? itemListObj = null;

        foreach (var block in jsonLdBlocks)
        {
            if (string.IsNullOrWhiteSpace(block)) continue;
            var node = JsonNode.Parse(block);
            if (node is JsonObject obj && obj["@type"]?.GetValue<string>() == "ItemList")
            {
                itemListObj = obj;
                break;
            }
        }

        if (itemListObj == null)
        {
            throw new InvalidOperationException("No ItemList JSON-LD block found on the result page.");
        }

        var items = new List<ListItem>();
        var elements = itemListObj["itemListElement"]?.AsArray();
        if (elements != null)
        {
            foreach (var element in elements)
            {
                if (element is JsonObject listItemObj)
                {
                    items.Add(MapJsonLdToListItem(listItemObj));
                }
            }
        }

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

    private static ListItem MapJsonLdToListItem(JsonObject obj)
    {
        var item = new ListItem();

        var urlStr = obj["url"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(urlStr))
        {
            item.Url = new Uri(urlStr);
        }

        var name = obj["name"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(name))
        {
            item.Name = name;
        }

        var description = obj["description"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(description))
        {
            item.Description = description;
        }

        var position = obj["position"];
        if (position != null)
        {
            item.Position = position.GetValue<int>();
        }

        return item;
    }

    private static Recipe MapJsonLdToRecipe(JsonObject obj)
    {
        var recipe = new Recipe();

        var name = obj["name"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(name))
        {
            recipe.Name = name;
        }

        var description = obj["description"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(description))
        {
            recipe.Description = description;
        }

        var url = obj["mainEntityOfPage"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(url))
        {
            recipe.Url = new Uri(url);
        }

        var image = obj["image"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(image))
        {
            recipe.Image = new Uri(image);
        }

        var prepTime = obj["prepTime"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(prepTime))
        {
            recipe.PrepTime = System.Xml.XmlConvert.ToTimeSpan(prepTime);
        }

        var cookTime = obj["cookTime"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(cookTime))
        {
            recipe.CookTime = System.Xml.XmlConvert.ToTimeSpan(cookTime);
        }

        var totalTime = obj["totalTime"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(totalTime))
        {
            recipe.TotalTime = System.Xml.XmlConvert.ToTimeSpan(totalTime);
        }

        var recipeYield = obj["recipeYield"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(recipeYield))
        {
            recipe.RecipeYield = recipeYield;
        }

        var keywords = obj["keywords"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(keywords))
        {
            recipe.Keywords = keywords;
        }

        if (obj["recipeIngredient"] is JsonArray ingredients)
        {
            var ingredientList = new List<string>();
            foreach (var ing in ingredients)
            {
                var val = ing?.GetValue<string>();
                if (!string.IsNullOrEmpty(val))
                {
                    ingredientList.Add(val);
                }
            }
            recipe.RecipeIngredient = new(ingredientList);
        }

        if (obj["recipeCategory"] is JsonArray categories)
        {
            var catList = new List<string>();
            foreach (var cat in categories)
            {
                var val = cat?.GetValue<string>();
                if (!string.IsNullOrEmpty(val))
                {
                    catList.Add(val);
                }
            }
            if (catList.Count > 0)
            {
                recipe.RecipeCategory = new(catList.First());
            }
        }
        else if (obj["recipeCategory"] is JsonValue catVal)
        {
            recipe.RecipeCategory = catVal.GetValue<string>();
        }

        if (obj["recipeCuisine"] is JsonArray cuisines)
        {
            var cuisineList = new List<string>();
            foreach (var c in cuisines)
            {
                var val = c?.GetValue<string>();
                if (!string.IsNullOrEmpty(val))
                {
                    cuisineList.Add(val);
                }
            }
            if (cuisineList.Count > 0)
            {
                recipe.RecipeCuisine = new(cuisineList.First());
            }
        }
        else if (obj["recipeCuisine"] is JsonValue cuisineVal)
        {
            recipe.RecipeCuisine = cuisineVal.GetValue<string>();
        }

        if (obj["aggregateRating"] is JsonObject ratingObj)
        {
            var rating = new AggregateRating();
            var ratingValue = ratingObj["ratingValue"];
            if (ratingValue != null)
            {
                rating.RatingValue = ratingValue.GetValue<double>();
            }
            var ratingCount = ratingObj["ratingCount"];
            if (ratingCount != null)
            {
                rating.RatingCount = ratingCount.GetValue<int>();
            }
            var reviewCount = ratingObj["reviewCount"];
            if (reviewCount != null)
            {
                rating.ReviewCount = reviewCount.GetValue<int>();
            }
            recipe.AggregateRating = rating;
        }

        if (obj["datePublished"] is JsonValue datePublished)
        {
            if (DateTimeOffset.TryParse(datePublished.GetValue<string>(), out var dto))
            {
                recipe.DatePublished = dto;
            }
        }

        return recipe;
    }

    /// <summary>
    /// Returns the raw JSON-LD block for the Recipe as a string (preserving original data exactly).
    /// </summary>
    internal static string? ExtractRawRecipeJsonLd(string html)
    {
        var blocks = ExtractJsonLdBlocks(html);
        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block)) continue;
            var node = JsonNode.Parse(block);
            if (node is JsonObject obj && obj["@type"]?.GetValue<string>() == "Recipe")
            {
                return block;
            }
        }
        return null;
    }
}
