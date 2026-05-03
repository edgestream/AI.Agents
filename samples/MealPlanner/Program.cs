using AI.Agents.Microsoft;
using Azure.AI.Projects;
using MealPlanner.Abstractions;
using MealPlanner.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI;

#pragma warning disable OPENAI001

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IRecipeSource, ChefkochRecipeSource>();
builder.Services.AddAIProjectClient();
builder.AddAIAgent("chef", (sp, name) =>
{
    var projectClient = sp.GetRequiredService<AIProjectClient>();
    return projectClient.GetProjectOpenAIClient().GetResponsesClient().AsIChatClient().AsAIAgent(
        instructions: "You are an agent that helps users find recipes.",
        name: name,
        description: "An agent that helps users find recipes.",
        tools: [RecipeSearchFunctionFactory.CreateAIFunction(sp)]
    );
});

var app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("chef"));

await app.RunAsync();
