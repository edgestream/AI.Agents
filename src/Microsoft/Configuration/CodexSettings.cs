namespace AI.Agents.Microsoft.Configuration;

public sealed class CodexSettings
{
    public string AuthFile { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-5.4";
    public string? AccessToken { get; set; }
    public string? AccountID {get; set; }
}