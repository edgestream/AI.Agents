namespace AI.Web.AGUIServer.IntegrationTests;

[TestClass]
public sealed class StartupTests
{
    [TestMethod]
    public void MissingEndpoint_ThrowsInvalidOperationException()
    {
        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Clear Foundry endpoint so auto-detection picks the Azure OpenAI path.
                    builder.UseSetting("Foundry:ProjectEndpoint", "");
                    // Provide deployment name but omit endpoint to trigger validation.
                    builder.UseSetting("AzureOpenAI:Endpoint", "");
                    builder.UseSetting("AzureOpenAI:DeploymentName", "some-deployment");
                });

            // Force the host to build and start, which triggers configuration validation.
            _ = factory.Server;
        });

        StringAssert.Contains(ex.Message, "endpoint");
    }

    [TestMethod]
    public void MissingDeploymentName_ThrowsInvalidOperationException()
    {
        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Clear Foundry endpoint so auto-detection picks the Azure OpenAI path.
                    builder.UseSetting("Foundry:ProjectEndpoint", "");
                    // Provide endpoint but omit deployment name to trigger validation.
                    builder.UseSetting("AzureOpenAI:Endpoint", "https://fake.openai.azure.com/");
                    builder.UseSetting("AzureOpenAI:DeploymentName", "");
                });

            _ = factory.Server;
        });

        StringAssert.Contains(ex.Message, "deployment name");
    }
}
