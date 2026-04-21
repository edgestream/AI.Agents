using AI.Agents.Microsoft;
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
                ["AzureOpenAI:DeploymentName"] = "some-deployment"
            },
            (services, _) => services.AddAzureOpenAIClient());

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddAzureOpenAIClient_MissingDeploymentName_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://fake.openai.azure.com/"
            },
            (services, _) => services.AddAzureOpenAIClient());

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void AddOpenAIClient_MissingApiKey_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["OpenAI:ModelId"] = "gpt-5.3-mini"
            },
            (services, _) => services.AddOpenAIClient());

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void AddOpenAIClient_MissingModelId_ThrowsInvalidOperationException()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-api-key"
            },
            (services, _) => services.AddOpenAIClient());

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    public void AddOpenAIClient_RegistersResponsesClientAndIChatClient_WhenConfigurationIsValid()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-api-key",
                ["OpenAI:ModelId"] = "openai/gpt-5-mini",
                ["OpenAI:Endpoint"] = "https://openrouter.ai/api/v1/"
            },
            (services, _) => services.AddOpenAIClient());

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
            (services, configuration) => services.AddAIClient(configuration));

        _ = provider.GetRequiredService<IChatClient>();
    }

    [TestMethod]
    public void AddAIClient_UsesOpenAIProvider_WhenOpenAIIsConfigured()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-api-key",
                ["OpenAI:ModelId"] = "gpt-5.3-mini"
            },
            (services, configuration) => services.AddAIClient(configuration));

        var chatClient = provider.GetRequiredService<IChatClient>();
        var responsesClient = provider.GetRequiredService<ResponsesClient>();

        Assert.IsNotNull(chatClient);
        Assert.IsNotNull(responsesClient);
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
