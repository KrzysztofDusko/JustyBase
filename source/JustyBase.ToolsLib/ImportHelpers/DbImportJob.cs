using JustyBase.PluginCommon;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Extensions;
using JustyBase.Tools.ImportHelpers;
using JustyBase.Tools.ImportHelpers.XML;
using System.Data;
using System.Globalization;

namespace JustyBase.Tools.Import;
public class DbImportJob : IDbImportJob
{
    public DbImportJob(IDataReader rdr, DatabaseTypeChooser typeChooser)
    {
        AsReader = rdr;
        _databaseTypeChoser = typeChooser;
        _columnHeadersNames = _databaseTypeChoser.NormalizedColumnHeaderNames;
    }

    public DbImportJob() { }

    public long RowsCount => _databaseTypeChoser.RowsCount;

    protected string[]? _columnHeadersNames = null;
    public string[]? ColumnHeadersNames => _columnHeadersNames;
    public DbTypeWithSize[] ColumnTypesBestMatch => _databaseTypeChoser?.ColumnTypesBestMatch;
    public List<string[]>? PreviewRows => _databaseTypeChoser?.PreviewRows;

    protected static readonly CultureInfo _cultureUS = CultureInfo.CreateSpecificCulture("en-US");
    protected readonly DatabaseTypeChooser _databaseTypeChoser = new DatabaseTypeChooser();

    //results 

    protected OneCellValue[][]? _linesX = null;

    public IDataReader AsReader { get; set; }

    public string[] ReturnHeadersWithDataTypes(DatabaseTypeEnum databaseType = DatabaseTypeEnum.NetezzaSQL)
    {
        StringExtension2.DeDuplicate(ColumnHeadersNames);
        string[] res = new string[ColumnHeadersNames.Length];
        for (int i = 0; i < ColumnHeadersNames.Length; i++)
        {
            res[i] = ColumnHeadersNames[i] + " " + ColumnTypesBestMatch[i].ToString(databaseType);
        }
        return res;
    }
}

