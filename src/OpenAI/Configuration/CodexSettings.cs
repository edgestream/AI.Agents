namespace AI.Agents.OpenAI;

public sealed class CodexSettings : OpenAISettings
{
    public CodexSettings()
    {
        Endpoint = "https://chatgpt.com/backend-api/codex";
        Protocol = OpenAIPProtocolSettings.Responses;
        Model = "gpt-5.4";
    }

    public string? AccountID { get; set; }
}
