using System;
using System.Reflection;
using System.Linq;
using System.IO;

var dllDir = "../src/AI.Web.AGUIServer/bin/Debug/net10.0";

// Load the extension type from the Agents.AI assembly
var agentsAssembly = Assembly.LoadFrom(Path.Combine(dllDir, "Microsoft.Agents.AI.dll"));
var chatClientExtensionsType = agentsAssembly.GetType("Microsoft.Agents.AI.ChatClientExtensions");

if (chatClientExtensionsType != null)
{
    Console.WriteLine("=== ChatClientExtensions (Microsoft.Agents.AI) ===");
    var asAIAgentMethod = chatClientExtensionsType.GetMethod("AsAIAgent", BindingFlags.Static | BindingFlags.Public);
    if (asAIAgentMethod != null)
    {
        var returnType = asAIAgentMethod.ReturnType;
        var parameters = string.Join(", ", asAIAgentMethod.GetParameters().Select(p => 
            $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"Namespace: {chatClientExtensionsType.Namespace}");
        Console.WriteLine($"Method signature: {returnType.Name} AsAIAgent({parameters})");
    }
}

var hostingAssembly = Assembly.LoadFrom(Path.Combine(dllDir, "Microsoft.Agents.AI.Hosting.AGUI.AspNetCore.dll"));
var agUIExtensionsType = hostingAssembly.GetType("Microsoft.Agents.AI.Hosting.AGUI.AspNetCore.AGUIEndpointRouteBuilderExtensions");

if (agUIExtensionsType != null)
{
    Console.WriteLine("\n=== AGUIEndpointRouteBuilderExtensions ===");
    var mapAGUIMethod = agUIExtensionsType.GetMethod("MapAGUI", BindingFlags.Static | BindingFlags.Public);
    if (mapAGUIMethod != null)
    {
        var returnType = mapAGUIMethod.ReturnType;
        var parameters = string.Join(", ", mapAGUIMethod.GetParameters().Select(p => 
            $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"Namespace: {agUIExtensionsType.Namespace}");
        Console.WriteLine($"Method signature: {returnType.Name} MapAGUI({parameters})");
    }
}
