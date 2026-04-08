using System.Xml.Linq;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer;

/// <summary>
/// Provides an <see cref="AIFunction"/> that fetches the latest headlines from the
/// Tagesschau RSS feed (<c>https://www.tagesschau.de/infoservices/alle-meldungen-100~rss2.xml</c>)
/// and returns a structured <see cref="NewsArticle"/> list.
/// </summary>
internal sealed class FetchTagesschauNewsFunction
{
    /// <summary>
    /// Base URL of the Tagesschau website. Configurable via <c>TagesschauAgent:BaseUrl</c>.
    /// All feed URLs are derived from this root.
    /// </summary>
    internal const string DefaultBaseUrl = "https://www.tagesschau.de";

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
            "Pass an optional section filter to select a topic feed " +
            "(e.g. 'inland', 'ausland', 'wirtschaft', 'wissen'). " +
            "Returns a JSON array of news articles with source, headline, topline, teaser and date.");

    /// <summary>
    /// Fetches news articles from the Tagesschau RSS feed.
    /// Returns an empty list on HTTP error or when the feed is empty.
    /// </summary>
    /// <param name="topic">
    /// Optional Tagesschau section name (e.g. <c>inland</c>, <c>ausland</c>, <c>wirtschaft</c>).
    /// When provided the section feed <c>{base}/{topic}/index~rss2.xml</c> is used;
    /// otherwise the general all-news feed is returned.
    /// </param>
    public async Task<List<NewsArticle>> FetchAsync(string? topic = null)
    {
        var url = string.IsNullOrWhiteSpace(topic)
            ? $"{_baseUrl}/infoservices/alle-meldungen-100~rss2.xml"
            : $"{_baseUrl}/{Uri.EscapeDataString(topic)}/index~rss2.xml";

        try
        {
            using var client = _httpClientFactory.CreateClient("tagesschau");
            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var xml = await response.Content.ReadAsStringAsync();
            return ParseRssFeed(xml);
        }
        catch
        {
            return [];
        }
    }

    private static List<NewsArticle> ParseRssFeed(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return [];

        try
        {
            var doc = XDocument.Parse(xml);
            var articles = new List<NewsArticle>();

            foreach (var item in doc.Descendants("item"))
            {
                var headline = item.Element("title")?.Value ?? "";
                var teaser = item.Element("description")?.Value ?? "";
                var date = item.Element("pubDate")?.Value ?? "";
                articles.Add(new NewsArticle("tagesschau", headline, "", teaser, date));
            }

            return articles;
        }
        catch (System.Xml.XmlException)
        {
            return [];
        }
    }
}
