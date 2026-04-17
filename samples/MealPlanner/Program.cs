using MealPlanner.Abstractions;
using MealPlanner.Providers;
using AI.Agents.Microsoft;
using Azure.AI.Projects;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IRecipeSource, ChefkochRecipeSource>();
builder.Services.AddSingleton<IRecipeRenderer, A2UIRecipeRenderer>();
builder.Services.AddAIProjectClient();
builder.AddAIAgent("meal-planner", (sp, name) =>
{
    var projectClient = sp.GetRequiredService<AIProjectClient>();
    return projectClient.AsAIAgent(
        model: builder.Configuration["Foundry:Model"]!,
        instructions: "You are an agent that helps users find recipes.",
        name: name,
        description: "An agent that helps users find recipes.",
        tools: [RecipeFunctionFactory.CreateSearchFunction(sp)]
    );
});

var app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("meal-planner"));

await app.RunAsync();