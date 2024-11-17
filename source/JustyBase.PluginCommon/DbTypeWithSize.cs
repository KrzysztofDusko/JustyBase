﻿using JustyBase.PluginCommon.Enums;

namespace JustyBase.PluginCommon;

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

    public string ToString(DatabaseTypeEnum databaseType)
    {
        return DatabaseTypeSimple switch
        {
            DbSimpleType.Integer => databaseType != DatabaseTypeEnum.Oracle ? "BIGINT" : "INTEGER",
            DbSimpleType.Numeric => databaseType != DatabaseTypeEnum.Oracle
                ? $"NUMERIC({NumericPrecision},{NumericScale})"
                : $"NUMBER ({NumericPrecision},{NumericScale})",
            DbSimpleType.Nvarchar => databaseType != DatabaseTypeEnum.Oracle
                ? $"NVARCHAR({TextLength})"
                : $"VARCHAR2({TextLength})",
            DbSimpleType.Date => "DATE",
            DbSimpleType.TimeStamp => "TIMESTAMP",
            DbSimpleType.NoInfo => databaseType != DatabaseTypeEnum.Oracle
                ? $"NVARCHAR({TextLength})"
                : $"VARCHAR2({TextLength})",
            DbSimpleType.Boolean => "BOOL",
            _ => throw new NotImplementedException()
        };
    }


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