namespace AI.Agents.Microsoft.Configuration;

public enum OpenAIPProtocolSettings
{
    Completions,    // /v1/chat/completions (well-established)
    Responses       // /v1/responses (newer protocol)
}

public sealed class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public string? Endpoint { get; set; }

    public string? OrganizationId { get; set; }

    public string? ProjectId { get; set; }

    public OpenAIPProtocolSettings Protocol { get; set; } = OpenAIPProtocolSettings.Responses;
}