using System.Net;
using System.Net.Http.Json;
using AI.AGUI.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AI.AGUI.Server.IntegrationTests;

/// <summary>
/// Tests that verify the catalog-driven hosting model introduced by <c>AI.AGUI.Hosting</c>.
/// </summary>
[TestClass]
public sealed class CatalogHostingTests
{
    /// <summary>
    /// The default server must register exactly one application catalog entry.
    /// </summary>
    [TestMethod]
    public void Server_RegistersOneCatalogEntry()
    {
        using var factory = new AGUIServerFactory();
        var entries = factory.Services.GetServices<ApplicationCatalogEntry>().ToList();

        Assert.AreEqual(1, entries.Count, "Expected exactly one registered catalog entry.");
    }

    /// <summary>
    /// The catalog discovery endpoint must return the registered application.
    /// </summary>
    [TestMethod]
    public async Task CatalogEndpoint_ReturnsRegisteredApplications()
    {
        using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/applications");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var catalog = await response.Content.ReadFromJsonAsync<List<ApplicationCatalogEntry>>();
        Assert.IsNotNull(catalog);
        Assert.AreEqual(1, catalog.Count);
        Assert.AreEqual("agui-agent", catalog[0].Id);
    }

    /// <summary>
    /// When no MCP server configuration is present for an application, the
    /// <c>McpClientRegistry</c> must not be registered in the DI container.
    /// </summary>
    [TestMethod]
    public void Server_WithoutMcpConfig_McpRegistryIsAbsent()
    {
        using var factory = new AGUIServerFactory();

        var registry = factory.Services.GetService<AI.MCP.Client.McpClientRegistry>();

        Assert.IsNull(registry, "McpClientRegistry should not be registered when no MCP servers are configured.");
    }

    /// <summary>
    /// The AGUI endpoint for the registered application must be reachable.
    /// </summary>
    [TestMethod]
    public async Task AGUIEndpoint_ForRegisteredApplication_IsReachable()
    {
        using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var payload = new
        {
            threadId = "catalog-test-thread",
            runId = "catalog-test-run",
            messages = Array.Empty<object>(),
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await client.PostAsJsonAsync("/agents/agui-agent", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }
}
