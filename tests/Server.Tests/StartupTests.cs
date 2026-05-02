using AI.Agents;
using AI.Agents.Microsoft;
using AI.Agents.Microsoft.Configuration;
using AI.Agents.OpenAI;
using Azure.AI.Projects;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AI.Agents.Server.Tests;

[TestClass]
public sealed class StartupTests
{
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddAIProjectClient_MissingEndpoint_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            [],
            (services, _) => services.AddAIProjectClient());

        _ = provider.GetRequiredService<AIProjectClient>();
    }

    [TestMethod]
    public void AddAIProjectClient_RegistersClient_WhenConfigurationIsValid()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["Foundry:Endpoint"] = "https://fake.foundry.endpoint/"
            },
            (services, _) => services.AddAIProjectClient());

        var client = provider.GetRequiredService<AIProjectClient>();

        Assert.IsNotNull(client);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddAzureOpenAIClient_MissingEndpoint_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["AzureOpenAI:Model"] = "some-deployment"
            },
            (services, _) => services.AddAIClient());

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddAzureOpenAIClient_MissingModel_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://fake.openai.azure.com/"
            },
            (services, _) => services.AddAIClient());

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddOpenAIClient_MissingApiKey_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["OpenAI:Model"] = "gpt-5.3-mini"
            },
            (services, configuration) =>
            {
                services.AddAIClient();
            });

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddOpenAIClient_MissingModel_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-api-key"
            },
            (services, configuration) =>
            {
                services.AddAIClient();
            });

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    public void AddOpenAIClient_RegistersResponsesClientAndIChatClient_WhenConfigurationIsValid()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-api-key",
                ["OpenAI:Model"] = "openai/gpt-5-mini",
                ["OpenAI:Endpoint"] = "https://openrouter.ai/api/v1/"
            },
            (services, configuration) =>
            {
                services.AddOpenAIClient();
                services.AddAIClient();
            });

        var chatClient = provider.GetRequiredService<ChatClient>();
        var responsesClient = provider.GetRequiredService<ResponsesClient>();
        var aiChatClient = provider.GetRequiredService<IChatClient>();

        Assert.IsNotNull(chatClient);
        Assert.IsNotNull(responsesClient);
        Assert.IsNotNull(aiChatClient);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddConfiguredAIClient_NoProvidersConfigured_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            [],
            (services, configuration) =>
            {
                services.AddAIClient();
            });

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    public void AddAIClient_UsesOpenAIProvider_WhenOpenAIIsConfigured()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-api-key",
                ["OpenAI:Model"] = "gpt-5.3-mini"
            },
            (services, configuration) =>
            {
                services.AddAIClient();
            });

        var chatClient = provider.GetRequiredService<IChatClient>();

        Assert.IsNotNull(chatClient);
    }

    [TestMethod]
    public void AddAIClient_UsesCodexProvider_WhenCodexIsConfigured()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["Codex:ApiKey"] = "test-api-key",
                ["Codex:AccountID"] = "test-account",
                ["Codex:Model"] = "gpt-5.4"
            },
            (services, configuration) =>
            {
                services.AddAIClient();
            });

        var chatClient = provider.GetRequiredService<IChatClient>();

        Assert.IsNotNull(chatClient);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddAIClient_CodexMissingAccountId_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["Codex:ApiKey"] = "test-api-key",
                ["Codex:Model"] = "gpt-5.4"
            },
            (services, configuration) =>
            {
                services.AddAIClient();
            });

        _ = provider.GetRequiredService<IChatClient>();
    }

    private static ServiceProvider CreateProvider(
        IEnumerable<KeyValuePair<string, string?>> settings,
        Action<IServiceCollection, IConfiguration> configureServices)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        configureServices(services, configuration);

        return services.BuildServiceProvider();
    }
}
