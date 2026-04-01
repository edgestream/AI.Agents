namespace AI.Web.E2ETests;

/// <summary>
/// End-to-end tests for the AGUIChat frontend using Playwright.
///
/// Prerequisites:
///   1. Build this project.
///   2. Install Playwright browsers once:
///      pwsh tests/AI.Web.E2ETests/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
///   3. Start the Next.js frontend (docker or npm):
///      docker compose -f docker-compose.e2e.yml up -d
///      — or —
///      cd src/AI.Web.AGUIChat && BACKEND_URL=http://localhost:8080/ npm run dev
///
/// The backend is started automatically by <see cref="StubBackendFixture"/> as part of
/// the test assembly setup, so no separate backend process is required.
///
/// Configuration (environment variables):
///   E2E_BASE_URL  – Base URL of the running frontend (default: http://localhost:3000).
///
/// Test categories:
///   Live – Tests that require a running frontend. Run with:
///          dotnet test tests/AI.Web.E2ETests --filter "TestCategory=Live"
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
    /// The backend stub (<see cref="FakeChatClient"/>) streams "Hello from FakeChatClient"
    /// through the real AG-UI SSE serialization path.
    /// Requires a running frontend — see <c>docker-compose.e2e.yml</c> or <c>npm run dev</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Live")]
    public async Task ChatPage_CanSendMessage_ReceivesResponse()
    {
        await Page.GotoAsync("/");

        // Type and submit the user message.
        var input = Page.GetByRole(AriaRole.Textbox);
        await input.FillAsync("Hello, are you there?");
        await input.PressAsync("Enter");

        // Wait for the assistant response from FakeChatClient to appear.
        await Expect(Page.GetByText("Hello from FakeChatClient"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    }
}
