using AI.Agents.Abstractions;
using AI.Agents.Microsoft.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.Agents.Microsoft.Auth;

/// <summary>
/// Middleware that blocks unauthenticated requests to the agent endpoint
/// when <see cref="AgentAccessSettings.RequireAuthenticationForAgent"/> is enabled.
/// </summary>
public sealed class AgentAccessMiddleware(RequestDelegate next, IOptions<AgentAccessSettings> settings, ILogger<AgentAccessMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IOptions<AgentAccessSettings> _settings = settings;
    private readonly ILogger<AgentAccessMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        if (_settings.Value.RequireAuthenticationForAgent
            && HttpMethods.IsPost(context.Request.Method)
            && IsAgentEndpoint(context.Request.Path))
        {
            var userContext = context.Items[typeof(IUserContext)] as IUserContext;
            if (userContext is null || !userContext.IsAuthenticated)
            {
                _logger.LogWarning(
                    "Agent access denied for unauthenticated user. " +
                    "RequireAuthenticationForAgent is enabled.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Authentication required. Please sign in to use the agent."
                });
                return;
            }
        }

        await _next(context);
    }

    private static bool IsAgentEndpoint(PathString path)
    {
        // The AGUI endpoint is mapped at root ("/").
        // In local dev the frontend proxies via /api/copilotkit to backend /.
        return path == "/";
    }
}
