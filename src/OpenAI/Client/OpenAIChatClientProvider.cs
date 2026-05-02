using AI.Agents.Abstractions;
using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

#pragma warning disable OPENAI001 // 'OpenAI.Responses.ResponsesClient' is for evaluation purposes only

namespace AI.Agents.OpenAI.Client;

internal sealed class OpenAIChatClientProvider : IClientProvider
{
    public bool CanCreateChatClient(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetService<IOptions<OpenAISettings>>()?.Value;
        return settings is not null && HasValidSettings(settings);
    }

    public IChatClient CreateChatClient(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<IOptions<OpenAISettings>>().Value;
        return CreateChatClient(settings);
    }

    internal static OpenAIClient CreateOpenAIClient(OpenAISettings settings)
    {
        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey), CreateOpenAIClientOptions(settings));
    }

    internal static IChatClient CreateChatClient(OpenAISettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateSettings(settings, "OpenAI");

        var openAIClient = CreateOpenAIClient(settings);
        return settings.Protocol switch
        {
            OpenAIPProtocolSettings.Completions => openAIClient.GetChatClient(settings.Model).AsIChatClient(),
            OpenAIPProtocolSettings.Responses => openAIClient.GetResponsesClient().AsIChatClient(settings.Model),
            _ => throw new InvalidOperationException("Unsupported OpenAI wire API protocol.")
        };
    }

    internal static OpenAIClientOptions CreateOpenAIClientOptions(OpenAISettings settings)
    {
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            options.Endpoint = CreateEndpoint(settings.Endpoint, "OpenAI");
        }
        return options;
    }

    internal static bool HasValidSettings(OpenAISettings settings)
    {
        try
        {
            ValidateSettings(settings, "OpenAI");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    internal static void ValidateSettings(OpenAISettings settings, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException($"{sectionName}:ApiKey is not configured.");
        }
        if (string.IsNullOrWhiteSpace(settings.Model))
        {
            throw new InvalidOperationException($"{sectionName}:Model is not configured.");
        }
        if (!string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            _ = CreateEndpoint(settings.Endpoint, sectionName);
        }
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
