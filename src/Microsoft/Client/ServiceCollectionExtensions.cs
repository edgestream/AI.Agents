using AI.Agents.Microsoft.Configuration;
using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using System.ClientModel;

#pragma warning disable OPENAI001

namespace AI.Agents.Microsoft;

/// <summary>
/// Extension methods for registering AI clients with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers configured AI provider and expose its chat client abstraction.
    /// </summary>
    public static IServiceCollection AddAIClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.GetSection("Foundry").Exists()) services.AddAIProjectClient();
        else if (configuration.GetSection("AzureOpenAI").Exists()) services.AddAzureOpenAIClient();
        else if (configuration.GetSection("OpenAI").Exists()) services.AddOpenAIClient();
        else throw new InvalidOperationException("AI provider not configured.");

        return services;
    }

    /// <summary>
    /// Registers <see cref="AIProjectClient"/> from a configuration section defined as <see cref="FoundrySettings"/>.
    /// </summary>
    public static IServiceCollection AddAIProjectClient(this IServiceCollection services, string sectionName = "Foundry")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<FoundrySettings>().BindConfiguration(sectionName);
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FoundrySettings>>().Value;
            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new InvalidOperationException($"{sectionName}:Endpoint is not configured.");
            }
            if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new InvalidOperationException($"{sectionName}:Endpoint must be an absolute URI.");
            }
            var projectClient = new AIProjectClient(endpoint, new DefaultAzureCredential());
            return projectClient;
        });
        services.AddSingleton(sp =>
        {
            var projectClient = sp.GetRequiredService<AIProjectClient>();
            return projectClient.GetProjectOpenAIClient();
        });
        services.AddSingleton(sp =>
        {
            var openAIClient = sp.GetRequiredService<ProjectOpenAIClient>();
            return openAIClient.GetResponsesClient();
        });
        services.AddSingleton(sp =>
        {
            var responsesClient = sp.GetRequiredService<ResponsesClient>();
            return responsesClient.AsIChatClient();
        });
        return services;
    }

    /// <summary>
    /// Registers <see cref="AzureOpenAIClient"/> and <see cref="IChatClient"/> from a configuration section
    /// defined as <see cref="AzureOpenAISettings"/>.
    /// </summary>
    public static IServiceCollection AddAzureOpenAIClient(this IServiceCollection services, string sectionName = "AzureOpenAI")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<AzureOpenAISettings>().BindConfiguration(sectionName);
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new InvalidOperationException($"{sectionName}:Endpoint is not configured.");
            }
            if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new InvalidOperationException($"{sectionName}:Endpoint must be an absolute URI.");
            }
            if (string.IsNullOrWhiteSpace(options.DeploymentName))
            {
                throw new InvalidOperationException($"{sectionName}:DeploymentName is not configured.");
            }
            return string.IsNullOrWhiteSpace(options.ApiKey)
                ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
                : new AzureOpenAIClient(endpoint, new AzureKeyCredential(options.ApiKey));
        });
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
            var openAIClient = sp.GetRequiredService<AzureOpenAIClient>();
            return openAIClient.GetChatClient(options.DeploymentName);
        });
        services.AddSingleton(sp =>
        {
            var openAIChatClient = sp.GetRequiredService<ChatClient>();
            return openAIChatClient.AsIChatClient();
        });
        return services;
    }

    /// <summary>
    /// Registers <see cref="ChatClient"/>, <see cref="ResponsesClient"/>, and <see cref="IChatClient"/>
    /// from a configuration section defined as <see cref="OpenAISettings"/>.
    /// </summary>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string sectionName = "OpenAI")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<OpenAISettings>().BindConfiguration(sectionName);
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException($"{sectionName}:ApiKey is not configured.");
            }
            if (string.IsNullOrWhiteSpace(options.ModelId))
            {
                throw new InvalidOperationException($"{sectionName}:ModelId is not configured.");
            }
            var clientOptions = new OpenAIClientOptions();
            if (!string.IsNullOrWhiteSpace(options.Endpoint))
            {
                if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint))
                {
                    throw new InvalidOperationException($"{sectionName}:Endpoint must be an absolute URI.");
                }
                clientOptions.Endpoint = endpoint;
            }
            if (!string.IsNullOrWhiteSpace(options.OrganizationId))
            {
                clientOptions.OrganizationId = options.OrganizationId;
            }
            if (!string.IsNullOrWhiteSpace(options.ProjectId))
            {
                clientOptions.ProjectId = options.ProjectId;
            }
            return clientOptions;
        });
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            return new ApiKeyCredential(options.ApiKey);
        });
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            var keyCredential = sp.GetRequiredService<ApiKeyCredential>();
            var clientOptions = sp.GetRequiredService<OpenAIClientOptions>();
            return new ChatClient(options.ModelId, keyCredential, clientOptions);
        });
        services.AddSingleton(sp =>
        {
            var keyCredential = sp.GetRequiredService<ApiKeyCredential>();
            var clientOptions = sp.GetRequiredService<OpenAIClientOptions>();
            return new ResponsesClient(keyCredential, clientOptions);
        });
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            if (options.Protocol == OpenAIPProtocolSettings.Completions)
            {
                var chatClient = sp.GetRequiredService<ChatClient>();
                return chatClient.AsIChatClient();
            }
            else if (options.Protocol == OpenAIPProtocolSettings.Responses)
            {
                var responsesClient = sp.GetRequiredService<ResponsesClient>();
                return responsesClient.AsIChatClient(options.ModelId);
            }
            else
            {
                throw new Exception("Unsupported wire API protocol.");
            }
        });
        return services;
    }
}