namespace AI.Agents.Microsoft.Configuration;

public sealed class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
}