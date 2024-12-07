using System.Text.Json.Serialization;

namespace JustyBase.Common.Models;

public sealed class SnippetsModel
{
    public required string[] Keywords { get; set; }
    public required string[] Snippets { get; set; }
    public required string[] MonkeySnippets { get; set; }
}

[JsonSerializable(typeof(SnippetsModel))]
internal partial class MyJsonContextSnipety : JsonSerializerContext
{
}
