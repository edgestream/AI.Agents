using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using System.Reflection;

// Use the actual methods to understand their signatures
var methods = typeof(ChatClientExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);
foreach (var method in methods)
{
    if (method.Name == "AsAIAgent")
    {
        Console.WriteLine($"=== AsAIAgent ===");
        Console.WriteLine($"Namespace: {typeof(ChatClientExtensions).Namespace}");
        var parameters = string.Join(", ", method.GetParameters().Select(p => 
            $"{GetShortTypeName(p.ParameterType)} {p.Name}"));
        Console.WriteLine($"  {GetShortTypeName(method.ReturnType)} AsAIAgent({parameters})");
    }
}

var hostingMethods = typeof(AGUIEndpointRouteBuilderExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);
foreach (var method in hostingMethods)
{
    if (method.Name == "MapAGUI")
    {
        Console.WriteLine($"\n=== MapAGUI ===");
        Console.WriteLine($"Namespace: {typeof(AGUIEndpointRouteBuilderExtensions).Namespace}");
        var parameters = string.Join(", ", method.GetParameters().Select(p => 
            $"{GetShortTypeName(p.ParameterType)} {p.Name}"));
        Console.WriteLine($"  {GetShortTypeName(method.ReturnType)} MapAGUI({parameters})");
    }
}

static string GetShortTypeName(Type t)
{
    if (t.IsGenericType) return t.Name;
    return t.Name;
}
