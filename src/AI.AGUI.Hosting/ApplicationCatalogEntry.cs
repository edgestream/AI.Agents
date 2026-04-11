namespace AI.AGUI.Hosting;

/// <summary>
/// Represents a single application entry returned by the catalog discovery endpoint.
/// </summary>
/// <param name="Id">Stable identifier used as the agent key and in the URL route.</param>
/// <param name="DisplayName">Human-readable label shown in the UI.</param>
/// <param name="Route">The backend URL path that the AG-UI endpoint is mapped to.</param>
public sealed record ApplicationCatalogEntry(string Id, string DisplayName, string Route);
