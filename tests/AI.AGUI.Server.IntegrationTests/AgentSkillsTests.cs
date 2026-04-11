using System.Net.Http.Json;

namespace AI.AGUI.Server.IntegrationTests;

/// <summary>
/// Tests that verify <see cref="Microsoft.Agents.AI.AgentSkillsProvider"/> is wired into
/// the default agent and degrades gracefully when no skills are present.
/// </summary>
[TestClass]
public sealed class AgentSkillsTests
{
    /// <summary>
    /// The server must start and serve requests when <c>Skills:Path</c> points to a
    /// directory that does not exist. AgentSkillsProvider discovers no skills but does
    /// not throw, so the agent remains fully functional.
    /// </summary>
    [TestMethod]
    public async Task Server_StartsCleanly_WhenSkillsPathDoesNotExist()
    {
        using var factory = new AGUIServerFactory()
            .WithWebHostBuilder(b => b.UseSetting("Skills:Path", "nonexistent-skills-dir"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// The server must start and serve requests when <c>Skills:Path</c> points to an
    /// empty directory (no skill sub-directories).
    /// </summary>
    [TestMethod]
    public async Task Server_StartsCleanly_WhenSkillsDirectoryIsEmpty()
    {
        var emptySkillsDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(emptySkillsDir);
        try
        {
            using var factory = new AGUIServerFactory()
                .WithWebHostBuilder(b => b.UseSetting("Skills:Path", emptySkillsDir));
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/health");

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            Directory.Delete(emptySkillsDir, recursive: true);
        }
    }

    /// <summary>
    /// When a valid <c>SKILL.md</c> is present in the configured skills directory the
    /// server must start and the AGUI endpoint must remain operational. This confirms the
    /// provider is wired without breaking the streaming pipeline.
    /// </summary>
    [TestMethod]
    public async Task AGUIEndpoint_RemainsOperational_WhenSkillIsPresent()
    {
        var skillsDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var skillDir = Path.Combine(skillsDir, "test-skill");
        Directory.CreateDirectory(skillDir);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(skillDir, "SKILL.md"),
                """
                ---
                name: test-skill
                description: A minimal skill used in integration tests. Use when asked to test skills.
                ---
                ## test-skill
                This skill is used only in automated tests.
                """);

            using var factory = new AGUIServerFactory()
                .WithWebHostBuilder(b => b.UseSetting("Skills:Path", skillsDir));
            using var client = factory.CreateClient();

            var payload = new
            {
                threadId = "skills-test-thread",
                runId = "skills-test-run",
                messages = System.Array.Empty<object>(),
                tools = System.Array.Empty<object>(),
                context = System.Array.Empty<object>(),
                forwardedProps = new { }
            };

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await client.PostAsJsonAsync("/agents/agui-agent", payload, cts.Token);

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            Directory.Delete(skillsDir, recursive: true);
        }
    }
}
