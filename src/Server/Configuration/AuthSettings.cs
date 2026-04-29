namespace AI.Agents.Server.Configuration;

/// <summary>
/// Configuration settings controlling access to the endpoint.
/// </summary>
public sealed class AuthSettings
{
    /// <summary>
    /// When <c>true</c>, unauthenticated users are blocked from using the endpoint.
    /// </summary>
    public bool AgentRequiresAuthentication { get; set; }
}
