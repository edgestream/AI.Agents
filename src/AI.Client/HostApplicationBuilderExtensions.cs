using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        var endpoint = builder.Configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
        var deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured.");
        var apiKey = builder.Configuration["AzureOpenAI:ApiKey"];

        builder.Services.AddSingleton<IChatClient>(_ =>
        {
            AzureOpenAIClient client = string.IsNullOrWhiteSpace(apiKey)
                ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            return client.GetChatClient(deploymentName).AsIChatClient();
        });

        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IChatClient"/> backed by a Microsoft Foundry project endpoint
    /// using the Responses Agent (direct inference) pattern. Reads <c>Foundry:ProjectEndpoint</c>
    /// and <c>Foundry:Model</c> from configuration. Authentication uses
    /// <see cref="DefaultAzureCredential"/> (managed identity / Entra ID).
    /// </summary>
    public static IHostApplicationBuilder AddFoundryResponsesAgentClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoint = builder.Configuration["Foundry:ProjectEndpoint"]
            ?? throw new InvalidOperationException("Foundry:ProjectEndpoint is not configured.");
        var model = builder.Configuration["Foundry:Model"]
            ?? throw new InvalidOperationException("Foundry:Model is not configured.");

        builder.Services.AddSingleton<AIProjectClient>(_ =>
            new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential()));

        builder.Services.AddSingleton<IChatClient>(sp =>
        {
            var projectClient = sp.GetRequiredService<AIProjectClient>();
            var agentOptions = new ChatClientAgentOptions
            {
                ChatOptions = new() { ModelId = model },
            };
            // AsAIAgent creates a server-side Responses Agent backed by the Foundry project
            // endpoint. ChatClientAgent.ChatClient exposes this as a plain IChatClient so that
            // the consuming host can wrap it with its own ChatClientAgentOptions (e.g. MCP tools).
            return projectClient.AsAIAgent(agentOptions, clientFactory: null, loggerFactory: null, services: sp).ChatClient;
        });

        return builder;
    }
}