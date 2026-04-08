using System.Reflection;
using AI.AGUI.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Web.AGUIServer;

/// <summary>
/// Discovers and activates a concrete <see cref="IAgentModule"/> implementation.
/// </summary>
internal static class AgentModuleLoader
{
    /// <summary>
    /// Looks for an <see cref="IAgentModule"/> first in the service collection
    /// (test-injection path), then by scanning all non-framework assemblies loaded
    /// in the current <see cref="AppDomain"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when a module was found and its
    /// <see cref="IAgentModule.Register"/> method was invoked;
    /// <see langword="false"/> when no module was found.
    /// </returns>
    public static bool LoadAgentModule(this IHostApplicationBuilder builder)
    {
        // Pre-registered path — used by test factories to inject a specific module.
        var moduleDescriptor = builder.Services.LastOrDefault(d => d.ServiceType == typeof(IAgentModule));
        if (moduleDescriptor?.ImplementationInstance is IAgentModule preRegistered)
        {
            preRegistered.Register(builder);
            return true;
        }

        // Discovery path — scan all non-framework assemblies in the current AppDomain.
        var implementations = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !IsFrameworkAssembly(a))
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException) { return []; }
            })
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IAgentModule).IsAssignableFrom(t))
            .ToList();

        if (implementations.Count == 0)
            return false;

        if (implementations.Count > 1)
            throw new InvalidOperationException(
                $"Multiple IAgentModule implementations found: " +
                string.Join(", ", implementations.Select(t => t.FullName)) +
                ". Exactly one implementation is required.");

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
        return true;
    }

    private static bool IsFrameworkAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        return name is null
            || name.StartsWith("System", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.", StringComparison.Ordinal)
            || name.StartsWith("mscorlib", StringComparison.Ordinal)
            || name.StartsWith("netstandard", StringComparison.Ordinal)
            || name.StartsWith("Anonymously Hosted", StringComparison.Ordinal);
    }
}
