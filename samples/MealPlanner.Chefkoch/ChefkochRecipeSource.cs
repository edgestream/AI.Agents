using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using MealPlanner.Abstractions;

namespace MealPlanner.Chefkoch;

/// <summary>
/// <see cref="IRecipeSource"/> implementation that extracts recipe data from
/// <c>chefkoch.de</c> using JSON-LD structured data embedded in recipe pages.
/// </summary>
public sealed partial class ChefkochRecipeSource : IRecipeSource
{
    private const string SearchBaseUrl = "https://api.chefkoch.de/v2/search-gateway/recipes?query=";
    private const string RecipeBaseUrl = "https://www.chefkoch.de/rezepte/";

    private readonly HttpClient _httpClient;

    public ChefkochRecipeSource(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var url = $"{SearchBaseUrl}{Uri.EscapeDataString(query)}&limit=10";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseSearchResponse(json);
    }

    /// <inheritdoc />
    public async Task<Recipe?> GetRecipeAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var url = $"{RecipeBaseUrl}{id}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseRecipePage(html, id);
    }

    /// <summary>
    /// Parses the Chefkoch search API JSON response into <see cref="RecipeSearchResult"/> items.
    /// </summary>
    internal static IReadOnlyList<RecipeSearchResult> ParseSearchResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var results = new List<RecipeSearchResult>();

        if (!doc.RootElement.TryGetProperty("results", out var resultsElement))
            return results;

        foreach (var item in resultsElement.EnumerateArray())
        {
            if (!item.TryGetProperty("recipe", out var recipe))
                continue;

            var id = recipe.TryGetProperty("id", out var idProp) ? idProp.ToString() : null;
            var title = recipe.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
            if (id is null || title is null)
                continue;

            var image = recipe.TryGetProperty("previewImageUrlTemplate", out var imgProp)
                ? imgProp.GetString()?.Replace("<format>", "crop-360x240")
                : null;

            double? rating = recipe.TryGetProperty("rating", out var ratingProp) && ratingProp.TryGetProperty("rating", out var rVal)
                ? rVal.GetDouble()
                : null;

            int? reviewCount = recipe.TryGetProperty("rating", out var rcProp) && rcProp.TryGetProperty("numVotes", out var nvVal)
                ? nvVal.GetInt32()
                : null;

            results.Add(new RecipeSearchResult
            {
                Id = id,
                Title = title,
                Image = image,
                Rating = rating,
                ReviewCount = reviewCount,
                SourceUrl = $"{RecipeBaseUrl}{id}",
            });
        }

        return results;
    }

    /// <summary>
    /// Parses a Chefkoch recipe HTML page by extracting the JSON-LD <c>Recipe</c> block.
    /// </summary>
    internal static Recipe? ParseRecipePage(string html, string id)
    {
        var jsonLd = ExtractJsonLd(html);
        if (jsonLd is null)
            return null;

        return ParseJsonLd(jsonLd, id);
    }

    /// <summary>
    /// Extracts the first JSON-LD block with <c>@type</c> = <c>Recipe</c> from the HTML.
    /// </summary>
    internal static string? ExtractJsonLd(string html)
    {
        var matches = JsonLdRegex().Matches(html);
        foreach (Match match in matches)
        {
            var content = WebUtility.HtmlDecode(match.Groups[1].Value);
            if (content.Contains("\"Recipe\"") || content.Contains("\"@type\":\"Recipe\"") || content.Contains("\"@type\": \"Recipe\""))
                return content;
        }
        return null;
    }

    /// <summary>
    /// Parses a JSON-LD Recipe object into a <see cref="Recipe"/> DTO.
    /// </summary>
    internal static Recipe? ParseJsonLd(string jsonLd, string id)
    {
        using var doc = JsonDocument.Parse(jsonLd);
        var root = doc.RootElement;

        // Handle JSON-LD arrays (some pages wrap in [...]).
        var recipe = root.ValueKind == JsonValueKind.Array
            ? FindRecipeInArray(root)
            : root;

        if (recipe.ValueKind == JsonValueKind.Undefined)
            return null;

        var title = recipe.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        if (title is null) return null;

        var image = GetFirstImage(recipe);
        var rating = recipe.TryGetProperty("aggregateRating", out var ar) && ar.TryGetProperty("ratingValue", out var rv)
            ? ParseDouble(rv) : null;
        var reviewCount = recipe.TryGetProperty("aggregateRating", out var ar2) && ar2.TryGetProperty("ratingCount", out var rc)
            ? ParseInt(rc) : null;

        var prepTime = recipe.TryGetProperty("prepTime", out var pt) ? ParseIso8601Duration(pt.GetString()) : null;
        var cookTime = recipe.TryGetProperty("cookTime", out var ct) ? ParseIso8601Duration(ct.GetString()) : null;
        var totalTime = recipe.TryGetProperty("totalTime", out var tt) ? ParseIso8601Duration(tt.GetString()) : null;

        // Fallback: if only totalTime is set, use it as cookTime.
        cookTime ??= totalTime;

        var servings = recipe.TryGetProperty("recipeYield", out var ry)
            ? ParseServings(ry)
            : null;

        var ingredients = recipe.TryGetProperty("recipeIngredient", out var ri)
            ? ri.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
            : [];

        var instructions = ParseInstructions(recipe);

        var nutrition = recipe.TryGetProperty("nutrition", out var nu) ? ParseNutrition(nu) : null;

        var sourceUrl = recipe.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : $"{RecipeBaseUrl}{id}";

        return new Recipe
        {
            Id = id,
            Title = title,
            Image = image,
            Rating = rating,
            ReviewCount = reviewCount,
            PrepTime = prepTime,
            CookTime = cookTime,
            Servings = servings,
            Ingredients = ingredients,
            Instructions = instructions,
            SourceUrl = sourceUrl,
            Nutrition = nutrition,
        };
    }

    private static JsonElement FindRecipeInArray(JsonElement array)
    {
        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty("@type", out var typeProp) && typeProp.GetString() == "Recipe")
                return item;
        }
        return default;
    }

    private static string? GetFirstImage(JsonElement recipe)
    {
        if (!recipe.TryGetProperty("image", out var imgProp))
            return null;
        if (imgProp.ValueKind == JsonValueKind.String)
            return imgProp.GetString();
        if (imgProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in imgProp.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    return item.GetString();
                if (item.TryGetProperty("url", out var url))
                    return url.GetString();
            }
        }
        if (imgProp.TryGetProperty("url", out var urlProp))
            return urlProp.GetString();
        return null;
    }

    private static List<string> ParseInstructions(JsonElement recipe)
    {
        if (!recipe.TryGetProperty("recipeInstructions", out var ri))
            return [];

        var instructions = new List<string>();

        if (ri.ValueKind == JsonValueKind.String)
        {
            // Some sites put all instructions in a single string.
            var text = ri.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                instructions.Add(text);
            return instructions;
        }

        if (ri.ValueKind == JsonValueKind.Array)
        {
            int step = 1;
            foreach (var item in ri.EnumerateArray())
            {
                string? text = null;
                if (item.ValueKind == JsonValueKind.String)
                {
                    text = item.GetString();
                }
                else if (item.TryGetProperty("text", out var textProp))
                {
                    text = textProp.GetString();
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    instructions.Add($"{step}. {text}");
                    step++;
                }
            }
        }

        return instructions;
    }

    private static NutritionInfo? ParseNutrition(JsonElement nu)
    {
        return new NutritionInfo
        {
            Calories = nu.TryGetProperty("calories", out var cal) ? ParseDoubleFromString(cal.GetString()) : null,
            ProteinGrams = nu.TryGetProperty("proteinContent", out var p) ? ParseDoubleFromString(p.GetString()) : null,
            FatGrams = nu.TryGetProperty("fatContent", out var f) ? ParseDoubleFromString(f.GetString()) : null,
            CarbGrams = nu.TryGetProperty("carbohydrateContent", out var c) ? ParseDoubleFromString(c.GetString()) : null,
            FiberGrams = nu.TryGetProperty("fiberContent", out var fi) ? ParseDoubleFromString(fi.GetString()) : null,
        };
    }

    private static double? ParseDoubleFromString(string? value)
    {
        if (value is null) return null;
        // Strip non-numeric suffixes like "kcal", "g".
        var numeric = NumericPrefixRegex().Match(value);
        return numeric.Success && double.TryParse(numeric.Value, System.Globalization.CultureInfo.InvariantCulture, out var d)
            ? d
            : null;
    }

    private static double? ParseDouble(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number)
            return el.GetDouble();
        if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;
        return null;
    }

    private static int? ParseInt(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number)
            return el.GetInt32();
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var i))
            return i;
        return null;
    }

    private static int? ParseServings(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number)
            return el.GetInt32();
        if (el.ValueKind == JsonValueKind.String)
        {
            var match = LeadingDigitsRegex().Match(el.GetString() ?? "");
            if (match.Success && int.TryParse(match.Value, out var s))
                return s;
        }
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                var result = ParseServings(item);
                if (result.HasValue) return result;
            }
        }
        return null;
    }

    /// <summary>
    /// Parses a subset of ISO 8601 duration (e.g. PT15M, PT1H30M).
    /// </summary>
    internal static TimeSpan? ParseIso8601Duration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("PT", StringComparison.OrdinalIgnoreCase))
            return null;

        var match = Iso8601DurationRegex().Match(value);
        if (!match.Success)
            return null;

        var hours = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value) : 0;
        var minutes = match.Groups["m"].Success ? int.Parse(match.Groups["m"].Value) : 0;
        var seconds = match.Groups["s"].Success ? int.Parse(match.Groups["s"].Value) : 0;

        return new TimeSpan(hours, minutes, seconds);
    }

    [GeneratedRegex(@"<script[^>]+type=[""']application/ld\+json[""'][^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex JsonLdRegex();

    [GeneratedRegex(@"^[\d.]+")]
    private static partial Regex NumericPrefixRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex LeadingDigitsRegex();

    [GeneratedRegex(@"^PT(?:(?<h>\d+)H)?(?:(?<m>\d+)M)?(?:(?<s>\d+)S)?$", RegexOptions.IgnoreCase)]
    private static partial Regex Iso8601DurationRegex();
}
