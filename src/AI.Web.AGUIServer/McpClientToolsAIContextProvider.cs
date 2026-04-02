using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer;

/// <summary>
/// Injects MCP server tools into every agent invocation at request time.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ChatClientAgent"/> clones its <see cref="Microsoft.Extensions.AI.ChatOptions"/>
/// (including any tools list) at construction time. Because <see cref="McpClientHostingService.StartAsync"/>
/// populates tools only after the agent singleton is already resolved, the cloned list would
/// always be empty if tools were supplied via the constructor.
/// </para>
/// <para>
/// This provider reads from <see cref="McpClientRegistry.Tools"/> at request-invocation time
/// (after all hosted services have fully started), so the registry always holds a
/// fully-populated list by then. Each <see cref="ModelContextProtocol.Client.McpClientTool"/>
/// is an <see cref="Microsoft.Extensions.AI.AIFunction"/> and therefore a valid
/// <see cref="Microsoft.Extensions.AI.AITool"/> — the cast is type-safe.
/// The returned <see cref="AIContext"/> is merged with the request's own context by the agent.
/// </para>
/// </remarks>
internal sealed class McpClientToolsAIContextProvider(McpClientRegistry registry) : AIContextProvider
{
#pragma warning disable MAAI001
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        AIContextProvider.InvokingContext context, CancellationToken cancellationToken = default)
#pragma warning restore MAAI001
    {
        var tools = registry.Tools;
        return tools.Count == 0
            ? new(new AIContext())
            : new(new AIContext { Tools = tools.Cast<AITool>().ToList() });
    }
}
