using System.ComponentModel;
using System.Text.Json;

namespace AI.Web.AGUIServer;

internal static class CitationTool
{
    public const string ToolName = "DisplaySources";

    [Description("Display source citations in the chat UI.")]
    public static string DisplaySources(
        [Description("Ordered list of sources supporting the answer.")]
        SourceCitation[] sources)
    {
        var normalizedSources = sources
            .Where(static source =>
                !string.IsNullOrWhiteSpace(source.Title) &&
                !string.IsNullOrWhiteSpace(source.Url) &&
                Uri.IsWellFormedUriString(source.Url, UriKind.Absolute))
            .Select(static source => new SourceCitation
            {
                Title = source.Title.Trim(),
                Url = source.Url.Trim(),
                Snippet = source.Snippet.Trim(),
            })
            .Take(5)
            .ToArray();

        return JsonSerializer.Serialize(new { sources = normalizedSources });
    }
}

internal sealed class SourceCitation
{
    [Description("Short human-readable title for the source.")]
    public string Title { get; set; } = string.Empty;

    [Description("Absolute URL to the source.")]
    public string Url { get; set; } = string.Empty;

    [Description("Short supporting snippet or quote from the source.")]
    public string Snippet { get; set; } = string.Empty;
}