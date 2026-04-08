using AI.AGUI.Server;
using AI.MAF.Tools;
using AI.MCP.Client;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
builder.AddAIClient();
builder.AddMCPClient();
builder.LoadAgentModule();
if (!builder.Services.Any(static d => d.IsKeyedService && d.ServiceType == typeof(AIAgent)))
{
    builder.AddAIAgent("AGUIAgent", (sp, key) =>
    {
        var clientRegistry = sp.GetRequiredService<McpClientRegistry>();
        var toolsContext = new McpClientToolsAIContextProvider(clientRegistry);
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var agentOptions = new ChatClientAgentOptions
        {
            Name = key,
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    You are a helpful assistant.

                    When the user asks for a recipe, call the search_recipes tool and format the result
                    as A2UI JSONL output. Output exactly three JSONL lines — surfaceUpdate, dataModelUpdate,
                    and beginRendering — using the data returned by the tool. Do not output any other text
                    alongside the JSONL lines.

                    A2UI JSONL example for a recipe (replace data with actual tool result):
                    ---BEGIN RECIPE_EXAMPLE---
                    {"surfaceUpdate":{"surfaceId":"recipe-surface","components":[{"id":"root-col","component":{"Column":{"children":{"explicitList":["recipe-title","prep-time","ingredients-list","steps-list"]}}}},{"id":"recipe-title","component":{"Text":{"usageHint":"h1","text":{"path":"title"}}}},{"id":"prep-time","component":{"Text":{"usageHint":"label","text":{"path":"prepTime"}}}},{"id":"ingredients-list","component":{"List":{"direction":"vertical","children":{"template":{"componentId":"ingredient-item","dataBinding":"/ingredients"}}}}},{"id":"ingredient-item","component":{"Text":{"usageHint":"body","text":{"path":"name"}}}},{"id":"steps-list","component":{"List":{"direction":"vertical","children":{"template":{"componentId":"step-item","dataBinding":"/steps"}}}}},{"id":"step-item","component":{"Text":{"usageHint":"body","text":{"path":"instruction"}}}}]}}
                    {"dataModelUpdate":{"surfaceId":"recipe-surface","path":"/","contents":{"title":"Spaghetti Carbonara","prepTime":"20 min","ingredients":[{"name":"200g spaghetti"},{"name":"100g pancetta"},{"name":"2 eggs"}],"steps":[{"instruction":"Cook pasta al dente"},{"instruction":"Fry pancetta until crispy"},{"instruction":"Mix eggs and cheese off the heat, combine"}]}}}
                    {"beginRendering":{"surfaceId":"recipe-surface","root":"root-col","styles":{}}}
                    ---END RECIPE_EXAMPLE---
                    """,
                Tools = [FetchAIFunctionFactory.CreateAIFunction(sp), SearchRecipesFunction.CreateAIFunction()]
            },
            AIContextProviders = [toolsContext],
        };
        return new ChatClientAgent(sp.GetRequiredService<IChatClient>(), agentOptions, loggerFactory, services: sp);
    });
}
builder.Services.AddAGUI();

WebApplication app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("AGUIAgent"));

await app.RunAsync();

public partial class Program { }