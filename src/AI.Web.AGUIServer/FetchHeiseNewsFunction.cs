using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer;

/// <summary>
/// Provides an <see cref="AIFunction"/> that fetches the latest headlines from the
/// Heise News Atom RSS feed and returns a structured <see cref="NewsArticle"/> list.
/// </summary>
internal sealed class FetchHeiseNewsFunction
{
    internal const string DefaultBaseUrl = "https://www.heise.de/rss/heise-atom.xml";

    /// <summary>
    /// Maps topic keywords to Heise section feed paths.
    /// The fallback (empty key) is the general top-news feed.
    /// </summary>
    private static readonly Dictionary<string, string> SectionFeeds = new(StringComparer.OrdinalIgnoreCase)
    {
        { "developer", "https://www.heise.de/rss/heise-developer-atom.xml" },
        { "developer-news", "https://www.heise.de/rss/heise-developer-atom.xml" },
        { "security", "https://www.heise.de/rss/heise-security-atom.xml" },
        { "open", "https://www.heise.de/rss/heise-open-atom.xml" },
        { "ct", "https://www.heise.de/rss/ct-atom.xml" },
    };

    private static readonly XNamespace AtomNs = "http://www.w3.org/2005/Atom";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _defaultFeedUrl;

    public FetchHeiseNewsFunction(IHttpClientFactory httpClientFactory, string? baseUrl = null)
    {
        _httpClientFactory = httpClientFactory;
        _defaultFeedUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl;
    }

    /// <summary>
    /// Creates the <see cref="AIFunction"/> wrapper that agents can invoke as a tool.
    /// </summary>
    public AIFunction CreateAIFunction() =>
        AIFunctionFactory.Create(
            FetchAsync,
            "FetchHeiseNews",
            "Fetches the latest tech/IT headlines from Heise News. " +
            "Pass an optional topic filter to select a section feed " +
            "(e.g. 'developer', 'security', 'open', 'ct'). " +
            "Returns a JSON array of news articles with source, headline, topline, teaser and date.");

    /// <summary>
    /// Fetches news articles from the Heise News Atom feed.
    /// Returns an empty list on HTTP error or when the feed is empty.
    /// </summary>
    /// <param name="topic">
    /// Optional section keyword. When provided and a matching section feed exists,
    /// that section feed is used; otherwise the general feed is used.
    /// </param>
    public async Task<List<NewsArticle>> FetchAsync(string? topic = null)
    {
        var feedUrl = ResolveFeedUrl(topic);

        try
        {
            using var client = _httpClientFactory.CreateClient("heise");
            using var response = await client.GetAsync(feedUrl);
            response.EnsureSuccessStatusCode();
            var xml = await response.Content.ReadAsStringAsync();
            return ParseAtomFeed(xml);
        }
        catch
        {
            return [];
        }
    }

    private string ResolveFeedUrl(string? topic)
    {
        if (!string.IsNullOrWhiteSpace(topic)
            && SectionFeeds.TryGetValue(topic, out var sectionUrl))
        {
            return sectionUrl;
        }

        return _defaultFeedUrl;
    }

    private static List<NewsArticle> ParseAtomFeed(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return [];

        try
        {
            var doc = XDocument.Parse(xml);
            var entries = doc.Descendants(AtomNs + "entry");

            var articles = new List<NewsArticle>();
            foreach (var entry in entries)
            {
                var title = entry.Element(AtomNs + "title")?.Value ?? "";
                var summary = entry.Element(AtomNs + "summary")?.Value ?? "";
                var updated = entry.Element(AtomNs + "updated")?.Value ?? "";

                articles.Add(new NewsArticle(
                    Source: "heise",
                    Headline: title,
                    Topline: "",
                    Teaser: summary,
                    Date: updated));
            }

            return articles;
        }
        catch
        {
            return [];
        }
    }
}
