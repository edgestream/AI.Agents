namespace AI.Web.AGUIServer;

/// <summary>
/// Shared data contract for a single news article returned by source agents
/// (<c>TagesschauAgent</c>, <c>HeiseNewsAgent</c>) and consumed by <c>NewsDigestAgent</c>.
/// </summary>
internal sealed record NewsArticle(
    string Source,
    string Headline,
    string Topline,
    string Teaser,
    string Date);
