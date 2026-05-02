using AI.Agents.Abstractions;
using AI.Agents.Microsoft.Configuration;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

#pragma warning disable OPENAI001

namespace AI.Agents.Microsoft.Client;

internal sealed class FoundryChatClientProvider : IClientProvider
{
    public bool CanCreateChatClient(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetService<IOptions<FoundrySettings>>()?.Value;
        return settings is not null && HasValidSettings(settings);
    }

    public IChatClient CreateChatClient(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<IOptions<FoundrySettings>>().Value;
        return CreateChatClient(settings);
    }

    internal static AIProjectClient CreateProjectClient(FoundrySettings settings, string sectionName = "Foundry")
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateSettings(settings, sectionName);

        return new AIProjectClient(CreateEndpoint(settings.Endpoint, sectionName), new DefaultAzureCredential());
    }

    internal static IChatClient CreateChatClient(FoundrySettings settings)
    {
        var projectClient = CreateProjectClient(settings);
        return projectClient.GetProjectOpenAIClient().GetResponsesClient().AsIChatClient();
    }

    internal static bool HasValidSettings(FoundrySettings settings)
    {
        try
        {
            ValidateSettings(settings, "Foundry");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    internal static void ValidateSettings(FoundrySettings settings, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            throw new InvalidOperationException($"{sectionName}:Endpoint is not configured.");
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