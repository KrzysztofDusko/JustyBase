using JustyBase.PluginCommon.Contracts;
using System.Text.Json.Serialization;

namespace JustyBase.Common.Models;

public sealed class OfflineDocumentContainer
{
    public Dictionary<string, OfflineTabData> SqlOfflineDocumentDictionary { get; set; } = [];
    public string? SelectedTabId { get; set; }
}

[JsonSerializable(typeof(OfflineDocumentContainer))]
public partial class MyJsonContextOfflineDocumentContainer : JsonSerializerContext
{
}

