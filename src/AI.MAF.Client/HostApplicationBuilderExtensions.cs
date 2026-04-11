using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;

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

    public static IHostApplicationBuilder AddFoundryResponsesAgentClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpoint = builder.Configuration["Foundry:ProjectEndpoint"];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            builder.Services.AddSingleton(sp =>
            {
                return new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
            });            
        }
        builder.Services.AddSingleton(sp =>
        {
            var projectClient = sp.GetRequiredService<AIProjectClient>();
            return projectClient.GetProjectOpenAIClient();
        });
        builder.Services.AddSingleton(sp =>
        {
            var projectClient = sp.GetRequiredService<AIProjectClient>();
            return projectClient.GetProjectOpenAIClient();
        });
        var model = builder.Configuration["Foundry:Model"];
        if (!string.IsNullOrWhiteSpace(model))
        {
            builder.Services.AddSingleton(sp =>
            {
                var openAIClient = sp.GetRequiredService<ProjectOpenAIClient>();
                return openAIClient.GetChatClient(model);
            });
        }
        builder.Services.AddSingleton(sp =>
        {
            var openAIChatClient = sp.GetRequiredService<ChatClient>();
            return openAIChatClient.AsIChatClient();
        });
        return builder;
    }
}
