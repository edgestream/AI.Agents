namespace AI.Agents.Server;

internal sealed record CopilotKitContextItem(string Description, string Value);

internal sealed record CopilotKitRequestContext(
    IReadOnlyList<CopilotKitContextItem> ContextItems,
    string? A2UIAction)
{
    public static readonly object HttpContextItemKey = typeof(CopilotKitRequestContext);
}