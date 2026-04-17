using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AI.Agents.Microsoft.Skills;

#pragma warning disable MAAI001 // AgentClassSkill is for evaluation purposes

/// <summary>
/// Extension methods for registering agent skills and the skills provider with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a class-based agent skill as a singleton and automatically registers the skills provider
    /// if not already registered.
    /// </summary>
    /// <typeparam name="TSkill">The type of the agent skill to register.</typeparam>
    /// <param name="services">The service collection to add the skill to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAIAgentSkill<TSkill>(this IServiceCollection services)
        where TSkill : AgentClassSkill<TSkill>
    {
        services.AddSingleton<AgentSkill, TSkill>();
        services.TryAddSingleton(sp =>
        {
            var builder = new AgentSkillsProviderBuilder();
            builder.UseSkills(sp.GetServices<AgentSkill>());
            return builder.Build();
        });
        return services;
    }
}