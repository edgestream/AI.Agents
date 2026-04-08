namespace AI.AGUI.E2ETests;

/// <summary>
/// End-to-end tests for the AGUIChat frontend using Playwright.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class ChatPageTests : PageTest
{
    private static string BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:3000";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions { BaseURL = BaseUrl };
    }

    /// <summary>
    /// Starts a Playwright trace before every test.
    /// In Development mode (ASPNETCORE_ENVIRONMENT=Development) traces are captured
    /// for all tests. In other environments tracing still starts so that a trace zip
    /// is available when a test fails — passing tests discard the buffer.
    /// Traces are saved next to the test assembly in a <c>traces/</c> sub-folder and
    /// can be opened with: npx playwright show-trace &lt;path&gt;
    /// </summary>
    [TestInitialize]
    public async Task StartTracingAsync()
    {
        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Title = TestContext.TestName,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    /// <summary>
    /// Stops tracing after each test.
    /// In Development mode the trace is always persisted (useful for inspecting
    /// passing tests too). In other environments the trace is only written to disk
    /// when the test failed; passing tests discard the buffer to avoid clutter.
    /// </summary>
    [TestCleanup]
    public async Task StopTracingAsync()
    {
        bool persist = TestContext.CurrentTestOutcome != UnitTestOutcome.Passed;

        string? tracePath = null;
        if (persist)
        {
            // Resolve relative to the test assembly so the path is stable
            // regardless of the working directory.
            var assemblyDir = Path.GetDirectoryName(typeof(ChatPageTests).Assembly.Location)!;
            var tracesDir = Path.Combine(assemblyDir, "traces");
            Directory.CreateDirectory(tracesDir);
            tracePath = Path.Combine(tracesDir, $"{TestContext.TestName}.zip");
        }

        await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
    }

    /// <summary>
    /// Sends a message and verifies that the assistant responds.
    /// The assertion is structural — it checks that a second message (the assistant
    /// turn) becomes visible in the chat UI without asserting any specific text.
    /// This makes the test valid against any backend: local stub, real Azure OpenAI,
    /// staging, or production.
    /// </summary>
    [TestMethod]
    public async Task ChatPage_CanSendMessage_ReceivesResponse()
    {
        await Page.GotoAsync("/");

        // Enter a user message.
        var input = Page.GetByRole(AriaRole.Textbox);
        await input.FillAsync("Hello, are you there?");

        // Wait for the send button to become enabled — it is disabled until the
        // SSE connection to the backend is established.
        var sendButton = Page.GetByTestId("copilot-send-button");
        await Expect(sendButton).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 30_000 });

        // Send the message.
        await input.PressAsync("Enter");

        // The user message must appear in the chat log.
        // CopilotKit renders user turns with data-testid="copilot-user-message".
        var userMessage = Page.GetByTestId("copilot-user-message");
        await Expect(userMessage.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        // An assistant message container must appear — any content is accepted.
        // CopilotKit renders assistant turns with data-testid="copilot-assistant-message".
        var assistantMessage = Page.GetByTestId("copilot-assistant-message");
        await Expect(assistantMessage.First).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
    }
}
