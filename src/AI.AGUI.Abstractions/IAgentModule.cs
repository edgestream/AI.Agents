using Microsoft.Extensions.Hosting;

namespace AI.AGUI.Abstractions;

/// <summary>
/// Contract for pluggable agent modules.
/// Implement this interface in a separate assembly to register custom agent
/// topologies into the AGUIServer host without modifying <c>Program.cs</c>.
/// </summary>
public interface IAgentModule
{
    /// <summary>
    /// Registers services, configuration and agents into the host.
    /// </summary>
    void Register(IHostApplicationBuilder builder);
}
