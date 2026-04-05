using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
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

        var endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is not configured.");
        var deploymentName = builder.Configuration["AzureOpenAI:DeploymentName"];
        if (string.IsNullOrWhiteSpace(deploymentName))
            throw new InvalidOperationException("Azure OpenAI deployment name is not configured.");
        
        var apiKey = builder.Configuration["AzureOpenAI:ApiKey"];

        var openAIClient = string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        builder.Services.AddSingleton(openAIClient);

        var openAIChatClient = openAIClient.GetChatClient(deploymentName);
        builder.Services.AddSingleton(openAIChatClient);

        var chatClient = openAIChatClient.AsIChatClient();
        builder.Services.AddSingleton(chatClient);

        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IChatClient"/> backed by a Microsoft Foundry project endpoint
    /// using the Chat Completions API.
    /// Reads <c>Foundry:ProjectEndpoint</c> and <c>Foundry:Model</c> from configuration.
    /// Authentication uses <see cref="DefaultAzureCredential"/> (managed identity in production;
    /// <see cref="Azure.Identity.AzureCliCredential"/> locally when <c>~/.azure</c> is bind-mounted).
    /// </summary>
    /// <remarks>
    /// Uses Chat Completions (not the Responses API) because the stateless Responses API adapter
    /// in Microsoft.Agents.AI.Foundry does not correctly translate tool-result messages into the
    /// Responses API <c>function_call_output</c> schema, causing HTTP 400 invalid_payload errors
    /// after tool calls. Chat Completions handles tool-result messages correctly and
    /// <see cref="Microsoft.Agents.AI.ChatClientAgent"/> manages conversation history statelessly
    /// by re-sending the full message list on every request.
    /// </remarks>
    public static IHostApplicationBuilder AddFoundryResponsesAgentClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoint = builder.Configuration["Foundry:ProjectEndpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Foundry:ProjectEndpoint is not configured.");

        var model = builder.Configuration["Foundry:Model"];
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Foundry:Model is not configured.");

        AIProjectClient projectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
        builder.Services.AddSingleton(projectClient);

        var openAIClient = projectClient.GetProjectOpenAIClient();
        builder.Services.AddSingleton(openAIClient);

        var openAIChatClient = openAIClient.GetChatClient(model);
        builder.Services.AddSingleton(openAIChatClient);

        var chatClient = openAIChatClient.AsIChatClient();
        builder.Services.AddSingleton(chatClient);

        return builder;
    }
}
