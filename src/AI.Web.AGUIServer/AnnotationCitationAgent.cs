using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer;

internal sealed class AnnotationCitationAgent(
    AIAgent innerAgent,
    ILogger<AnnotationCitationAgent> logger) : DelegatingAIAgent(innerAgent)
{
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<SourceCitation> citations = [];
        bool hasExplicitCitationToolCall = false;

        string? responseId = null;
        string? lastAssistantMessageId = null;
        string? lastAgentId = null;
        string? lastAuthorName = null;

        await foreach (AgentResponseUpdate update in this.InnerAgent
            .RunStreamingAsync(messages, session, options, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            responseId ??= update.ResponseId;
            lastAgentId ??= update.AgentId;
            lastAuthorName ??= update.AuthorName;

            if (update.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(update.MessageId))
            {
                lastAssistantMessageId = update.MessageId;
            }

            if (!hasExplicitCitationToolCall && ContainsCitationToolCall(update))
            {
                hasExplicitCitationToolCall = true;
            }

            CaptureCitations(update, citations);
            yield return update;
        }

        if (hasExplicitCitationToolCall || citations.Count == 0)
        {
            if (hasExplicitCitationToolCall)
            {
                logger.LogDebug("Skipping synthetic citation emission because {ToolName} was already called.", CitationTool.ToolName);
            }

            yield break;
        }

        SourceCitation[] normalizedSources = citations
            .DistinctBy(static citation => citation.Url, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        if (normalizedSources.Length == 0)
        {
            yield break;
        }

        logger.LogDebug("Emitting {Count} synthetic citation(s) for annotated content.", normalizedSources.Length);

        var toolCallId = $"citation_{Guid.NewGuid():N}";
        var toolCallArguments = normalizedSources
            .Select(static source => new Dictionary<string, object?>
            {
                ["title"] = source.Title,
                ["url"] = source.Url,
                ["snippet"] = source.Snippet,
            })
            .ToArray();

        yield return new AgentResponseUpdate(
            ChatRole.Assistant,
            [
                new FunctionCallContent(
                    toolCallId,
                    CitationTool.ToolName,
                    new Dictionary<string, object?>
                    {
                        ["sources"] = toolCallArguments,
                    })
            ])
        {
            AgentId = lastAgentId,
            AuthorName = lastAuthorName,
            CreatedAt = DateTimeOffset.UtcNow,
            MessageId = lastAssistantMessageId ?? $"msg_{toolCallId}",
            ResponseId = responseId,
        };

        yield return new AgentResponseUpdate(
            ChatRole.Tool,
            [new FunctionResultContent(toolCallId, CitationTool.DisplaySources(normalizedSources))])
        {
            AgentId = lastAgentId,
            CreatedAt = DateTimeOffset.UtcNow,
            MessageId = $"result_{toolCallId}",
            ResponseId = responseId,
        };
    }

    private static bool ContainsCitationToolCall(AgentResponseUpdate update)
    {
        return update.Contents.OfType<FunctionCallContent>().Any(static content =>
            string.Equals(content.Name, CitationTool.ToolName, StringComparison.OrdinalIgnoreCase));
    }

    private static void CaptureCitations(AgentResponseUpdate update, List<SourceCitation> citations)
    {
        foreach (AIContent content in update.Contents)
        {
            if (content.Annotations is null)
            {
                continue;
            }

            foreach (AIAnnotation annotation in content.Annotations)
            {
                SourceCitation? citation = TryCreateCitation(annotation, content);
                if (citation is not null)
                {
                    citations.Add(citation);
                }
            }
        }
    }

    private static SourceCitation? TryCreateCitation(AIAnnotation annotation, AIContent content)
    {
        string? url = GetMetadataValue(annotation.AdditionalProperties, "url", "uri")
            ?? GetRawValue(annotation.RawRepresentation, "Url", "Uri");

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? sourceUri))
        {
            return null;
        }

        string title = GetMetadataValue(annotation.AdditionalProperties, "title")
            ?? GetRawValue(annotation.RawRepresentation, "Title")
            ?? sourceUri.Host;

        string snippet = GetMetadataValue(annotation.AdditionalProperties, "snippet", "citedText", "text", "textToReplace")
            ?? GetRawValue(annotation.RawRepresentation, "Snippet", "CitedText", "Text", "TextToReplace")
            ?? ExtractAnnotatedText(annotation, content)
            ?? string.Empty;

        return new SourceCitation
        {
            Title = title.Trim(),
            Url = sourceUri.ToString(),
            Snippet = snippet.Trim(),
        };
    }

    private static string? GetMetadataValue(IDictionary<string, object?>? additionalProperties, params string[] keys)
    {
        if (additionalProperties is null)
        {
            return null;
        }

        foreach (string key in keys)
        {
            if (additionalProperties.TryGetValue(key, out object? value))
            {
                string? stringValue = ToStringValue(value);
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    return stringValue;
                }
            }

            string? alternateKey = additionalProperties.Keys.FirstOrDefault(existingKey =>
                string.Equals(existingKey, key, StringComparison.OrdinalIgnoreCase));
            if (alternateKey is not null && additionalProperties.TryGetValue(alternateKey, out value))
            {
                string? stringValue = ToStringValue(value);
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    return stringValue;
                }
            }
        }

        return null;
    }

    private static string? GetRawValue(object? rawRepresentation, params string[] propertyNames)
    {
        if (rawRepresentation is null)
        {
            return null;
        }

        Type rawType = rawRepresentation.GetType();
        foreach (string propertyName in propertyNames)
        {
            var property = rawType.GetProperty(propertyName);
            if (property is null)
            {
                property = rawType.GetProperties()
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            }

            if (property is null)
            {
                continue;
            }

            string? value = ToStringValue(property.GetValue(rawRepresentation));
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? ExtractAnnotatedText(AIAnnotation annotation, AIContent content)
    {
        if (content is not TextContent textContent || string.IsNullOrWhiteSpace(textContent.Text) || annotation.AnnotatedRegions is null)
        {
            return null;
        }

        foreach (AnnotatedRegion region in annotation.AnnotatedRegions)
        {
            var startProperty = region.GetType().GetProperty("StartIndex");
            var endProperty = region.GetType().GetProperty("EndIndex");

            if (startProperty?.GetValue(region) is not int startIndex || endProperty?.GetValue(region) is not int endIndex)
            {
                continue;
            }

            if (startIndex < 0 || endIndex <= startIndex || endIndex > textContent.Text.Length)
            {
                continue;
            }

            return textContent.Text[startIndex..endIndex];
        }

        return null;
    }

    private static string? ToStringValue(object? value)
    {
        return value switch
        {
            null => null,
            string text => text,
            Uri uri => uri.ToString(),
            _ => value.ToString(),
        };
    }
}