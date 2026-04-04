using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer;

/// <summary>
/// <see cref="IChatClient"/> middleware that translates <see cref="AIAnnotation"/>
/// metadata on <see cref="TextContent"/> into markdown footnotes appended to the
/// assistant's streamed response.
/// </summary>
/// <remarks>
/// The AG-UI SSE adapter only forwards <see cref="TextContent.Text"/> to the frontend;
/// it ignores all annotation metadata. This middleware:
/// <list type="number">
///   <item>Passes every <see cref="ChatResponseUpdate"/> through immediately (preserving streaming UX).</item>
///   <item>Collects <see cref="AIAnnotation"/> citations as they arrive.</item>
///   <item>After the inner stream ends, yields one final text update containing a
///         markdown "Sources" section with numbered reference links.</item>
/// </list>
/// Because the transform operates at the <see cref="IChatClient"/> level it plugs into
/// any agent without wrapping or special tool instructions.
/// </remarks>
internal sealed class CitationMiddleware(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<Citation> citations = [];
        string? lastMessageId = null;
        string? lastResponseId = null;

        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken)
            .ConfigureAwait(false))
        {
            lastMessageId ??= update.MessageId;
            lastResponseId ??= update.ResponseId;

            CollectCitations(update, citations);
            yield return update;
        }

        if (citations.Count == 0)
        {
            yield break;
        }

        IReadOnlyList<Citation> unique = citations
            .DistinctBy(static c => c.Url, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unique.Count == 0)
        {
            yield break;
        }

        yield return new ChatResponseUpdate(ChatRole.Assistant, BuildFootnotes(unique))
        {
            MessageId = lastMessageId,
            ResponseId = lastResponseId,
        };
    }

    private static void CollectCitations(ChatResponseUpdate update, List<Citation> citations)
    {
        foreach (AIContent content in update.Contents)
        {
            if (content.Annotations is null)
            {
                continue;
            }

            foreach (AIAnnotation annotation in content.Annotations)
            {
                Citation? citation = TryExtractCitation(annotation, content);
                if (citation is not null)
                {
                    citations.Add(citation);
                }
            }
        }
    }

    private static Citation? TryExtractCitation(AIAnnotation annotation, AIContent content)
    {
        string? url = GetValue(annotation.AdditionalProperties, "url", "uri")
                   ?? GetReflected(annotation.RawRepresentation, "Url", "Uri");

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return null;
        }

        string title = GetValue(annotation.AdditionalProperties, "title")
                    ?? GetReflected(annotation.RawRepresentation, "Title")
                    ?? uri.Host;

        string snippet = GetValue(annotation.AdditionalProperties, "snippet", "citedText", "text", "textToReplace")
                      ?? GetReflected(annotation.RawRepresentation, "Snippet", "CitedText", "Text", "TextToReplace")
                      ?? ExtractSpanText(annotation, content)
                      ?? string.Empty;

        return new Citation(title.Trim(), uri.ToString(), snippet.Trim());
    }

    private static string BuildFootnotes(IReadOnlyList<Citation> citations)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("**Sources:**");

        for (int i = 0; i < citations.Count; i++)
        {
            Citation c = citations[i];
            string line = string.IsNullOrWhiteSpace(c.Snippet)
                ? $"{i + 1}. [{c.Title}]({c.Url})"
                : $"{i + 1}. [{c.Title}]({c.Url}) — {c.Snippet}";
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    #region Metadata extraction helpers

    private static string? GetValue(IDictionary<string, object?>? properties, params string[] keys)
    {
        if (properties is null) return null;

        foreach (string key in keys)
        {
            if (properties.TryGetValue(key, out object? value))
            {
                string? s = AsString(value);
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }

            string? alt = properties.Keys.FirstOrDefault(k =>
                string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            if (alt is not null && properties.TryGetValue(alt, out value))
            {
                string? s = AsString(value);
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }

        return null;
    }

    private static string? GetReflected(object? raw, params string[] propertyNames)
    {
        if (raw is null) return null;

        Type type = raw.GetType();
        foreach (string name in propertyNames)
        {
            var prop = type.GetProperty(name)
                    ?? type.GetProperties().FirstOrDefault(p =>
                           string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (prop is null) continue;

            string? s = AsString(prop.GetValue(raw));
            if (!string.IsNullOrWhiteSpace(s)) return s;
        }

        return null;
    }

    private static string? ExtractSpanText(AIAnnotation annotation, AIContent content)
    {
        if (content is not TextContent textContent
            || string.IsNullOrWhiteSpace(textContent.Text)
            || annotation.AnnotatedRegions is null)
        {
            return null;
        }

        foreach (AnnotatedRegion region in annotation.AnnotatedRegions)
        {
            var startProp = region.GetType().GetProperty("StartIndex");
            var endProp = region.GetType().GetProperty("EndIndex");

            if (startProp?.GetValue(region) is not int start || endProp?.GetValue(region) is not int end)
                continue;

            if (start < 0 || end <= start || end > textContent.Text.Length)
                continue;

            return textContent.Text[start..end];
        }

        return null;
    }

    private static string? AsString(object? value) => value switch
    {
        null => null,
        string s => s,
        Uri u => u.ToString(),
        _ => value.ToString(),
    };

    #endregion

    internal sealed record Citation(string Title, string Url, string Snippet);
}
