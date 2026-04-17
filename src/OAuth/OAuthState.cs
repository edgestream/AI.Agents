using System.Security.Cryptography;
using System.Text;

namespace AI.Agents.OAuth;

/// <summary>
/// Represents the state of an ongoing OAuth authorization flow.
/// Stored temporarily while the user is redirected to the external OAuth provider.
/// </summary>
public sealed record OAuthState
{
    /// <summary>
    /// Gets the unique state parameter used to prevent CSRF attacks.
    /// </summary>
    public required string StateId { get; init; }

    /// <summary>
    /// Gets the user ID who initiated the authorization flow.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the MCP server name that requires authorization.
    /// </summary>
    public required string McpServerName { get; init; }

    /// <summary>
    /// Gets the PKCE code verifier used for the authorization code exchange.
    /// </summary>
    public string? CodeVerifier { get; init; }

    /// <summary>
    /// Gets the URL to redirect to after authorization completes.
    /// </summary>
    public string? RedirectUri { get; init; }

    /// <summary>
    /// Gets the timestamp when this state was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Generates a new random state ID.
    /// </summary>
    public static string GenerateStateId() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    /// <summary>
    /// Generates a PKCE code verifier.
    /// </summary>
    public static string GenerateCodeVerifier() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    /// <summary>
    /// Computes the PKCE code challenge from a code verifier.
    /// </summary>
    public static string ComputeCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}