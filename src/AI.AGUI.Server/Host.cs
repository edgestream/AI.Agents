using AI.MAF.Tools;
using AI.MCP.Client;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001 // AgentSkillsProvider is marked experimental; suppress until the API stabilises in a future Microsoft.Agents.AI release

namespace AI.AGUI.Server;

/// <summary>
/// Bootstraps and runs the AGUI server.
/// External module projects (those that implement <see cref="Abstractions.IAgentModule"/>)
/// can call <see cref="RunAsync"/> as their sole entry point so the server is launched
/// with their module assembly already in the AppDomain — enabling
/// <see cref="AgentModuleLoader.LoadAgentModule"/> to discover the module automatically.
/// </summary>
public static class Host
{
    public static async Task RunAsync(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
        builder.WebHost.UseUrls("http://localhost:8000");
        builder.AddAIClient();
        builder.AddMCPClient();
        builder.LoadAgentModule();
        if (!builder.Services.Any(static d => d.IsKeyedService && d.ServiceType == typeof(AIAgent)))
        {
            builder.AddAIAgent("AGUIAgent", (sp, key) =>
            {
                var clientRegistry = sp.GetRequiredService<McpClientRegistry>();
                var toolsContext = new McpClientToolsAIContextProvider(clientRegistry);
                var configuration = sp.GetRequiredService<IConfiguration>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var skillsPath = configuration["Skills:Path"] ?? "skills";
                var skillsProvider = new AgentSkillsProvider(
                    Path.Combine(AppContext.BaseDirectory, skillsPath),
                    loggerFactory: loggerFactory);
                var agentOptions = new ChatClientAgentOptions
                {
                    Name = key,
                    ChatOptions = new ChatOptions
                    {
                        Instructions = "You are a helpful assistant.",
                        Tools = [FetchAIFunctionFactory.CreateAIFunction(sp)]
                    },
                    AIContextProviders = [toolsContext, skillsProvider],
                };
                return new ChatClientAgent(sp.GetRequiredService<IChatClient>(), agentOptions, loggerFactory, services: sp);
            });
        }
        builder.Services.AddAGUI();
        WebApplication app = builder.Build();
        app.MapGet("/health", () => "OK");
        app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("AGUIAgent"));
        await app.RunAsync();
    }
}
