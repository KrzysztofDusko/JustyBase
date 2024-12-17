namespace JustyBase.PluginCommon.Models;

public record DatabaseColumn
{
    public string Name { get; init; }
    public string? Desc { get; init; }
    public string FullTypeName { get; init; }
    public bool ColumnNotNull { get; init; }
    public string? COLDEFAULT { get; init; }
    public DatabaseColumn(string name, string? desc, string fullTypeName, bool notNull, string? colDef)
    {
        Name = name;
        Desc = desc;
        FullTypeName = fullTypeName;
        ColumnNotNull = notNull;
        COLDEFAULT = colDef;
    }
};