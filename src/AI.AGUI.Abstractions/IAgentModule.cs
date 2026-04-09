using Microsoft.Extensions.Hosting;

namespace AI.AGUI.Abstractions;

/// <summary>
/// Contract for pluggable agent modules.
/// Implement this interface in a separate assembly to register custom agent
/// topologies into the AGUIServer host without modifying <c>Program.cs</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Agent Skills convention:</b> If a module wants to expose agent skills it should
/// create an <see cref="Microsoft.Agents.AI.AgentSkillsProvider"/> (pointing to a
/// <c>skills/</c> directory copied next to the module assembly) and register a keyed
/// <see cref="Microsoft.Agents.AI.AIContextProvider"/> in the host service collection
/// so that the agent factory can discover and compose it.
/// </para>
/// </remarks>
public interface IAgentModule
{
    /// <summary>
    /// Registers services, configuration and agents into the host.
    /// </summary>
    void Register(IHostApplicationBuilder builder);
}
