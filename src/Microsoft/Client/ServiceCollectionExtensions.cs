using AI.Agents.Microsoft.Configuration;
using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AI.Agents.Microsoft;

/// <summary>
/// Extension methods for registering AI clients with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
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

            return new AIProjectClient(endpoint, new DefaultAzureCredential());
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
            var innerClient = openAIChatClient.AsIChatClient();
            // Wrap with token usage tracking
            return new Client.TokenUsageTrackingChatClient(innerClient);
        });

        return services;
    }
}