using System.Text;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;

namespace AI.Agents.AGUI;

#pragma warning disable MAAI001

public sealed class AGUIAIContextProvider(IHttpContextAccessor httpContextAccessor) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext?.Items.TryGetValue(AGUIRequestContext.HttpContextItemKey, out var value) != true
            || value is not AGUIRequestContext requestContext)
        {
            return new(new AIContext());
        }

        var instructions = BuildInstructions(requestContext);
        return new(new AIContext { Instructions = instructions });
    }

    private static string BuildInstructions(AGUIRequestContext requestContext)
    {
        var builder = new StringBuilder();
        builder.AppendLine("The current request includes client-side CopilotKit context.");
        builder.AppendLine("When a structured UI would help, prefer using the render_a2ui tool when it is available.");
        builder.AppendLine("Do not emit raw JSON or raw a2ui_operations containers in a normal assistant text message.");
        builder.AppendLine("If no A2UI render tool is available, fall back to a normal text answer instead of pasting protocol JSON inline.");

        if (requestContext.ContextItems.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Client context items:");

            foreach (var item in requestContext.ContextItems)
            {
                builder.Append("- ");
                builder.AppendLine(item.Description);
                builder.AppendLine(item.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(requestContext.A2UIAction))
        {
            builder.AppendLine();
            builder.AppendLine("Latest client A2UI action:");
            builder.AppendLine(requestContext.A2UIAction);
        }

        return builder.ToString().TrimEnd();
    }
}

#pragma warning restore MAAI001