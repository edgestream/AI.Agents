#pragma warning disable MAAI001 // Agent skills types are marked experimental

using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
public static class HostApplicationBuilderExtensions
{
    private const string DefaultSkillsPath = "skills";
    private const string SkillsPathConfigKey = "Skills:Path";

    /// <summary>
    /// Registers a class-based agent skill as a singleton and automatically registers the skills provider
    /// if not already registered.
    /// </summary>
    /// <typeparam name="TSkill">The skill type deriving from <see cref="AgentClassSkill{T}"/>.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddAIAgentSkill<TSkill>(this IHostApplicationBuilder builder)
        where TSkill : AgentClassSkill<TSkill>, new()
    {
        builder.Services.TryAddSingleton<TSkill>();
        builder.Services.AddSingleton<IRegisteredAgentSkill>(sp => 
            new RegisteredAgentSkillWrapper<TSkill>(sp.GetRequiredService<TSkill>()));
        
        // Auto-register the skills provider if not already registered
        EnsureSkillsProviderRegistered(builder);
        
        return builder;
    }

    /// <summary>
    /// Registers a class-based agent skill instance as a singleton and automatically registers the skills provider
    /// if not already registered.
    /// </summary>
    /// <typeparam name="TSkill">The skill type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="instance">The skill instance.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddAIAgentSkill<TSkill>(this IHostApplicationBuilder builder, TSkill instance)
        where TSkill : AgentClassSkill<TSkill>
    {
        builder.Services.AddSingleton(instance);
        builder.Services.AddSingleton<IRegisteredAgentSkill>(new RegisteredAgentSkillWrapper<TSkill>(instance));
        
        // Auto-register the skills provider if not already registered
        EnsureSkillsProviderRegistered(builder);
        
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="AgentSkillsProvider"/> using the path from configuration ("Skills:Path") 
    /// or "skills" as default. This is called automatically when skills are added via <see cref="AddAIAgentSkill{TSkill}(IHostApplicationBuilder)"/>.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddAIAgentSkillsProvider(this IHostApplicationBuilder builder)
    {
        var path = builder.Configuration[SkillsPathConfigKey] ?? DefaultSkillsPath;
        return builder.AddAIAgentSkillsProvider(path);
    }

    /// <summary>
    /// Registers an <see cref="AgentSkillsProvider"/> with the specified file skills path.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="path">The path to file-based skills directory.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddAIAgentSkillsProvider(this IHostApplicationBuilder builder, string path)
    {
        return builder.AddAIAgentSkillsProvider(options => options.FileSkillsPath = path);
    }

    /// <summary>
    /// Registers an <see cref="AgentSkillsProvider"/> that combines all registered <see cref="IRegisteredAgentSkill"/>
    /// instances with optional file-based skills.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional configuration for file-based skills and approval settings.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddAIAgentSkillsProvider(
        this IHostApplicationBuilder builder,
        Action<AgentSkillsProviderOptions>? configure)
    {
        builder.Services.AddSingleton(sp =>
        {
            var options = new AgentSkillsProviderOptions();
            configure?.Invoke(options);

            var loggerFactory = sp.GetService<ILoggerFactory>();
            var registeredSkills = sp.GetServices<IRegisteredAgentSkill>();

            var providerBuilder = new AgentSkillsProviderBuilder();

            // Add file-based skills if path is configured
            if (!string.IsNullOrEmpty(options.FileSkillsPath))
            {
                var fullPath = Path.IsPathRooted(options.FileSkillsPath)
                    ? options.FileSkillsPath
                    : Path.Combine(AppContext.BaseDirectory, options.FileSkillsPath);

                providerBuilder.UseFileSkill(fullPath);

                // File-based skills require a script runner
                if (options.FileScriptRunner is not null)
                {
                    providerBuilder.UseFileScriptRunner(options.FileScriptRunner);
                }
                else
                {
                    // Default no-op runner that returns an error message
                    providerBuilder.UseFileScriptRunner((skill, script, args, ct) =>
                        Task.FromResult<object?>("Script execution disabled: no FileScriptRunner configured."));
                }
            }

            // Add all registered class-based skills
            foreach (var skill in registeredSkills)
            {
                skill.AddToBuilder(providerBuilder);
            }

            // Configure approval if required
            if (options.RequireApproval)
            {
                providerBuilder.UseScriptApproval(true);
            }

            // Set logger factory if available
            if (loggerFactory is not null)
            {
                providerBuilder.UseLoggerFactory(loggerFactory);
            }

            return providerBuilder.Build();
        });

        return builder;
    }

    private static void EnsureSkillsProviderRegistered(IHostApplicationBuilder builder)
    {
        // Use a marker service to track if provider factory was already registered
        if (builder.Services.Any(d => d.ServiceType == typeof(AgentSkillsProviderMarker)))
        {
            return;
        }

        builder.Services.AddSingleton<AgentSkillsProviderMarker>();
        
        var path = builder.Configuration[SkillsPathConfigKey] ?? DefaultSkillsPath;
        builder.AddAIAgentSkillsProvider(path);
    }

    private sealed class AgentSkillsProviderMarker;

    private sealed class RegisteredAgentSkillWrapper<TSkill>(TSkill skill) : IRegisteredAgentSkill
        where TSkill : AgentClassSkill<TSkill>
    {
        public void AddToBuilder(AgentSkillsProviderBuilder providerBuilder) => providerBuilder.UseSkill(skill);
    }
}
#pragma warning restore MAAI001
