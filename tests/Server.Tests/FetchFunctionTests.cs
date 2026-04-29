using System.Net;
using System.Text.Json;
using AI.Agents.Server.Tools;
using Microsoft.Extensions.AI;

namespace AI.Agents.Server.Tests;

[TestClass]
public sealed class FetchFunctionTests
{
    [TestMethod]
    public void CreateAIFunction_HasExpectedDeclaration()
    {
        var function = FetchAIFunctionFactory.CreateAIFunction(
            new TestHttpClientFactory(new HttpClient(new StaticResponseHandler("OK"))));

        Assert.AreEqual("fetch", function.Name);
        Assert.IsNotNull(function.Description);
        Assert.IsTrue(function.Description.Contains("GET", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Fetch_ReturnsResponseBody_FromHttpClient()
    {
        var handler = new StaticResponseHandler("""{"status":"ok"}""");
        var httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        var function = FetchAIFunctionFactory.CreateAIFunction(httpClientFactory);

        var result = await function.InvokeAsync(new AIFunctionArguments
        {
            ["uri"] = "https://example.test/data"
        });

        Assert.AreEqual("""{"status":"ok"}""", ResultToString(result));
        Assert.AreEqual("fetch", httpClientFactory.Name);
        Assert.AreEqual(new Uri("https://example.test/data"), handler.RequestUri);
    }

    private static string? ResultToString(object? result)
    {
        return result switch
        {
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            JsonElement element => element.GetRawText(),
            _ => result?.ToString()
        };
    }

    private sealed class TestHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
    {
        public string? Name { get; private set; }

        public HttpClient CreateClient(string name)
        {
            Name = name;
            return httpClient;
        }
    }

    private sealed class StaticResponseHandler(string content) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
        }
    }
}
