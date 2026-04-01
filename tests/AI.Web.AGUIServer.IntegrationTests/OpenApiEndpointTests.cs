using System.Net;

namespace AI.Web.AGUIServer.IntegrationTests;

[TestClass]
public sealed class OpenApiEndpointTests
{
    private static WebApplicationFactory<Program> _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new AGUIServerFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
        });
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task OpenApi_IsAvailableInDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
