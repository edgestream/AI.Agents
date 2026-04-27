using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace AI.Agents.AGUI;

public sealed class AGUIRequestContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (!ShouldInspect(httpContext.Request))
        {
            await next(httpContext);
            return;
        }

        httpContext.Request.EnableBuffering();

        try
        {
            using var document = await JsonDocument.ParseAsync(
                httpContext.Request.Body,
                cancellationToken: httpContext.RequestAborted);

            var requestContext = Parse(document.RootElement);
            if (requestContext is not null)
            {
                httpContext.Items[AGUIRequestContext.HttpContextItemKey] = requestContext;
            }
        }
        catch (JsonException)
        {
        }
        finally
        {
            httpContext.Request.Body.Position = 0;
        }

        await next(httpContext);
    }

    private static bool ShouldInspect(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method)
            && request.Path == "/"
            && request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static AGUIRequestContext? Parse(JsonElement root)
    {
        var contextItems = new List<AGUIContextItem>();

        if (root.TryGetProperty("context", out var contextElement)
            && contextElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in contextElement.EnumerateArray())
            {
                if (!item.TryGetProperty("description", out var descriptionElement)
                    || descriptionElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (!item.TryGetProperty("value", out var valueElement))
                {
                    continue;
                }

                var description = descriptionElement.GetString();
                var value = valueElement.ValueKind == JsonValueKind.String
                    ? valueElement.GetString()
                    : valueElement.GetRawText();

                if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                contextItems.Add(new(description, value));
            }
        }

        string? a2uiAction = null;

        if (root.TryGetProperty("forwardedProps", out var forwardedPropsElement)
            && forwardedPropsElement.ValueKind == JsonValueKind.Object
            && forwardedPropsElement.TryGetProperty("a2uiAction", out var a2uiActionElement))
        {
            a2uiAction = a2uiActionElement.GetRawText();
        }

        return contextItems.Count == 0 && string.IsNullOrWhiteSpace(a2uiAction)
            ? null
            : new AGUIRequestContext(contextItems, a2uiAction);
    }
}