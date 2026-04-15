public sealed class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;

    public string DeploymentName { get; set; } = string.Empty;

    public string? ApiKey { get; set; }
}