using MealPlanner.Abstractions;
using MealPlanner.Providers;
using Azure.AI.Projects;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("chefkoch");
builder.Services.AddMemoryCache(o => o.SizeLimit = 100);
builder.Services.AddSingleton<IRecipeSource>(sp =>
    new CachedRecipeSource(
        new ChefkochRecipeSource(sp.GetRequiredService<IHttpClientFactory>()),
        sp.GetRequiredService<IMemoryCache>()
    )
);
builder.Services.AddKeyedSingleton<IRecipeRenderer, A2UIRecipeRenderer>("a2ui");
builder.AddAIClient();
builder.AddAIAgent("meal-planner", (sp, name) =>
{
    var projectClient = sp.GetRequiredService<AIProjectClient>();
    return projectClient.AsAIAgent(
        builder.Configuration["Foundry:Model"] ?? "gpt-5.3-chat",
        "You are an agent that helps users plan meals and find recipes.",
        name,
        "An agent that helps users plan meals and find recipes.",
        [
            RecipeFunctionFactory.CreateGetFunction(sp),
            RecipeFunctionFactory.CreateRenderFunction(sp),
            RecipeFunctionFactory.CreateSearchFunction(sp),
        ]
    );
});
var app = builder.Build();
app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("meal-planner"));
await app.RunAsync();