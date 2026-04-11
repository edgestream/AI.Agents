using AI.AGUI.Hosting;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for mapping AGUI endpoints and the application catalog.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps a route for every registered AGUI application and exposes the
    /// <c>GET /applications</c> catalog discovery endpoint.
    /// </summary>
    /// <remarks>
    /// Every <see cref="ApplicationCatalogEntry"/> registered via
    /// <see cref="Microsoft.Extensions.Hosting.AGUIHostingBuilderExtensions.AddAGUIApplication"/>
    /// is mapped to <c>POST /agents/{id}</c>.
    /// The catalog endpoint returns a JSON array of <see cref="ApplicationCatalogEntry"/> objects.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown at startup when no AGUI application has been registered.
    /// </exception>
    public static WebApplication MapAGUI(this WebApplication app)
    {
        var entries = app.Services.GetServices<ApplicationCatalogEntry>().ToList();

        if (entries.Count == 0)
            throw new InvalidOperationException(
                "No AGUI application has been registered. " +
                "Call AddAGUIApplication before calling MapAGUI.");

        foreach (var entry in entries)
        {
            var agent = app.Services.GetRequiredKeyedService<AIAgent>(entry.Id);
            app.MapAGUI(entry.Route, agent);
        }

        // Catalog discovery endpoint consumed by the frontend.
        app.MapGet("/applications", (IEnumerable<ApplicationCatalogEntry> entries) =>
            Results.Ok(entries.ToList()));

        return app;
    }
}
