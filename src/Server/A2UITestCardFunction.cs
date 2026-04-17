using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AI.Agents.Server;

internal static class A2UITestCardFunction
{
    private static readonly JsonSerializerOptions A2UIJsonSerializerOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNamingPolicy = null,
    };

    public static AIFunction Create() =>
        AIFunctionFactory.Create(
            GenerateTestCard,
            name: "generate_test_card",
            description: "Generates an A2UI test card with a title and description. Returns A2UI operations to render the card.",
            A2UIJsonSerializerOptions);

    private const string BasicCatalogId = "https://a2ui.org/specification/v0_9/basic_catalog.json";

    private static object GenerateTestCard(
        [Description("Title text displayed as a heading in the card")] string title = "A2UI Test Card",
        [Description("Body text displayed below the title")] string description = "Hello from AI.Agents! This card was rendered via the A2UI declarative UI protocol.")
    {
        var surfaceId = Guid.NewGuid().ToString("N")[..12];

        // v0.9 A2UI operations — createSurface + updateComponents
        object[] a2ui_operations =
        [
            new { createSurface = new { surfaceId, catalogId = BasicCatalogId } },
            new
            {
                updateComponents = new
                {
                    surfaceId,
                    components = new object[]
                    {
                        new { id = "root",    component = "Card",   child    = "body" },
                        new { id = "body",    component = "Column", children = new[] { "heading", "desc" } },
                        new { id = "heading", component = "Text",   text     = title,       variant = "h2" },
                        new { id = "desc",    component = "Text",   text     = description },
                    }
                }
            },
        ];

        return new { a2ui_operations };
    }
}
