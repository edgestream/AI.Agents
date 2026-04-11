using MealPlanner;
using Azure.AI.Projects;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);
builder.AddAIClient();
builder.AddAGUIApplication("meal-planner", "Meal Planner", (sp, name) =>
{
    var projectClient = sp.GetRequiredService<AIProjectClient>();
    return projectClient.AsAIAgent(
        builder.Configuration["Foundry:Model"] ?? "gpt-5.3-chat",
        "You are an agent that helps users plan meals and find recipes.",
        name,
        "An agent that helps users plan meals and find recipes.",
        [SearchRecipesFunction.CreateAIFunction()]
    );
});
var app = builder.Build();
app.MapGet("/health", () => "OK");
app.MapAGUI();
await app.RunAsync();