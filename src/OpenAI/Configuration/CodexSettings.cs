namespace AI.Agents.OpenAI;

public sealed class CodexSettings : OpenAISettings
{
    public CodexSettings()
    {
        Endpoint = "https://chatgpt.com/backend-api/codex";
        Protocol = OpenAIPProtocolSettings.Responses;
    }
    public string? AccountID { get; set; }
}
