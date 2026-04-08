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
    /// Maps topic keywords to official Heise section feed URLs.
    /// Source: https://www.heise.de/news-extern/news.html
    /// The fallback is the general heise online News feed.
    /// </summary>
    private static readonly Dictionary<string, string> SectionFeeds = new(StringComparer.OrdinalIgnoreCase)
    {
        // heise online sections
        { "it",             "https://www.heise.de/rss/heise-Rubrik-IT-atom.xml" },
        { "wissen",         "https://www.heise.de/rss/heise-Rubrik-Wissen-atom.xml" },
        { "mobiles",        "https://www.heise.de/rss/heise-Rubrik-Mobiles-atom.xml" },
        { "entertainment",  "https://www.heise.de/rss/heise-Rubrik-Entertainment-atom.xml" },
        { "netzpolitik",    "https://www.heise.de/rss/heise-Rubrik-Netzpolitik-atom.xml" },
        { "wirtschaft",     "https://www.heise.de/rss/heise-Rubrik-Wirtschaft-atom.xml" },
        { "journal",        "https://www.heise.de/rss/heise-Rubrik-Journal-atom.xml" },
        { "top",            "https://www.heise.de/rss/heise-top-atom.xml" },
        { "top-news",       "https://www.heise.de/rss/heise-top-atom.xml" },
        // sub-brands
        { "developer",      "https://www.heise.de/developer/feed.xml" },
        { "developer-news", "https://www.heise.de/developer/feed.xml" },
        { "security",       "https://www.heise.de/security/feed.xml" },
        { "ct",             "https://www.heise.de/ct/feed.xml" },
        { "plus",           "https://www.heise.de/rss/heiseplus-atom.xml" },
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
            "Pass an optional topic filter to select a section feed. " +
            "Valid topics: it, wissen, mobiles, entertainment, netzpolitik, wirtschaft, journal, top, developer, security, ct, plus. " +
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
