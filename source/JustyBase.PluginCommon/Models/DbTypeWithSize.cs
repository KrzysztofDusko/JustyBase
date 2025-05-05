using JustyBase.PluginCommon.Enums;

namespace JustyBase.PluginCommon.Models;

public record DbTypeWithSize(DbSimpleType DatabaseTypeSimple)
{
    public int TextLength { get; init; }
    public int NumericPrecision { get; init; }
    public int NumericScale { get; init; }

    public override string ToString()
    {
        return DatabaseTypeSimple switch
        {
            DbSimpleType.Integer => "BIGINT",
            DbSimpleType.Numeric => $"NUMERIC({NumericPrecision},{NumericScale})",
            DbSimpleType.Nvarchar => $"NVARCHAR({TextLength})",
            DbSimpleType.Date => "DATE",
            DbSimpleType.TimeStamp => "TIMESTAMP",
            DbSimpleType.NoInfo => $"NVARCHAR({TextLength})",
            DbSimpleType.Boolean => "BOOL",
            _ => throw new NotImplementedException()
        };
    }

    public string ToString(DatabaseTypeEnum databaseType) => DatabaseTypeSimple switch
    {
        DbSimpleType.Integer => databaseType != DatabaseTypeEnum.Oracle ? "BIGINT" : "INTEGER",
        DbSimpleType.Numeric => databaseType != DatabaseTypeEnum.Oracle
            ? $"NUMERIC({NumericPrecision},{NumericScale})"
            : $"NUMBER ({NumericPrecision},{NumericScale})",
        DbSimpleType.Nvarchar => $"{GetTextTypeName(databaseType)}({TextLength})",
        DbSimpleType.Date => "DATE",
        DbSimpleType.TimeStamp => "TIMESTAMP",
        DbSimpleType.NoInfo => $"{GetTextTypeName(databaseType)}({TextLength})",
        DbSimpleType.Boolean => "BOOL",
        _ => throw new NotImplementedException()
    };

    private static string GetTextTypeName(DatabaseTypeEnum databaseType) => databaseType switch
    {
        DatabaseTypeEnum.NetezzaSQL => "NVARCHAR",
        DatabaseTypeEnum.NetezzaSQLOdbc => "NVARCHAR",
        DatabaseTypeEnum.DB2 => "VARCHAR",
        DatabaseTypeEnum.MsSqlTrusted => "NVARCHAR",
        DatabaseTypeEnum.Oracle => "VARCHAR2",
        DatabaseTypeEnum.Sqlite => "TEXT",
        DatabaseTypeEnum.PostgreSql => "VARCHAR",
        DatabaseTypeEnum.DuckDB => "TEXT",
        DatabaseTypeEnum.MySql => "TEXT",
        _ => throw new NotImplementedException()
    };

    public Type GetNativeType()
    {
        return DatabaseTypeSimple switch
        {
            DbSimpleType.Integer => typeof(long),
            DbSimpleType.Numeric => typeof(decimal),
            DbSimpleType.Nvarchar => typeof(string),
            DbSimpleType.Date => typeof(DateTime),
            DbSimpleType.TimeStamp => typeof(DateTime),
            DbSimpleType.NoInfo => typeof(string),
            DbSimpleType.Boolean => typeof(bool),
            _ => typeof(string)
        };
    }
}