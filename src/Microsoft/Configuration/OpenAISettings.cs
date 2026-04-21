namespace AI.Agents.Microsoft.Configuration;

public sealed class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    public string? OrganizationId { get; set; }

    public string? ProjectId { get; set; }
}