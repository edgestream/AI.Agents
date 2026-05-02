namespace AI.Agents.OpenAI;

public enum OpenAIPProtocolSettings
{
    Completions,    // /v1/chat/completions (well-established)
    Responses       // /v1/responses (newer protocol)
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public virtual string? Endpoint { get; set; }
    public string Model { get; set; } = string.Empty;
    public virtual OpenAIPProtocolSettings Protocol { get; set; } = OpenAIPProtocolSettings.Responses;
}
