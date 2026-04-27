namespace AI.Agents.AGUI;

public sealed record AGUIContextItem(string Description, string Value);

public sealed record AGUIRequestContext(IReadOnlyList<AGUIContextItem> ContextItems, string? A2UIAction)
{
    public static readonly object HttpContextItemKey = typeof(AGUIRequestContext);
}