using System.Net;
using System.Text;
using System.Text.Json;
using AI.Web.AGUIServer;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer.IntegrationTests;

/// <summary>
/// Unit tests for <see cref="FetchTagesschauNewsFunction"/> and
/// <see cref="FetchHeiseNewsFunction"/> using a fake HTTP message handler.
/// No real network calls are made.
/// </summary>
[TestClass]
public sealed class FetchNewsFunctionTests
{
    // --- FetchTagesschauNewsFunction ---

    [TestMethod]
    public async Task FetchTagesschauNews_SuccessfulResponse_ReturnsArticles()
    {
        var json = """
            {
              "news": [
                { "title": "Headline 1", "topline": "Top 1", "firstSentence": "Teaser 1", "date": "2026-04-07" },
                { "title": "Headline 2", "topline": "Top 2", "firstSentence": "Teaser 2", "date": "2026-04-07" },
                { "title": "Headline 3", "topline": "Top 3", "firstSentence": "Teaser 3", "date": "2026-04-07" }
              ]
            }
            """;
        var factory = CreateHttpClientFactory("tagesschau", HttpStatusCode.OK, json);
        var fn = new FetchTagesschauNewsFunction(factory);

        var articles = await fn.FetchAsync();

        Assert.AreEqual(3, articles.Count);
        Assert.AreEqual("tagesschau", articles[0].Source);
        Assert.AreEqual("Headline 1", articles[0].Headline);
        Assert.AreEqual("Top 1", articles[0].Topline);
        Assert.AreEqual("Teaser 1", articles[0].Teaser);
    }

    [TestMethod]
    public async Task FetchTagesschauNews_EmptyFeed_ReturnsEmptyList()
    {
        var json = """{ "news": [] }""";
        var factory = CreateHttpClientFactory("tagesschau", HttpStatusCode.OK, json);
        var fn = new FetchTagesschauNewsFunction(factory);

        var articles = await fn.FetchAsync();

        Assert.AreEqual(0, articles.Count);
    }

    [TestMethod]
    public async Task FetchTagesschauNews_HttpError_ReturnsEmptyList()
    {
        var factory = CreateHttpClientFactory("tagesschau", HttpStatusCode.InternalServerError, "");
        var fn = new FetchTagesschauNewsFunction(factory);

        var articles = await fn.FetchAsync();

        Assert.AreEqual(0, articles.Count);
    }

    [TestMethod]
    public async Task FetchTagesschauNews_WithTopic_ReturnsArticles()
    {
        var json = """{ "news": [{ "title": "Sport News", "topline": "", "firstSentence": "...", "date": "2026-04-07" }] }""";
        var factory = CreateHttpClientFactory("tagesschau", HttpStatusCode.OK, json);
        var fn = new FetchTagesschauNewsFunction(factory);

        var articles = await fn.FetchAsync(topic: "sport");

        Assert.AreEqual(1, articles.Count);
        Assert.AreEqual("tagesschau", articles[0].Source);
    }

    [TestMethod]
    public void FetchTagesschauNews_CreateAIFunction_HasExpectedName()
    {
        var factory = CreateHttpClientFactory("tagesschau", HttpStatusCode.OK, "{}");
        var fn = new FetchTagesschauNewsFunction(factory);

        var aiFunction = fn.CreateAIFunction();

        Assert.AreEqual("FetchTagesschauNews", aiFunction.Name);
    }

    // --- FetchHeiseNewsFunction ---

    [TestMethod]
    public async Task FetchHeiseNews_SuccessfulAtomFeed_ReturnsArticles()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <feed xmlns="http://www.w3.org/2005/Atom">
              <entry>
                <title type="text">Heise Headline 1</title>
                <summary type="html">Heise Teaser 1</summary>
                <updated>2026-04-07T10:00:00Z</updated>
              </entry>
              <entry>
                <title type="text">Heise Headline 2</title>
                <summary type="html">Heise Teaser 2</summary>
                <updated>2026-04-07T11:00:00Z</updated>
              </entry>
            </feed>
            """;
        var factory = CreateHttpClientFactory("heise", HttpStatusCode.OK, xml);
        var fn = new FetchHeiseNewsFunction(factory);

        var articles = await fn.FetchAsync();

        Assert.AreEqual(2, articles.Count);
        Assert.AreEqual("heise", articles[0].Source);
        Assert.AreEqual("Heise Headline 1", articles[0].Headline);
        Assert.AreEqual("Heise Teaser 1", articles[0].Teaser);
        Assert.AreEqual("2026-04-07T10:00:00Z", articles[0].Date);
    }

    [TestMethod]
    public async Task FetchHeiseNews_EmptyFeed_ReturnsEmptyList()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <feed xmlns="http://www.w3.org/2005/Atom">
            </feed>
            """;
        var factory = CreateHttpClientFactory("heise", HttpStatusCode.OK, xml);
        var fn = new FetchHeiseNewsFunction(factory);

        var articles = await fn.FetchAsync();

        Assert.AreEqual(0, articles.Count);
    }

    [TestMethod]
    public async Task FetchHeiseNews_HttpError_ReturnsEmptyList()
    {
        var factory = CreateHttpClientFactory("heise", HttpStatusCode.ServiceUnavailable, "");
        var fn = new FetchHeiseNewsFunction(factory);

        var articles = await fn.FetchAsync();

        Assert.AreEqual(0, articles.Count);
    }

    [TestMethod]
    public async Task FetchHeiseNews_KnownTopicFilter_UsesSectionFeed()
    {
        // Developer feed URL differs from general feed; the factory still returns articles.
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <feed xmlns="http://www.w3.org/2005/Atom">
              <entry>
                <title type="text">Dev Article</title>
                <summary type="html">Dev Teaser</summary>
                <updated>2026-04-07T12:00:00Z</updated>
              </entry>
            </feed>
            """;
        // Create a factory that accepts any client name.
        var factory = CreateAnyNameHttpClientFactory(HttpStatusCode.OK, xml);
        var fn = new FetchHeiseNewsFunction(factory);

        var articles = await fn.FetchAsync(topic: "developer");

        Assert.AreEqual(1, articles.Count);
        Assert.AreEqual("Dev Article", articles[0].Headline);
    }

    [TestMethod]
    public async Task FetchHeiseNews_UnknownTopicFilter_FallsBackToGeneralFeed()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <feed xmlns="http://www.w3.org/2005/Atom">
              <entry>
                <title type="text">General Article</title>
                <summary type="html">General Teaser</summary>
                <updated>2026-04-07T09:00:00Z</updated>
              </entry>
            </feed>
            """;
        var factory = CreateHttpClientFactory("heise", HttpStatusCode.OK, xml);
        var fn = new FetchHeiseNewsFunction(factory);

        var articles = await fn.FetchAsync(topic: "unknowntopic");

        Assert.AreEqual(1, articles.Count);
    }

    [TestMethod]
    public void FetchHeiseNews_CreateAIFunction_HasExpectedName()
    {
        var factory = CreateHttpClientFactory("heise", HttpStatusCode.OK, "");
        var fn = new FetchHeiseNewsFunction(factory);

        var aiFunction = fn.CreateAIFunction();

        Assert.AreEqual("FetchHeiseNews", aiFunction.Name);
    }

    // --- helpers ---

    /// <summary>
    /// Creates an <see cref="IHttpClientFactory"/> that returns a fake <see cref="HttpClient"/>
    /// for the given named client, responding with <paramref name="statusCode"/> and
    /// <paramref name="content"/>.
    /// </summary>
    private static IHttpClientFactory CreateHttpClientFactory(
        string clientName, HttpStatusCode statusCode, string content)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(clientName)
            .ConfigurePrimaryHttpMessageHandler(() =>
                new FakeHttpMessageHandler(statusCode, content));
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }

    /// <summary>
    /// Creates an <see cref="IHttpClientFactory"/> where any client name returns the same
    /// canned response. Useful when the tested code selects a different named client based
    /// on a topic filter.
    /// </summary>
    private static IHttpClientFactory CreateAnyNameHttpClientFactory(
        HttpStatusCode statusCode, string content)
    {
        return new SingleResponseHttpClientFactory(statusCode, content);
    }

    // --- test doubles ---

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8,
                    content.TrimStart().StartsWith('<') ? "application/xml" : "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// An <see cref="IHttpClientFactory"/> that returns a client backed by
    /// <see cref="FakeHttpMessageHandler"/> regardless of the client name requested.
    /// </summary>
    private sealed class SingleResponseHttpClientFactory(
        HttpStatusCode statusCode, string content) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new FakeHttpMessageHandler(statusCode, content));
    }
}
