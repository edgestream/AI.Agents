namespace AI.AGUI.Server.IntegrationTests;

[TestClass]
public sealed class StartupTests
{
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void MissingEndpoint_ThrowsInvalidOperationException()
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
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void MissingDeploymentName_ThrowsInvalidOperationException()
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
    }

    [TestMethod]
    public void FoundryMissingModel_FallsBackToAzureOpenAI_WhenConfigured()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Foundry:ProjectEndpoint", "https://fake.foundry.endpoint/");
                builder.UseSetting("Foundry:Model", "");
                builder.UseSetting("AzureOpenAI:Endpoint", "https://fake.openai.azure.com/");
                builder.UseSetting("AzureOpenAI:DeploymentName", "some-deployment");
            });

        _ = factory.Server;
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void FoundryMissingModel_FallsBackToAzureOpenAIValidation_ThrowsInvalidOperationException()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Missing Foundry:Model forces the Azure OpenAI fallback path.
                builder.UseSetting("Foundry:ProjectEndpoint", "https://fake.foundry.endpoint/");
                builder.UseSetting("Foundry:Model", "");
            });

        _ = factory.Server;
    }
}
