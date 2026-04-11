using AI.AGUI.Hosting;
using AI.MCP.Client;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for <see cref="IHostApplicationBuilder"/> that provide the explicit
/// AGUI hosting surface.  Host applications call <see cref="AddAGUIApplication"/> for each
/// application they want to expose and then call <see cref="AspNetCore.Builder.WebApplicationExtensions.MapAGUI"/>
/// on the built <see cref="WebApplication"/>.
/// </summary>
public static class AGUIHostingBuilderExtensions
{
    /// <summary>
    /// Registers an AGUI application with the given <paramref name="id"/> and
    /// <paramref name="displayName"/>, and wires up its agent via <paramref name="agentFactory"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The application is exposed at the route <c>/agents/{id}</c>.
    /// </para>
    /// <para>
    /// MCP infrastructure is activated only when the configuration section
    /// <c>Applications:{id}:McpServers</c> contains at least one server entry.
    /// The per-application MCP servers are flattened into the global
    /// <c>McpServers</c> configuration key expected by <c>AI.MCP.Client</c>.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="id">Stable application identifier used as the agent key and URL segment.</param>
    /// <param name="displayName">Human-readable name shown in the frontend selector.</param>
    /// <param name="agentFactory">Factory that creates the <see cref="AIAgent"/> for this application.</param>
    /// <returns>The <paramref name="builder"/> for method chaining.</returns>
    public static IHostApplicationBuilder AddAGUIApplication(
        this IHostApplicationBuilder builder,
        string id,
        string displayName,
        Func<IServiceProvider, string, AIAgent> agentFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(agentFactory);

        var route = $"/agents/{id}";

        // Register the catalog entry so MapAGUI can discover it.
        builder.Services.AddSingleton(new ApplicationCatalogEntry(id, displayName, route));

        // Register the agent under its stable id.
        builder.Services.AddAIAgent(id, agentFactory);

        // Register AGUI serialisation / pipeline services (idempotent).
        builder.Services.AddAGUI();

        // Activate MCP infrastructure only when this application has servers configured.
        ActivateMcpIfConfigured(builder, id);

        return builder;
    }

    private static void ActivateMcpIfConfigured(IHostApplicationBuilder builder, string id)
    {
        var mcpSection = builder.Configuration.GetSection($"Applications:{id}:McpServers");
        var serverKeys = mcpSection.GetChildren().ToList();
        if (serverKeys.Count == 0)
            return;

        // Flatten app-specific MCP server config into the global McpServers section
        // that AI.MCP.Client's HostingService reads from.
        var inMemory = new Dictionary<string, string?>();
        foreach (var server in serverKeys)
        {
            foreach (var entry in server.GetChildren())
                inMemory[$"McpServers:{server.Key}:{entry.Key}"] = entry.Value;
        }
        ((IConfigurationBuilder)builder.Configuration).Add(
            new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = inMemory
            });

        // Register MCP infrastructure once across all applications.
        if (!builder.Services.Any(d => d.ServiceType == typeof(McpClientRegistry)))
            builder.AddMCPClient();
    }
}
