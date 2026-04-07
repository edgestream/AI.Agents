using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer;

/// <summary>
/// Provides an <see cref="AIFunction"/> that fetches the latest headlines from the
/// Tagesschau REST API (<c>https://www.tagesschau.de/api2u/news</c>) and returns
/// a structured <see cref="NewsArticle"/> list.
/// </summary>
internal sealed class FetchTagesschauNewsFunction
{
    internal const string DefaultBaseUrl = "https://www.tagesschau.de/api2u/news";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;

    public FetchTagesschauNewsFunction(IHttpClientFactory httpClientFactory, string? baseUrl = null)
    {
        _httpClientFactory = httpClientFactory;
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl;
    }

    /// <summary>
    /// Creates the <see cref="AIFunction"/> wrapper that agents can invoke as a tool.
    /// </summary>
    public AIFunction CreateAIFunction() =>
        AIFunctionFactory.Create(
            FetchAsync,
            "FetchTagesschauNews",
            "Fetches the latest headlines from Tagesschau. " +
            "Pass an optional topic/section filter (e.g. 'inland', 'wirtschaft', 'sport'). " +
            "Returns a JSON array of news articles with source, headline, topline, teaser and date.");

    /// <summary>
    /// Fetches news articles from the Tagesschau API.
    /// Returns an empty list on HTTP error or when the feed is empty.
    /// </summary>
    /// <param name="topic">
    /// Optional Tagesschau section filter (value of the <c>ressort</c> query parameter).
    /// When <see langword="null"/> the general top-news feed is returned.
    /// </param>
    public async Task<List<NewsArticle>> FetchAsync(string? topic = null)
    {
        var url = string.IsNullOrWhiteSpace(topic)
            ? _baseUrl
            : $"{_baseUrl}?ressort={Uri.EscapeDataString(topic)}";

        try
        {
            using var client = _httpClientFactory.CreateClient("tagesschau");
            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return ParseResponse(json);
        }
        catch
        {
            return [];
        }
    }

    private static List<NewsArticle> ParseResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("news", out var newsArray))
                return [];

            var articles = new List<NewsArticle>();
            foreach (var item in newsArray.EnumerateArray())
            {
                var headline = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var topline = item.TryGetProperty("topline", out var tl) ? tl.GetString() ?? "" : "";
                var teaser = item.TryGetProperty("firstSentence", out var fs) ? fs.GetString() ?? "" : "";
                var date = item.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "";
                articles.Add(new NewsArticle("tagesschau", headline, topline, teaser, date));
            }

            return articles;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
