namespace AI.Agents.Server.Configuration;

/// <summary>
/// Configuration settings controlling access to the AI agent endpoint.
/// </summary>
public sealed class AgentAccessSettings
{
    /// <summary>
    /// When <c>true</c>, unauthenticated users are blocked from using the agent endpoint.
    /// Set this when a frontier (premium/costly) model is configured to prevent
    /// anonymous token consumption.
    /// </summary>
    public bool RequireAuthenticationForAgent { get; set; }
}
