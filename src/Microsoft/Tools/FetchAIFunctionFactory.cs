using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Agents.Microsoft.Tools;

public sealed class FetchAIFunctionFactory
{
    public static AIFunction CreateAIFunction(IServiceProvider sp)
    {
        var httpClientFactory = sp.GetKeyedService<IHttpClientFactory>("fetch")
            ?? sp.GetRequiredService<IHttpClientFactory>();
        return CreateAIFunction(httpClientFactory);
    }

    public static AIFunction CreateAIFunction(IHttpClientFactory httpClientFactory)
    {
        var fetchClient = new FetchClient(httpClientFactory);
        return CreateAIFunction(fetchClient);
    }

    internal static AIFunction CreateAIFunction(FetchClient fetchClient)
    {
        return AIFunctionFactory.Create(
            fetchClient.FetchAsync,
            name: "fetch",
            description: "Send a GET request to the specified URI and return the response body as a string.");
    }
}