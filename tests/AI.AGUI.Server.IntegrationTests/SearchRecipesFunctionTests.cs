using System.Net.Http.Json;
using MealPlanner;

namespace AI.AGUI.Server.IntegrationTests;

[TestClass]
public sealed class SearchRecipesFunctionTests
{
    [TestMethod]
    public void CreateAIFunction_HasExpectedName()
    {
        var function = SearchRecipesFunction.CreateAIFunction();

        Assert.AreEqual("search_recipes", function.Name);
    }

    [TestMethod]
    public void CreateAIFunction_HasNonEmptyDescription()
    {
        var function = SearchRecipesFunction.CreateAIFunction();

        Assert.IsFalse(string.IsNullOrWhiteSpace(function.Description),
            "search_recipes should have a non-empty description.");
    }

    [TestMethod]
    public async Task AGUIEndpoint_WithSearchRecipesTool_ReturnsSuccessStream()
    {
        // Arrange: factory uses FakeChatClient which returns a plain text response
        // without invoking any tools. Verifies the default server agent starts
        // and the AGUI endpoint remains healthy.
        using var factory = new AGUIServerFactory();
        using var client = factory.CreateClient();

        var payload = new
        {
            threadId = "recipe-thread",
            runId = "recipe-run",
            messages = Array.Empty<object>(),
            tools = Array.Empty<object>(),
            context = Array.Empty<object>(),
            forwardedProps = new { }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        using var response = await client.PostAsJsonAsync("/", payload, cts.Token);

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(cts.Token);
        Assert.IsFalse(string.IsNullOrWhiteSpace(body), "SSE stream should not be empty.");
    }
}
