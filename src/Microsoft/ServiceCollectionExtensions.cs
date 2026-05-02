using AI.Agents.Abstractions;
using AI.Agents.Microsoft.Client;
using AI.Agents.Microsoft.Configuration;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

#pragma warning disable OPENAI001

namespace AI.Agents.Microsoft;

/// <summary>
/// Extension methods for registering AI clients with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Foundry chat provider from a configuration section defined as <see cref="FoundrySettings"/>.
    /// </summary>
    public static IServiceCollection AddFoundryAIProvider(this IServiceCollection services, string sectionName = "Foundry")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<FoundrySettings>().BindConfiguration(sectionName);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, FoundryChatClientProvider>());
        return services;
    }

    /// <summary>
    /// Registers the Foundry chat provider from a configuration section defined as <see cref="FoundrySettings"/>.
    /// </summary>
    public static IServiceCollection AddAIProjectClient(this IServiceCollection services, string sectionName = "Foundry")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddFoundryAIProvider(sectionName);
        return services.AddAIProjectClientCore(sectionName);
    }

    /// <summary>
    /// Registers the Azure OpenAI chat provider from a configuration section defined as <see cref="AzureOpenAISettings"/>.
    /// </summary>
    public static IServiceCollection AddAzureOpenAIProvider(this IServiceCollection services, string sectionName = "AzureOpenAI")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<AzureOpenAISettings>().BindConfiguration(sectionName);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, AzureOpenAIChatClientProvider>());
        return services;
    }

    /// <summary>
    /// Registers the Azure OpenAI chat provider from a configuration section defined as <see cref="AzureOpenAISettings"/>.
    /// </summary>
    public static IServiceCollection AddAzureOpenAIClient(this IServiceCollection services, string sectionName = "AzureOpenAI")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddAzureOpenAIProvider(sectionName);
        return services.AddAzureOpenAIClientCore(sectionName);
    }

    internal static IServiceCollection AddAIProjectClientCore(this IServiceCollection services, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<FoundrySettings>>().Value;
            return FoundryChatClientProvider.CreateProjectClient(settings, sectionName);
        });
        services.TryAddSingleton(sp =>
        {
            var projectClient = sp.GetRequiredService<AIProjectClient>();
            return projectClient.GetProjectOpenAIClient();
        });
        services.TryAddSingleton(sp =>
        {
            var projectClient = sp.GetRequiredService<AIProjectClient>();
            return projectClient.GetProjectOpenAIClient().GetResponsesClient();
        });
        return services;
    }

    internal static IServiceCollection AddAzureOpenAIClientCore(this IServiceCollection services, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
            return AzureOpenAIChatClientProvider.CreateOpenAIClient(settings, sectionName);
        });
        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
            var openAIClient = sp.GetRequiredService<AzureOpenAIClient>();
            return AzureOpenAIChatClientProvider.CreateChatClient(openAIClient, settings, sectionName);
        });
        return services;
    }
}