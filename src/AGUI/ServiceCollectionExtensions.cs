using Microsoft.Extensions.DependencyInjection;

namespace AI.Agents.AGUI;

public static class ServiceCollectionExtensions
{
    public static void AddAGUIContextProvider(this IServiceCollection services)
    {
        services.AddSingleton<AGUIAIContextProvider>();
    }
}
