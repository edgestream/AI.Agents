using System;
using System.Reflection;

var dllDir = "src/AI.Web.AGUIServer/bin/Debug/net10.0";

// Load all assemblies with their dependencies
foreach (var dll in System.IO.Directory.GetFiles(dllDir, "*.dll"))
{
    try
    {
        Assembly.LoadFrom(dll);
    }
    catch { }
}

var agentsAssembly = Assembly.LoadFrom(System.IO.Path.Combine(dllDir, "Microsoft.Agents.AI.dll"));

foreach (var type in agentsAssembly.GetTypes())
{
    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
    foreach (var method in methods.Where(m => m.Name.Contains("AsAIAgent")))
    {
        Console.WriteLine($"Found: {type.FullName}.{method.Name}");
    }
}

var hostingAssembly = Assembly.LoadFrom(System.IO.Path.Combine(dllDir, "Microsoft.Agents.AI.Hosting.AGUI.AspNetCore.dll"));

foreach (var type in hostingAssembly.GetTypes())
{
    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
    foreach (var method in methods.Where(m => m.Name.Contains("MapAGUI")))
    {
        Console.WriteLine($"Found: {type.FullName}.{method.Name}");
    }
}
