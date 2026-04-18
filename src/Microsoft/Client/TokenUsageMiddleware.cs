using Microsoft.AspNetCore.Http;

namespace AI.Agents.Microsoft.Client;

/// <summary>
/// Middleware that adds token usage information to HTTP response headers.
/// </summary>
public sealed class TokenUsageMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenUsageMiddleware"/>.
    /// </summary>
    public TokenUsageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // After the request completes, check if there's usage data available
        var usage = TokenUsageTrackingChatClient.CurrentUsage;
        if (usage != null)
        {
            if (usage.InputTokenCount.HasValue)
            {
                context.Response.Headers["X-Token-Usage-Input"] = usage.InputTokenCount.Value.ToString();
            }
            
            if (usage.OutputTokenCount.HasValue)
            {
                context.Response.Headers["X-Token-Usage-Output"] = usage.OutputTokenCount.Value.ToString();
            }
            
            if (usage.TotalTokenCount.HasValue)
            {
                context.Response.Headers["X-Token-Usage-Total"] = usage.TotalTokenCount.Value.ToString();
            }
        }
    }
}
