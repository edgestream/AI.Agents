using System.ClientModel;
using AI.Agents.Abstractions;
using AI.Agents.OpenAI.Client;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenAI;

#pragma warning disable OPENAI001 // 'OpenAI.Responses.ResponsesClient' is for evaluation purposes only

namespace AI.Agents.OpenAI;

/// <summary>
/// Extension methods for registering the AI providers with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Codex chat client from a configuration section defined as <see cref="CodexSettings"/>.
    /// </summary>
    public static IServiceCollection AddCodexClient(this IServiceCollection services, string sectionName = "Codex")
    {
        services.AddCodexProvider(sectionName);
        services.TryAddSingleton<IChatClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CodexSettings>>().Value;
            return CodexChatClientProvider.CreateChatClient(settings);
        });
        return services;
    }

    /// <summary>
    /// Registers the Codex chat client from an explicit settings object.
    /// </summary>
    public static IServiceCollection AddCodexClient(this IServiceCollection services, CodexSettings settings)
    {
        services.AddCodexProvider(settings);
        services.TryAddSingleton<IChatClient>(_ => CodexChatClientProvider.CreateChatClient(settings));
        return services;
    }

    /// <summary>
    /// Registers the Codex chat provider from a configuration section defined as <see cref="CodexSettings"/>.
    /// </summary>
    public static IServiceCollection AddCodexProvider(this IServiceCollection services, string sectionName = "Codex")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<CodexSettings>().BindConfiguration(sectionName);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, CodexChatClientProvider>());
        return services;
    }

    /// <summary>
    /// Registers the Codex chat provider from an explicit settings object.
    /// </summary>
    public static IServiceCollection AddCodexProvider(this IServiceCollection services, CodexSettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        services.AddSingleton(Options.Create(settings));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, CodexChatClientProvider>());
        return services;
    }

    /// <summary>
    /// Registers the OpenAI chat client using an already configured
    /// <see cref="IOptions{TOptions}"/> registration for <see cref="OpenAISettings"/>.
    /// </summary>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services)
    {
        services.AddOpenAIProvider();
        services.AddOpenAIClientCore();
        services.TryAddSingleton<IChatClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            return OpenAIChatClientProvider.CreateChatClient(settings);
        });
        return services;
    }

    /// <summary>
    /// Registers the OpenAI chat client from a configuration section defined as <see cref="OpenAISettings"/>.
    /// </summary>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string sectionName)
    {
        services.AddOpenAIProvider(sectionName);
        services.AddOpenAIClientCore();
        services.TryAddSingleton<IChatClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            return OpenAIChatClientProvider.CreateChatClient(settings);
        });
        return services;
    }

    /// <summary>
    /// Registers the OpenAI chat client from an explicit settings object.
    /// </summary>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, OpenAISettings settings)
    {
        services.AddOpenAIProvider(settings);
        services.AddOpenAIClientCore();
        services.TryAddSingleton<IChatClient>(_ => OpenAIChatClientProvider.CreateChatClient(settings));
        return services;
    }

    /// <summary>
    /// Registers the OpenAI chat provider from a configuration section defined as <see cref="OpenAISettings"/>.
    /// </summary>
    public static IServiceCollection AddOpenAIProvider(this IServiceCollection services, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<OpenAISettings>().BindConfiguration(sectionName);
        return services.AddOpenAIProvider();
    }

    /// <summary>
    /// Registers the OpenAI chat provider from an explicit settings object.
    /// </summary>
    public static IServiceCollection AddOpenAIProvider(this IServiceCollection services, OpenAISettings settings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);

        services.AddSingleton(Options.Create(settings));
        return services.AddOpenAIProvider();
    }

    /// <summary>
    /// Registers the OpenAI chat provider using an already configured
    /// <see cref="IOptions{TOptions}"/> registration for <see cref="OpenAISettings"/>.
    /// </summary>
    public static IServiceCollection AddOpenAIProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<OpenAISettings>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, OpenAIChatClientProvider>());
        return services;
    }

    internal static IServiceCollection AddOpenAIClientCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            OpenAIChatClientProvider.ValidateSettings(settings, "OpenAI");
            return new ApiKeyCredential(settings.ApiKey);
        });
        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            return OpenAIChatClientProvider.CreateOpenAIClientOptions(settings);
        });
        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            return OpenAIChatClientProvider.CreateOpenAIClient(settings);
        });
        services.TryAddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<OpenAISettings>>().Value;
            var openAIClient = sp.GetRequiredService<OpenAIClient>();
            return openAIClient.GetChatClient(settings.Model);
        });
        services.TryAddSingleton(sp =>
        {
            var openAIClient = sp.GetRequiredService<OpenAIClient>();
            return openAIClient.GetResponsesClient();
        });
        return services;
    }
}
