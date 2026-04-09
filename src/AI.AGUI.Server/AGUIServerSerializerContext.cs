using System.Text.Json.Serialization;

namespace AI.AGUI.Server;

/// <summary>
/// Source-generated serializer context for the AGUI Server.
/// Intentionally omits <c>PropertyNamingPolicy</c> so that C# property names
/// are emitted as-is (PascalCase) — required by the A2UI renderer for component
/// type discriminators such as <c>Column</c>, <c>Text</c>, and <c>List</c>.
/// Pass <see cref="Default"/>.<see cref="JsonSerializerContext.Options"/> to
/// <c>AIFunctionFactory.Create()</c> for any tool that returns A2UI operations.
/// </summary>
[JsonSerializable(typeof(object[]))]
internal sealed partial class AGUIServerSerializerContext : JsonSerializerContext;
