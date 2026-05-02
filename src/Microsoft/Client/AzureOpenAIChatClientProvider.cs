using AI.Agents.Abstractions;
using AI.Agents.Microsoft.Configuration;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AI.Agents.Microsoft.Client;

internal sealed class AzureOpenAIChatClientProvider : IClientProvider
{
    public bool CanCreateChatClient(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetService<IOptions<AzureOpenAISettings>>()?.Value;
        return settings is not null && HasValidSettings(settings);
    }

    public IChatClient CreateChatClient(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<IOptions<AzureOpenAISettings>>().Value;
        return CreateChatClient(settings);
    }

    internal static AzureOpenAIClient CreateOpenAIClient(AzureOpenAISettings settings, string sectionName = "AzureOpenAI")
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateSettings(settings, sectionName);

        var endpoint = CreateEndpoint(settings.Endpoint, sectionName);
        return string.IsNullOrWhiteSpace(settings.ApiKey)
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(settings.ApiKey));
    }

    internal static ChatClient CreateChatClient(AzureOpenAIClient openAIClient, AzureOpenAISettings settings, string sectionName = "AzureOpenAI")
    {
        ArgumentNullException.ThrowIfNull(openAIClient);
        ArgumentNullException.ThrowIfNull(settings);
        ValidateSettings(settings, sectionName);

        return openAIClient.GetChatClient(settings.Model);
    }

    internal static IChatClient CreateChatClient(AzureOpenAISettings settings)
    {
        var openAIClient = CreateOpenAIClient(settings);
        return CreateChatClient(openAIClient, settings).AsIChatClient();
    }

    internal static bool HasValidSettings(AzureOpenAISettings settings)
    {
        try
        {
            ValidateSettings(settings, "AzureOpenAI");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    internal static void ValidateSettings(AzureOpenAISettings settings, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException($"{sectionName}:Endpoint is not configured.");
        }
        if (string.IsNullOrWhiteSpace(settings.Model))
        {
            throw new InvalidOperationException($"{sectionName}:Model is not configured.");
        }

        _ = CreateEndpoint(settings.Endpoint, sectionName);
    }

    private static Uri CreateEndpoint(string endpoint, string sectionName)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"{sectionName}:Endpoint must be an absolute URI.");
        }

        return uri;
    }
}