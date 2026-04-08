using System.ComponentModel;

internal class FetchClient(IHttpClientFactory httpClientFactory)
{
    private IHttpClientFactory _httpClientFactory = httpClientFactory;

    [Description("Send a GET request to the specified URI and return the response body as a string.")]
    internal async Task<string> FetchAsync(
        [Description("URI to send the GET request to.")]
        string uri
    )
    {
        using var httpClient = _httpClientFactory.CreateClient("fetch");
        using var responseMessage = await httpClient.GetAsync(uri);
        responseMessage.EnsureSuccessStatusCode();
        return await responseMessage.Content.ReadAsStringAsync();
    }
}
