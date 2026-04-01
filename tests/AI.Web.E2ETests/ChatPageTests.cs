namespace AI.Web.E2ETests;

/// <summary>
/// End-to-end tests for the AGUIChat frontend using Playwright.
///
/// Prerequisites:
///   1. Build this project.
///   2. Install Playwright browsers once:
///      pwsh tests/AI.Web.E2ETests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
///
/// Configuration (environment variables):
///   E2E_BASE_URL  – Base URL of the running frontend (default: http://localhost:3000).
///   E2E_LIVE_TEST – Set to "true" to run the send-message test (requires a live backend).
/// </summary>
[TestClass]
public sealed class ChatPageTests : PageTest
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:3000";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions { BaseURL = BaseUrl };
    }

    /// <summary>
    /// Verifies that the chat page loads and has the expected title.
    /// </summary>
    [TestMethod]
    public async Task ChatPage_LoadsWithCorrectTitle()
    {
        await Page.GotoAsync("/");

        await Expect(Page).ToHaveTitleAsync("AGUIChat");
    }

    /// <summary>
    /// Verifies that the chat input textarea is rendered and focusable.
    /// </summary>
    [TestMethod]
    public async Task ChatPage_DisplaysChatInput()
    {
        await Page.GotoAsync("/");

        var input = Page.GetByRole(AriaRole.Textbox);
        await Expect(input).ToBeVisibleAsync();
    }

    /// <summary>
    /// Sends a message and verifies that the assistant's response is rendered.
    /// Requires a running backend; gated by the <c>E2E_LIVE_TEST=true</c> environment variable.
    /// With the docker-compose E2E stack (<c>docker compose -f docker-compose.e2e.yml up</c>)
    /// the stub backend returns "Hello from stub!" as its response.
    /// </summary>
    [TestMethod]
    public async Task ChatPage_CanSendMessage_ReceivesResponse()
    {
        if (!bool.TryParse(Environment.GetEnvironmentVariable("E2E_LIVE_TEST"), out var live) || !live)
        {
            Assert.Inconclusive(
                "Set E2E_LIVE_TEST=true to run this test. " +
                "A running frontend and backend are required (see docker-compose.e2e.yml).");
        }

        await Page.GotoAsync("/");

        // Type and submit the user message.
        var input = Page.GetByRole(AriaRole.Textbox);
        await input.FillAsync("Hello, are you there?");
        await input.PressAsync("Enter");

        // Wait for an assistant response to appear (stub responds with "Hello from stub!").
        await Expect(Page.GetByText("Hello from stub!"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    }
}
