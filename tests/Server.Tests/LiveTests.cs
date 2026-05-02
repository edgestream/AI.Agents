using System.Net;
using System.Net.Http.Json;
using AI.Agents.Microsoft;
using AI.Agents.Microsoft.Client;
using AI.Agents.Microsoft.Configuration;
using AI.Agents.OpenAI;
using Microsoft.Extensions.Configuration;

namespace AI.Agents.Server.Tests;

/// <summary>
/// Optional live integration tests that boot the real application against real cloud credentials.
/// The current server wiring uses provider options and <see cref="AI.Agents.ServiceCollectionExtensions.AddAIClient"/>,
/// so live tests require a configured provider in local configuration.
/// Tests are skipped when Foundry is not configured (e.g. in CI without credentials).
/// </summary>
[TestClass]
[TestCategory("ExternalDependency")]
[TestCategory("Live")]
public sealed class LiveTests
{
    private static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

    /// <summary>
    /// Returns true when the Foundry endpoint is configured in local settings,
    /// indicating that real cloud calls can be made.
    /// </summary>
    private static bool HasProviderConfig()
    {
        var serverBasePath = FindServerBasePath();
        var config = new ConfigurationBuilder()
            .SetBasePath(serverBasePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var openAI = config.GetSection("OpenAI").Get<OpenAISettings>();
        if (openAI is not null && HasRequiredOpenAISettings(openAI))
        {
            return true;
        }

        var codex = config.GetSection("Codex").Get<CodexSettings>();
        if (codex is not null && HasRequiredOpenAISettings(codex) && !string.IsNullOrWhiteSpace(codex.AccountID))
        {
            return true;
        }

        var foundry = config.GetSection("Foundry").Get<FoundrySettings>();
        if (foundry is not null && FoundryChatClientProvider.HasValidSettings(foundry))
        {
            return true;
        }

        var azureOpenAI = config.GetSection("AzureOpenAI").Get<AzureOpenAISettings>();
        return azureOpenAI is not null && AzureOpenAIChatClientProvider.HasValidSettings(azureOpenAI);
    }

    private static bool HasRequiredOpenAISettings(OpenAISettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.ApiKey)
            && !string.IsNullOrWhiteSpace(settings.Model)
            && (string.IsNullOrWhiteSpace(settings.Endpoint)
                || Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out _));
    }

    private static string FindServerBasePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Server");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
    }

    [TestMethod]
    public async Task Live_HealthEndpoint_ReturnsOk()
    {
        if (!HasProviderConfig())
            Assert.Inconclusive("No AI provider configured — skipping live test.");

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(EnvironmentName));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Live_AGUIEndpoint_ReturnsSSEStream()
    {
        if (!HasProviderConfig())
            Assert.Inconclusive("No AI provider configured — skipping live test.");

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(EnvironmentName));
        using var client = factory.CreateClient();

        var payload = new
        {
            threadId = "live-test-thread",
            runId = "live-test-run",
            messages = new[]
            {
                new { role = "user", content = "Say hello in one word." }
            },
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var response = await client.PostAsJsonAsync("/", payload, cts.Token);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync(cts.Token);
        Assert.IsFalse(string.IsNullOrWhiteSpace(body), "SSE stream body should not be empty.");
    }
}
