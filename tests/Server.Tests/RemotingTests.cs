using AI.Agents.Server.Catalog;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Agents.Server.Tests;

[TestClass]
public sealed class CatalogTests
{
    [TestMethod]
    public void AddAIAgents_RegistersConfiguredAguiAgent()
    {
        using var provider = CreateProvider(
            new Dictionary<string, string?>
            {
                ["Agents:news:Protocol"] = "AGUI",
                ["Agents:news:Endpoint"] = "http://localhost:8888",
                ["Agents:news:Description"] = "Mock news agent."
            });

        var catalog = provider.GetRequiredService<AgentCatalog>();
        var agent = provider.GetRequiredKeyedService<AIAgent>("news");

        Assert.AreEqual(1, catalog.AgentDefinitions.Count);
        Assert.AreEqual("news", catalog.AgentDefinitions[0].Name);
        Assert.AreEqual("http://localhost:8888/", catalog.AgentDefinitions[0].Endpoint.ToString());
        Assert.AreEqual("news", agent.Name);
    }

    [TestMethod]
    public void AddRemoteAgents_ThrowsForUnsupportedProtocol()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CreateProvider(
                new Dictionary<string, string?>
                {
                    ["Agents:news:Protocol"] = "MCP",
                    ["Agents:news:Endpoint"] = "http://localhost:8888"
                }));

        StringAssert.Contains(ex.Message, "unsupported protocol");
    }

    [TestMethod]
    public void AddRemoteAgents_ThrowsForRelativeEndpoint()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CreateProvider(
                new Dictionary<string, string?>
                {
                    ["Agents:news:Protocol"] = "AGUI",
                    ["Agents:news:Endpoint"] = "/news"
                }));

        StringAssert.Contains(ex.Message, "absolute HTTP or HTTPS endpoint URI");
    }

    private static ServiceProvider CreateProvider(IEnumerable<KeyValuePair<string, string?>> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddAIAgents(configuration);
        return services.BuildServiceProvider();
    }
}
