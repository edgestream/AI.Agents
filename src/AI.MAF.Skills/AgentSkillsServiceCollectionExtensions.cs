#pragma warning disable MAAI001 // Agent skills types are marked experimental

using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AI.MAF.Skills;

/// <summary>
/// Marker interface for class-based agent skills registered via DI.
/// </summary>
public interface IRegisteredAgentSkill
{
    /// <summary>
    /// Adds this skill to the provided builder.
    /// </summary>
    void AddToBuilder(AgentSkillsProviderBuilder builder);
}

/// <summary>
/// Extension methods for registering agent skills and the skills provider with dependency injection.
/// </summary>
public static class AgentSkillsServiceCollectionExtensions
{
    /// <summary>
    /// Registers a class-based agent skill as a singleton.
    /// Skills are later collected by <see cref="AddAgentSkillsProvider"/> to build the provider.
    /// </summary>
    /// <typeparam name="TSkill">The skill type deriving from <see cref="AgentClassSkill{T}"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentSkill<TSkill>(this IServiceCollection services)
        where TSkill : AgentClassSkill<TSkill>, new()
    {
        services.TryAddSingleton<TSkill>();
        services.AddSingleton<IRegisteredAgentSkill>(sp => 
            new RegisteredAgentSkillWrapper<TSkill>(sp.GetRequiredService<TSkill>()));
        return services;
    }

    /// <summary>
    /// Registers a class-based agent skill instance as a singleton.
    /// </summary>
    /// <typeparam name="TSkill">The skill type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="instance">The skill instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentSkill<TSkill>(this IServiceCollection services, TSkill instance)
        where TSkill : AgentClassSkill<TSkill>
    {
        services.AddSingleton(instance);
        services.AddSingleton<IRegisteredAgentSkill>(new RegisteredAgentSkillWrapper<TSkill>(instance));
        return services;
    }

    /// <summary>
    /// Registers an <see cref="AgentSkillsProvider"/> that combines all registered <see cref="IRegisteredAgentSkill"/>
    /// instances with optional file-based skills.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for file-based skills and approval settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentSkillsProvider(
        this IServiceCollection services,
        Action<AgentSkillsProviderOptions>? configure = null)
    {
        services.AddSingleton(sp =>
        {
            var options = new AgentSkillsProviderOptions();
            configure?.Invoke(options);

            var loggerFactory = sp.GetService<ILoggerFactory>();
            var registeredSkills = sp.GetServices<IRegisteredAgentSkill>();

            var builder = new AgentSkillsProviderBuilder();

            // Add file-based skills if path is configured
            if (!string.IsNullOrEmpty(options.FileSkillsPath))
            {
                var fullPath = Path.IsPathRooted(options.FileSkillsPath)
                    ? options.FileSkillsPath
                    : Path.Combine(AppContext.BaseDirectory, options.FileSkillsPath);

                builder.UseFileSkill(fullPath);

                // File-based skills require a script runner
                if (options.FileScriptRunner is not null)
                {
                    builder.UseFileScriptRunner(options.FileScriptRunner);
                }
                else
                {
                    // Default no-op runner that returns an error message
                    builder.UseFileScriptRunner((skill, script, args, ct) =>
                        Task.FromResult<object?>("Script execution disabled: no FileScriptRunner configured."));
                }
            }

            // Add all registered class-based skills
            foreach (var skill in registeredSkills)
            {
                skill.AddToBuilder(builder);
            }

            // Configure approval if required
            if (options.RequireApproval)
            {
                builder.UseScriptApproval(true);
            }

            // Set logger factory if available
            if (loggerFactory is not null)
            {
                builder.UseLoggerFactory(loggerFactory);
            }

            return builder.Build();
        });

        return services;
    }

    private sealed class RegisteredAgentSkillWrapper<TSkill>(TSkill skill) : IRegisteredAgentSkill
        where TSkill : AgentClassSkill<TSkill>
    {
        public void AddToBuilder(AgentSkillsProviderBuilder builder) => builder.UseSkill(skill);
    }
}
#pragma warning restore MAAI001
