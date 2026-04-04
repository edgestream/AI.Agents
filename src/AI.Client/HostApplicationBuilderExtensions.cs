using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostApplicationBuilder"/> to configure AI client support.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddAIClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddAzureOpenAIClient();

        return builder;
    }

    public static IHostApplicationBuilder AddAzureOpenAIClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint)) throw new InvalidOperationException("Azure OpenAI endpoint is not configured.");
        var deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"];
        if (string.IsNullOrWhiteSpace(deploymentName)) throw new InvalidOperationException("Azure OpenAI deployment name is not configured.");
        var apiKey = builder.Configuration["AzureOpenAI:ApiKey"];
        
        builder.Services.AddSingleton(_ => {
            AzureOpenAIClient client = string.IsNullOrWhiteSpace(apiKey)
                ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                : new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
            return client.GetChatClient(deploymentName).AsIChatClient();
        });

        return builder;
    }
}