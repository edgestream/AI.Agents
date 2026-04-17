using AI.Agents.MCP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AI.Agents.Server.IntegrationTests;

[TestClass]
public sealed class McpOAuthConfigurationTests
{
    [TestMethod]
    public async Task MCPServerOptions_BindsOAuthConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpServers:github:Type"] = "stdio",
                ["McpServers:github:Command"] = "npx",
                ["McpServers:github:Args:0"] = "-y",
                ["McpServers:github:Args:1"] = "@modelcontextprotocol/server-github",
                ["McpServers:github:Auth:Type"] = "OAuth",
                ["McpServers:github:Auth:ClientId"] = "test-client-id",
                ["McpServers:github:Auth:ClientSecret"] = "test-client-secret",
                ["McpServers:github:Auth:AuthorizationUrl"] = "https://github.com/login/oauth/authorize",
                ["McpServers:github:Auth:TokenUrl"] = "https://github.com/login/oauth/access_token",
                ["McpServers:github:Auth:Scopes:0"] = "repo",
                ["McpServers:github:Auth:Scopes:1"] = "read:user",
                ["McpServers:github:Auth:UsePkce"] = "true",
                ["McpServers:github:Auth:TokenEnvVar"] = "GITHUB_PERSONAL_ACCESS_TOKEN",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMCPClient();

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MCPClientOptions>>().Value;

        Assert.IsTrue(options.Servers.TryGetValue("github", out var github));
        Assert.AreEqual("stdio", github.Type);
        Assert.AreEqual("npx", github.Command);
        
        Assert.IsNotNull(github.Auth);
        Assert.AreEqual("OAuth", github.Auth.Type);
        Assert.AreEqual("test-client-id", github.Auth.ClientId);
        Assert.AreEqual("test-client-secret", github.Auth.ClientSecret);
        Assert.AreEqual("https://github.com/login/oauth/authorize", github.Auth.AuthorizationUrl);
        Assert.AreEqual("https://github.com/login/oauth/access_token", github.Auth.TokenUrl);
        Assert.IsNotNull(github.Auth.Scopes);
        CollectionAssert.AreEqual(new[] { "repo", "read:user" }, github.Auth.Scopes);
        Assert.IsTrue(github.Auth.UsePkce);
        Assert.AreEqual("GITHUB_PERSONAL_ACCESS_TOKEN", github.Auth.TokenEnvVar);
        Assert.IsTrue(github.Auth.IsConfigured);
    }

    [TestMethod]
    public async Task MCPServerOptions_WithoutOAuth_HasNullAuth()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpServers:filesystem:Type"] = "stdio",
                ["McpServers:filesystem:Command"] = "npx",
                ["McpServers:filesystem:Args:0"] = "-y",
                ["McpServers:filesystem:Args:1"] = "@modelcontextprotocol/server-filesystem",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMCPClient();

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MCPClientOptions>>().Value;

        Assert.IsTrue(options.Servers.TryGetValue("filesystem", out var filesystem));
        Assert.IsNull(filesystem.Auth);
    }

    [TestMethod]
    public void MCPOAuthOptions_IsConfigured_FalseWhenMissingRequired()
    {
        var auth = new MCPOAuthOptions
        {
            ClientId = "test-client-id"
            // Missing AuthorizationUrl and TokenUrl
        };

        Assert.IsFalse(auth.IsConfigured);
    }

    [TestMethod]
    public void MCPOAuthOptions_IsConfigured_TrueWhenComplete()
    {
        var auth = new MCPOAuthOptions
        {
            ClientId = "test-client-id",
            AuthorizationUrl = "https://example.com/authorize",
            TokenUrl = "https://example.com/token"
        };

        Assert.IsTrue(auth.IsConfigured);
    }

    [TestMethod]
    public async Task MCPServerOptions_HttpWithOAuth_BindsCorrectly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpServers:ms365:Type"] = "http",
                ["McpServers:ms365:Url"] = "https://graph-mcp.example.com/mcp",
                ["McpServers:ms365:Auth:Type"] = "OAuth",
                ["McpServers:ms365:Auth:ClientId"] = "ms365-client-id",
                ["McpServers:ms365:Auth:AuthorizationUrl"] = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
                ["McpServers:ms365:Auth:TokenUrl"] = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                ["McpServers:ms365:Auth:Scopes:0"] = "User.Read",
                ["McpServers:ms365:Auth:Scopes:1"] = "Calendars.Read",
                ["McpServers:ms365:Auth:Scopes:2"] = "Mail.Read",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMCPClient();

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MCPClientOptions>>().Value;

        Assert.IsTrue(options.Servers.TryGetValue("ms365", out var ms365));
        Assert.AreEqual("http", ms365.Type);
        Assert.AreEqual("https://graph-mcp.example.com/mcp", ms365.Url);
        
        Assert.IsNotNull(ms365.Auth);
        Assert.AreEqual("ms365-client-id", ms365.Auth.ClientId);
        Assert.IsNotNull(ms365.Auth.Scopes);
        Assert.AreEqual(3, ms365.Auth.Scopes.Length);
        Assert.IsTrue(ms365.Auth.IsConfigured);
    }
}
