using AI.AGUI.Abstractions;
using AI.AGUI.Server;
using AI.MCP.Client;
using MealPlanner;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001 // AgentSkillsProvider/McpClientToolsAIContextProvider are experimental

public class AgentModule : IAgentModule
{
    public void Register(IHostApplicationBuilder builder)
    {
        builder.AddAIAgent("AGUIAgent", (sp, key) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var clientRegistry = sp.GetRequiredService<McpClientRegistry>();
            var toolsContext = new McpClientToolsAIContextProvider(clientRegistry);
            var configuration = sp.GetRequiredService<IConfiguration>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var skillsPath = configuration["Skills:Path"] ?? "skills";
            var skillsProvider = new AgentSkillsProvider(Path.Combine(AppContext.BaseDirectory, skillsPath), loggerFactory: loggerFactory);
            var agentOptions = new ChatClientAgentOptions
            {
                Name = key,
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                    You are a helpful meal planning assistant.

                    When the user asks for a recipe, call the search_recipes tool.
                    The tool returns a rendered recipe card that is displayed automatically — do not re-summarize the recipe as text.
                    """,
                    Tools = [SearchRecipesFunction.CreateAIFunction()],
                },
                AIContextProviders = [toolsContext, skillsProvider],
            };
            return new ChatClientAgent(chatClient, agentOptions, loggerFactory, services: sp);
        });
    }
}

#pragma warning restore MAAI001