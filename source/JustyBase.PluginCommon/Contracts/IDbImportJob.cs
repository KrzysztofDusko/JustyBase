﻿using System.Data;
using JustyBase.PluginCommon.Enums;

namespace JustyBase.PluginCommon.Contracts;

public interface IDbImportJob
{
    IDataReader AsReader { get; set; }
    string[]? ColumnHeadersNames { get; }
    DbTypeWithSize[] ColumnTypesBestMatch { get; }
    List<string[]>? PreviewRows { get; }
    long RowsCount { get; }
    string[] ReturnHeadersWithDataTypes(DatabaseTypeEnum databaseType = DatabaseTypeEnum.NetezzaSQL);
}