using System;

namespace JustyBase.Models;

public sealed class TableRow
{
    public object[] Fields { get; init; }
    public TableRow()
    {
        Fields = [];
    }
    public override string ToString()
    {
        return EMPTY_NAME_PLACEHOLDED;
    }
    //HACK to make possingle grouping rows by null value in Avalonia data grid
    public const string EMPTY_NAME_PLACEHOLDED = "3F75B4BA-E527-45EF-A900-408FC19F9136";
}
