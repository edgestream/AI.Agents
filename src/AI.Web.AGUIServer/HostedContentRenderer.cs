// These types are experimental in Microsoft.Extensions.AI but stable enough for our purposes.
// Suppress MEAI001 for the entire file so new content types can be added without per-site suppressions.
#pragma warning disable MEAI001
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;

namespace AI.Web.AGUIServer;

/// <summary>
/// A <see cref="DelegatingChatClient"/> middleware that converts hosted-service content types
/// (such as <see cref="WebSearchToolCallContent"/> and <see cref="WebSearchToolResultContent"/>)
/// into <see cref="TextContent"/> with Markdown formatting so they are visualized
/// in CopilotKit alongside regular text responses.
/// </summary>
/// <remarks>
/// The AG-UI protocol and CopilotKit only surface <see cref="TextContent"/> and function-call
/// content in the chat UI. Any other <see cref="AIContent"/> subclass returned by the LLM
/// (e.g. web-search results, code-interpreter output) would otherwise be silently discarded
/// before reaching the browser. This middleware intercepts those items and replaces them with
/// an equivalent <see cref="TextContent"/> containing a Markdown rendering of the data.
/// </remarks>
internal sealed class HostedContentRenderer(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(chatMessages, options, cancellationToken);
        foreach (var message in response.Messages)
            message.Contents = [.. message.Contents.SelectMany(Render)];
        return response;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
        {
            if (update.Contents is { Count: > 0 })
                update.Contents = [.. update.Contents.SelectMany(Render)];

            yield return update;
        }
    }

    // ---------------------------------------------------------------------------
    // Content dispatch
    // ---------------------------------------------------------------------------

    internal static IEnumerable<AIContent> Render(AIContent content) =>
        content switch
        {
            TextContent => [content],
            FunctionCallContent => [content],
            FunctionResultContent => [content],
            WebSearchToolCallContent webSearch => RenderWebSearchCall(webSearch),
            WebSearchToolResultContent webResult => RenderWebSearchResult(webResult),
            CodeInterpreterToolCallContent codeCall => RenderCodeInterpreterCall(codeCall),
            CodeInterpreterToolResultContent codeResult => RenderCodeInterpreterResult(codeResult),
            ImageGenerationToolResultContent imageResult => RenderImageGenerationResult(imageResult),
            McpServerToolCallContent mcpCall => RenderMcpServerToolCall(mcpCall),
            McpServerToolResultContent mcpResult => RenderMcpServerToolResult(mcpResult),
            ErrorContent error => [new TextContent($"\u26a0\ufe0f **Error**: {error.Message}\n\n")],
            _ => []
        };

    // ---------------------------------------------------------------------------
    // Web search
    // ---------------------------------------------------------------------------

    private static IEnumerable<AIContent> RenderWebSearchCall(WebSearchToolCallContent webSearch)
    {
        if (webSearch.Queries is not { Count: > 0 })
            yield break;

        var queries = string.Join(", ", webSearch.Queries.Select(q => $"\"{q}\""));
        yield return new TextContent($"\ud83d\udd0d **Web Search**: {queries}\n\n");
    }

    private static IEnumerable<AIContent> RenderWebSearchResult(WebSearchToolResultContent webResult)
    {
        if (webResult.Results is not { Count: > 0 })
            yield break;

        var sb = new StringBuilder();
        sb.AppendLine("\ud83c\udf10 **Search Results**:");
        foreach (var result in webResult.Results)
        {
            switch (result)
            {
                case UriContent uri:
                    var title = uri.AdditionalProperties?.TryGetValue("title", out var t) == true
                        ? t?.ToString() ?? uri.Uri.ToString()
                        : uri.Uri.ToString();
                    sb.AppendLine($"- [{title}]({uri.Uri})");
                    break;
                case TextContent text when !string.IsNullOrWhiteSpace(text.Text):
                    sb.AppendLine($"- {text.Text}");
                    break;
            }
        }
        sb.AppendLine();
        yield return new TextContent(sb.ToString());
    }

    // ---------------------------------------------------------------------------
    // Code interpreter
    // ---------------------------------------------------------------------------

    private static IEnumerable<AIContent> RenderCodeInterpreterCall(CodeInterpreterToolCallContent codeCall)
    {
        if (codeCall.Inputs is not { Count: > 0 })
            yield break;

        foreach (var input in codeCall.Inputs)
        {
            if (input is DataContent data && data.MediaType == "text/x-python" && data.Data.Length > 0)
            {
                var code = Encoding.UTF8.GetString(data.Data.Span);
                yield return new TextContent($"\ud83d\udcbb **Code Execution**:\n```python\n{code}\n```\n\n");
            }
        }
    }

    private static IEnumerable<AIContent> RenderCodeInterpreterResult(CodeInterpreterToolResultContent codeResult)
    {
        if (codeResult.Outputs is not { Count: > 0 })
            yield break;

        var sb = new StringBuilder();
        sb.AppendLine("\ud83d\udcca **Execution Output**:");
        sb.AppendLine("```");
        foreach (var output in codeResult.Outputs)
        {
            switch (output)
            {
                case TextContent text:
                    sb.Append(text.Text);
                    break;
                case UriContent uri:
                    sb.AppendLine($"[Output File]({uri.Uri})");
                    break;
            }
        }
        sb.AppendLine("```\n");
        yield return new TextContent(sb.ToString());
    }

    // ---------------------------------------------------------------------------
    // Image generation
    // ---------------------------------------------------------------------------

    private static IEnumerable<AIContent> RenderImageGenerationResult(ImageGenerationToolResultContent imageResult)
    {
        if (imageResult.Outputs is not { Count: > 0 })
            yield break;

        foreach (var output in imageResult.Outputs)
        {
            if (output is UriContent uri)
            {
                var alt = uri.AdditionalProperties?.TryGetValue("alt", out var a) == true
                    ? a?.ToString() ?? "Generated image"
                    : "Generated image";

                var text = uri.HasTopLevelMediaType("image")
                    ? $"\n![{alt}]({uri.Uri})\n\n"
                    : $"\n[Generated File]({uri.Uri})\n\n";

                yield return new TextContent(text);
            }
        }
    }

    // ---------------------------------------------------------------------------
    // MCP server tools
    // ---------------------------------------------------------------------------

    private static IEnumerable<AIContent> RenderMcpServerToolCall(McpServerToolCallContent mcpCall)
    {
        var server = string.IsNullOrEmpty(mcpCall.ServerName) ? string.Empty : $" ({mcpCall.ServerName})";
        yield return new TextContent($"\u2699\ufe0f **MCP Tool Call**: `{mcpCall.Name}`{server}\n\n");
    }

    private static IEnumerable<AIContent> RenderMcpServerToolResult(McpServerToolResultContent mcpResult)
    {
        if (mcpResult.Outputs is not { Count: > 0 })
            yield break;

        var sb = new StringBuilder();
        sb.AppendLine("\ud83d\udd27 **MCP Tool Result**:");
        sb.AppendLine("```");
        foreach (var output in mcpResult.Outputs)
        {
            switch (output)
            {
                case TextContent text:
                    sb.Append(text.Text);
                    break;
                case UriContent uri:
                    sb.AppendLine($"[{uri.Uri}]({uri.Uri})");
                    break;
            }
        }
        sb.AppendLine("```\n");
        yield return new TextContent(sb.ToString());
    }
}
