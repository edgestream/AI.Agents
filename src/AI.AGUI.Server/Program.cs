using AI.MAF.Client;
using AI.MAF.Skills;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;

#pragma warning disable MAAI001 // AgentSkillsProvider is marked experimental

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile($"/run/secrets/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

builder.Services.AddAIProjectClient();
builder.Services.AddAIAgentSkill<DateTimeSkill>();
builder.Services.AddAIAgent("agui-agent", (sp, key) =>
{
    var projectClient = sp.GetRequiredService<AIProjectClient>();
    var skillsProvider = sp.GetRequiredService<AgentSkillsProvider>();
    return projectClient.AsAIAgent(
        new ChatClientAgentOptions
        {
            Name = key,
            Description = "Generic Agent",
            ChatOptions = new()
            {
                ModelId = "gpt-5.3-chat",
                Instructions = """You are a helpful assistant."""
            },
            AIContextProviders = [skillsProvider]
        },
        services: sp);
});

var app = builder.Build();

app.MapGet("/health", () => "OK");
app.MapAGUI("/", app.Services.GetRequiredKeyedService<AIAgent>("agui-agent"));

await app.RunAsync();