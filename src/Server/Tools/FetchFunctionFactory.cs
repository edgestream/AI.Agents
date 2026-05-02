using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace AI.Agents.Server.Tools;

/// <summary>
/// Factory for creating an AIFunction that can fetch web content using an HttpClient.
/// </summary>
public sealed class FetchAIFunctionFactory
{
    private sealed class FetchTool(IHttpClientFactory httpClientFactory)
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        [Description("Send a GET request to the specified URI and return the response body text.")]
        internal async Task<string> FetchAsync(
            [Description("URI to send the GET request to.")]
            string uri)
        {
            using var httpClient = _httpClientFactory.CreateClient("fetch");
            using var responseMessage = await httpClient.GetAsync(uri);
            responseMessage.EnsureSuccessStatusCode();
            return await responseMessage.Content.ReadAsStringAsync();
        }
    }

    /// <summary>
    /// Creates an AIFunction that can fetch web content using an HttpClient from the service provider.
    /// </summary>
    /// <param name="sp">The service provider to resolve the HttpClientFactory from.</param>
    /// <returns>An AIFunction that can fetch web content.</returns>
    public static AIFunction CreateAIFunction(IServiceProvider sp)
    {
        var httpClientFactory = sp.GetKeyedService<IHttpClientFactory>("fetch")
         ?? sp.GetRequiredService<IHttpClientFactory>();
        return CreateAIFunction(httpClientFactory);
    }

    /// <summary>
    /// Creates an AIFunction that can fetch web content using the provided HttpClientFactory.
    /// </summary>
    /// <param name="httpClientFactory">The HttpClientFactory to use for creating HttpClient instances.</param>
    /// <returns>An AIFunction that can fetch web content.</returns>
    public static AIFunction CreateAIFunction(IHttpClientFactory httpClientFactory)
    {
        var fetchClient = new FetchTool(httpClientFactory);
        return CreateAIFunction(fetchClient);
    }

    private static AIFunction CreateAIFunction(FetchTool fetchClient)
    {
        return AIFunctionFactory.Create(
            fetchClient.FetchAsync,
            name: "fetch",
            description: "Send a GET request to the specified URI and return the response body text.");
    }
}
