using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer;

/// <summary>
/// Injects MCP server tools into every agent invocation at request time.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ChatClientAgent"/> clones its <see cref="Microsoft.Extensions.AI.ChatOptions"/>
/// (including any tools list) at construction time. Because <see cref="McpHostingService.StartAsync"/>
/// populates the shared <see cref="IList{AITool}"/> only after the agent singleton is already
/// resolved, the cloned list would always be empty if tools were supplied via the constructor.
/// </para>
/// <para>
/// This provider is invoked at request time (after hosted services have fully started),
/// so it always sees the fully-populated tools list and returns it as additional
/// <see cref="AIContext"/>, which the agent merges with the request's own context.
/// </para>
/// </remarks>
internal sealed class McpToolsContextProvider(IList<AITool> mcpTools) : AIContextProvider
{
#pragma warning disable MAAI001
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        AIContextProvider.InvokingContext context, CancellationToken cancellationToken = default)
#pragma warning restore MAAI001
        => mcpTools.Count == 0
            ? new(new AIContext())
            : new(new AIContext { Tools = mcpTools });
}
