using System.Net;
using System.Net.Http.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AI.AGUI.Server.IntegrationTests;

/// <summary>
/// Tests that verify the direct hosting model where agents are registered via
/// <c>AddAIAgent</c> and mapped directly with <c>MapAGUI</c>.
/// </summary>
[TestClass]
public sealed class DirectHostingTests
{
    /// <summary>
    /// The default server must register the named AI agent in the DI container.
    /// </summary>
    [TestMethod]
    public void Server_RegistersNamedAIAgent()
    {
        using var factory = new AGUIServerFactory();

        var agent = factory.Services.GetKeyedService<AIAgent>("agui-agent");

        Assert.IsNotNull(agent, "Expected the 'agui-agent' keyed service to be registered.");
    }

    /// <summary>
    /// The MCPClientRegistry is always registered to support OAuth endpoint 
    /// configuration lookup, even when no MCP servers are configured.
    /// The registry will be empty but the service is available.
    /// </summary>
    [TestMethod]
    public void Server_WithoutMcpConfig_McpRegistryIsAvailable()
    {
        using var factory = new AGUIServerFactory();

        var registry = factory.Services.GetService<AI.Agents.MCP.MCPClientRegistry>();

        // Registry is now always registered to support OAuth configuration lookup
        Assert.IsNotNull(registry, "MCPClientRegistry should be registered for OAuth endpoint support.");
        Assert.AreEqual(0, registry.Tools.Count, "MCPClientRegistry should have no tools when no MCP servers are configured.");
    }

    /// <summary>
    /// The AGUI endpoint mapped at <c>/</c> must respond with a valid SSE stream.
    /// </summary>
    [TestMethod]
    public async Task AGUIEndpoint_IsReachable()
    {
        using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var payload = new
        {
            threadId = "hosting-test-thread",
            runId = "hosting-test-run",
            messages = Array.Empty<object>(),
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }
}
