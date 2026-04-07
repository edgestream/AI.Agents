using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer;

/// <summary>
/// Discovers and activates the configured <see cref="IAgentModule"/> implementation.
/// </summary>
internal static class AgentModuleLoader
{
    /// <summary>
    /// Checks for a pre-registered <see cref="IAgentModule"/> in the service collection
    /// first; when found its <see cref="IAgentModule.Register"/> method is invoked and
    /// the config-driven path is skipped.
    /// Otherwise reads <c>config["AgentModule"]</c>.  When a value is present the named
    /// assembly is scanned for exactly one concrete <see cref="IAgentModule"/>
    /// implementation, which is then instantiated and its
    /// <see cref="IAgentModule.Register"/> method invoked.
    /// When neither source provides a module the call is a no-op (single-agent fallback).
    /// </summary>
    public static IHostApplicationBuilder LoadAgentModule(this IHostApplicationBuilder builder)
    {
        // Check for a pre-registered IAgentModule (e.g. injected by a test factory).
        var moduleDescriptor = builder.Services.LastOrDefault(
            d => d.ServiceType == typeof(IAgentModule));
        if (moduleDescriptor?.ImplementationInstance is IAgentModule preRegistered)
        {
            preRegistered.Register(builder);
            return builder;
        }

        var moduleName = builder.Configuration["AgentModule"];

        if (string.IsNullOrEmpty(moduleName))
        {
            return builder;
        }

        var assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == moduleName)
            ?? throw new InvalidOperationException(
                $"Assembly '{moduleName}' is not loaded in the current AppDomain.");

        var implementations = assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IAgentModule).IsAssignableFrom(t))
            .ToList();

        if (implementations.Count == 0)
        {
            throw new InvalidOperationException(
                $"Assembly '{moduleName}' does not contain any implementation of IAgentModule.");
        }

        if (implementations.Count > 1)
        {
            throw new InvalidOperationException(
                $"Assembly '{moduleName}' contains multiple IAgentModule implementations: " +
                string.Join(", ", implementations.Select(t => t.FullName)) +
                ". Exactly one implementation is required.");
        }

        var implementationType = implementations[0];
        object? instance;
        try
        {
            instance = Activator.CreateInstance(implementationType);
        }
        catch (MissingMethodException ex)
        {
            throw new InvalidOperationException(
                $"IAgentModule implementation '{implementationType.FullName}' must have a public parameterless constructor.", ex);
        }

        ((IAgentModule)instance!).Register(builder);

        return builder;
    }
}
