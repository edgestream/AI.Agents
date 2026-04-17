using AI.Agents.Microsoft;
using Azure.AI.Projects;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            services => services.AddAIProjectClient());

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
            services => services.AddAIProjectClient());

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
            services => services.AddAzureOpenAIClient());

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
            services => services.AddAzureOpenAIClient());

        _ = provider.GetRequiredService<IChatClient>();
    }

    private static ServiceProvider CreateProvider(
        IEnumerable<KeyValuePair<string, string?>> settings,
        Action<IServiceCollection> configureServices)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        configureServices(services);

        return services.BuildServiceProvider();
    }
}
