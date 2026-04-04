using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable MAAI001 

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostApplicationBuilder"/> to configure AI client support.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers an <see cref="IChatClient"/> by auto-detecting the provider from configuration:
    /// uses Foundry when <c>Foundry:ProjectEndpoint</c> is present, otherwise Azure OpenAI.
    /// </summary>
    public static IHostApplicationBuilder AddAIClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return !string.IsNullOrWhiteSpace(builder.Configuration["Foundry:ProjectEndpoint"])
            ? builder.AddFoundryResponsesAgentClient()
            : builder.AddAzureOpenAIClient();
    }

    /// <summary>
    /// Registers an <see cref="IChatClient"/> backed by Azure OpenAI.
    /// Reads <c>AzureOpenAI:Endpoint</c>, <c>AzureOpenAI:DeploymentName</c>,
    /// and optionally <c>AzureOpenAI:ApiKey</c> from configuration.
    /// Falls back to <see cref="DefaultAzureCredential"/> when no API key is present.
    /// </summary>
    public static IHostApplicationBuilder AddAzureOpenAIClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is not configured.");

        var deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"];
        if (string.IsNullOrWhiteSpace(deploymentName))
            throw new InvalidOperationException("Azure OpenAI deployment name is not configured.");

        var apiKey = builder.Configuration["AzureOpenAI:ApiKey"];

        var openAIClient = string.IsNullOrEmpty(apiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        builder.Services.AddSingleton(openAIClient);

        var chatClient = openAIClient.GetChatClient(deploymentName);
        builder.Services.AddSingleton(chatClient);

        builder.Services.AddSingleton<IChatClient>(chatClient.AsIChatClient());
        
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IChatClient"/> backed by a Microsoft Foundry project endpoint
    /// using the Responses API (stateless, no server-side conversation storage).
    /// Reads <c>Foundry:ProjectEndpoint</c> and <c>Foundry:Model</c> from configuration.
    /// Authentication uses <see cref="DefaultAzureCredential"/> (managed identity / Entra ID).
    /// </summary>
    public static IHostApplicationBuilder AddFoundryResponsesAgentClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoint = builder.Configuration["Foundry:ProjectEndpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Foundry:ProjectEndpoint is not configured.");

        var model = builder.Configuration["Foundry:Model"];
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Foundry:Model is not configured.");

        var tokenCredential = new DefaultAzureCredential();

        var projectClient = new AIProjectClient(new Uri(endpoint), tokenCredential);

        builder.Services.AddSingleton<AIProjectClient>(projectClient);

        var openAIClient = projectClient.GetProjectOpenAIClient();
        builder.Services.AddSingleton(openAIClient);

        var responsesClient = openAIClient.GetProjectResponsesClientForModel(model);
        builder.Services.AddSingleton(responsesClient);

        var chatClient = responsesClient.AsIChatClientWithStoredOutputDisabled(model);
        builder.Services.AddSingleton(chatClient);
        
        return builder;
    }
}