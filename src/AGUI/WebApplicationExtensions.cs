using AI.Agents.AGUI;
using Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
    public static void UseAGUIRequestMiddleware(this WebApplication app)
    {
        app.UseMiddleware<AGUIRequestContextMiddleware>();
    }  
}