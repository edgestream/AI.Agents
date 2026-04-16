namespace AI.AGUI.Auth;

/// <summary>
/// Provides storage and retrieval of OAuth authorization state during the OAuth flow.
/// </summary>
public interface IOAuthStateStore
{
    /// <summary>
    /// Stores the OAuth state for later retrieval during callback.
    /// </summary>
    /// <param name="state">The OAuth state to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask StoreStateAsync(OAuthState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves and removes the OAuth state by state ID.
    /// </summary>
    /// <param name="stateId">The state ID to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The OAuth state, or null if not found or expired.</returns>
    ValueTask<OAuthState?> ConsumeStateAsync(string stateId, CancellationToken cancellationToken = default);
}
