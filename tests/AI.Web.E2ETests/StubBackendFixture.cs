namespace AI.Web.E2ETests;

/// <summary>
/// Assembly-level fixture that starts a real Kestrel HTTP server on
/// <c>http://localhost:8080</c> using <see cref="WebApplicationFactory{TEntryPoint}"/>
/// backed by <see cref="FakeChatClient"/>. This exercises the real AG-UI SSE
/// serialization path without requiring Azure credentials.
///
/// The server is started once before any test in the assembly runs and stopped
/// after all tests complete.
/// </summary>
[TestClass]
public static class StubBackendFixture
{
    private static WebApplicationFactory<Program>? _factory;

    [AssemblyInitialize]
    public static void Start(TestContext context)
    {
        _factory = new E2EServerFactory();

        // Force the host to start and bind the port.
        _factory.CreateClient().Dispose();
    }

    [AssemblyCleanup]
    public static void Stop() => _factory?.Dispose();
}

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> that:
/// <list type="bullet">
///   <item>Injects dummy Azure OpenAI configuration so startup validation passes.</item>
///   <item>Replaces <see cref="IChatClient"/> with <see cref="FakeChatClient"/>.</item>
///   <item>
///     Overrides <see cref="CreateHost"/> to start a real Kestrel server on
///     <c>http://localhost:8080</c> in addition to the in-memory test server,
///     so the Next.js frontend (and Playwright) can reach the backend via TCP.
///   </item>
/// </list>
/// </summary>
internal sealed class E2EServerFactory : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide dummy AzureOpenAI config so startup validation passes.
        builder.UseSetting("AzureOpenAI:Endpoint", "https://fake.openai.azure.com/");
        builder.UseSetting("AzureOpenAI:DeploymentName", "fake-deployment");

        // Replace the real IChatClient with a deterministic fake.
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IChatClient));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<IChatClient>(new FakeChatClient());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the in-memory test host (used by CreateClient()).
        var testHost = builder.Build();

        // Also start a real Kestrel host on a fixed TCP port so the Next.js
        // frontend can reach the backend from outside the test process.
        builder.ConfigureWebHost(b => b.UseKestrel(o => o.ListenLocalhost(8080)));
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _kestrelHost?.Dispose();

        base.Dispose(disposing);
    }
}
