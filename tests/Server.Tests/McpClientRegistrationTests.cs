using AI.Agents.MCP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AI.Agents.Server.Tests;

[TestClass]
public sealed class McpClientRegistrationTests
{
    [TestMethod]
    public async Task AddMCPClient_RegistersInfrastructureServices()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMCPClient();

        await using var provider = services.BuildServiceProvider();

        var registry = provider.GetService<MCPClientRegistry>();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        var options = provider.GetRequiredService<IOptions<MCPClientOptions>>().Value;

        Assert.IsNotNull(registry, "MCPClientRegistry should be registered.");
        Assert.IsTrue(hostedServices.OfType<HostingService>().Any(), "HostingService should be registered as a hosted service.");
        Assert.AreEqual(0, options.Servers.Count, "Default MCP options should start empty when no configuration is present.");
    }

    [TestMethod]
    public async Task AddMCPClient_BindsServersFromConfigurationSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpServers:learn:Type"] = "http",
                ["McpServers:learn:Url"] = "https://learn.microsoft.com/api/mcp",
                ["McpServers:filesystem:Type"] = "stdio",
                ["McpServers:filesystem:Command"] = "npx",
                ["McpServers:filesystem:Args:0"] = "-y",
                ["McpServers:filesystem:Args:1"] = "@modelcontextprotocol/server-filesystem",
                ["McpServers:filesystem:Env:DEBUG"] = "1",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMCPClient();

        await using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<MCPClientOptions>>().Value;

        Assert.IsTrue(options.Servers.TryGetValue("learn", out var learn), "Expected HTTP MCP server to be bound.");
        Assert.AreEqual("http", learn.Type);
        Assert.AreEqual("https://learn.microsoft.com/api/mcp", learn.Url);

        Assert.IsTrue(options.Servers.TryGetValue("filesystem", out var filesystem), "Expected stdio MCP server to be bound.");
        Assert.AreEqual("stdio", filesystem.Type);
        Assert.AreEqual("npx", filesystem.Command);
        CollectionAssert.AreEqual(new[] { "-y", "@modelcontextprotocol/server-filesystem" }, filesystem.Args);
        Assert.AreEqual("1", filesystem.Env!["DEBUG"]);
    }

    [TestMethod]
    public async Task AddMCPClient_AppliesConfigureDelegate()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMCPClient(options =>
        {
            options.Servers["learn"] = new MCPServerOptions
            {
                Type = "http",
                Url = "https://learn.microsoft.com/api/mcp",
            };
        });

        await using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<MCPClientOptions>>().Value;

        Assert.IsTrue(options.Servers.TryGetValue("learn", out var learn), "Expected MCP server configured in code to be available.");
        Assert.AreEqual("http", learn.Type);
        Assert.AreEqual("https://learn.microsoft.com/api/mcp", learn.Url);
    }
}