using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JustyBase.Common.Models;
public sealed class SqlTabData
{
    public required string MyId { get; set; }
    public required string Title { get; set; }
    public required string SqlText { get; set; }
    public required string SqlFilePath { get; set; }
    public int ConnectionIndex { get; set; } = -1;
    public int FontSize { get; set; } = DEFAULT_DOCUMENT_FONT_SIZE;

    public const int DEFAULT_DOCUMENT_FONT_SIZE = 13;
}

public sealed class ManySQL
{
    public Dictionary<string, SqlTabData> SqlTabDataDictionary { get; set; } = new();
    public string SelectedTabId { get; set; }
}

[JsonSerializable(typeof(ManySQL))]
public partial class MyJsonContextManySQL : JsonSerializerContext
{
}

