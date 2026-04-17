using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AI.Agents.MCP;

#pragma warning disable MAAI001

/// <summary>
/// Injects MCP server tools into every agent invocation at request time.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Microsoft.Agents.AI.ChatClientAgent"/> clones its
/// <see cref="Microsoft.Extensions.AI.ChatOptions"/> (including any tools list) at construction
/// time. Because <see cref="AI.Agents.MCP.HostingService.StartAsync"/> populates clients only
/// after the agent singleton is already resolved, the cloned list would always be empty if tools
/// were supplied via the constructor.
/// </para>
/// <para>
/// This provider reads from <see cref="MCPClientRegistry.Tools"/> at request-invocation time
/// (after all hosted services have fully started), so the registry always holds a
/// fully-populated list by then.
/// </para>
/// </remarks>
public sealed class MCPClientToolsAIContextProvider(MCPClientRegistry registry) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        return new(new AIContext { Tools = [..registry.Tools.Cast<AITool>()] });
    }
}

#pragma warning restore MAAI001
