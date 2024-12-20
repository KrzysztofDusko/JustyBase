using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using JustyBase.Helpers.Interactions;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.ViewModels.Tools;
using JustyBase.Common.Contracts;
using JustyBase.ViewModels.Documents;
using JustyBase.PluginCommon.Contracts;
using JustyBase.Common.Models;
using JustyBase.PluginCommons;

namespace JustyBase.Shared.Helpers;

internal static partial class SqlDocumentViewModelHelper
{
    public const string CurrentOptionsListDROP = "Drop";
    public const string CurrentOptionsListDDL = "Ddl";
    public const string CurrentOptionsListRECREATE = "Recreate";
    public const string CurrentOptionsListRENAME = "Rename";
    public const string CurrentOptionsListJUMP_TO = "Jump to";
    public const string CurrentOptionsListCREATE_FROM = "Create from";
    public const string CurrentOptionsListGROOM = "Groom";
    public const string CurrentOptionsListSELECT = "Select";

    public static bool NotSupportedFileExtension(string path)
    {
        return !path.EndsWithAny([".xlsb", ".xlsx", ".csv", ".csv.br", ".dat.br", ".csv.gz", ".dat.gz", ".csv.zst", ".dat.zst"]);
    }

    public static readonly Dictionary<string, string> KnownParams = [];
    public static List<string> GetVariableValuesP1(string query)
    {
        var toAsk = new List<string>();
        foreach (Match match in rxParam.Matches(query.CreateCleanSql()).Cast<Match>())
        {
            var variableTxt = match.Groups["param"].Value.ToUpper();
            if (!toAsk.Contains(variableTxt))
            {
                toAsk.Add(variableTxt);
                KnownParams.TryAdd(variableTxt, "");
            }
        }
        return toAsk;
    }


    public static List<string> ConvertSqlTextToListOfSqls(bool singleCommandLocal, string query)
    {
        List<string> sqls;
        if (singleCommandLocal)
        {
            sqls = [query];
        }
        else
        {
            sqls = query.MySplitForSqlSplit(';');
        }

        return sqls;
    }
    private static readonly char[] _newLiness = ['\r', '\n'];
    public static int? FindForcedTimeout(string query)
    {
        int? FORCED_TIMEOUT = null;
        var i1 = query.IndexOf(DatabaseService.TIMEOUT_OVERRIDE) + DatabaseService.TIMEOUT_OVERRIDE.Length + 1;
        if (i1 < query.Length - 1)
        {
            var i2 = query.IndexOfAny(_newLiness, i1);
            if (i1 != -1 && i2 > i1)
            {
                string timeoutTxt = query[(i1)..i2].Trim();
                if (int.TryParse(timeoutTxt, out var forcedTimeout))
                {
                    FORCED_TIMEOUT = forcedTimeout;
                }
            }
        }

        return FORCED_TIMEOUT;
    }


    [GeneratedRegex("(?<exportName>((___)|@)expCsv|((___)|@)expXlsx): (?<sql>.*)[\\s\\r\\n\\t]+->[\\s\\r\\n\\t]+(?<filePath>([-zżźćńółęąśa-z0-9\\\\:_\\.\\s]*\\.(xlsx|xlsb|dat|justData|parquet|[a-z]{3,4})|nul))([\\s\\r\\n\\t]+{[\\s\\r\\n\\t]+(?<options>.*)[\\s\\r\\n\\t]+})?", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex rxExportRegexGen();
    public static readonly Regex rxExportCsvXlsx = rxExportRegexGen();

    [GeneratedRegex(@"^\s*declare\s+(?<sessionVar>&[a-z]{1}[a-z_\d]*)\s*=\s*(?<sessionValue>[^;]+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex rxSessionVariableDefineGen();
    public static readonly Regex RxSessionVariableDefine = rxSessionVariableDefineGen();

    [GeneratedRegex(@"(?<param>\$[a-zA-Z]{1}[a-zA-Z_\d]*)", RegexOptions.CultureInvariant)]
    private static partial Regex rxParamGen();
    public static readonly Regex rxParam = rxParamGen();

    public static readonly Regex DatabaseSchemaTableRegex = new(@"(((?<part1>\w+)\.)?(?<part2>\w*)\.)?(?<part3>\w+)");

    public static readonly Regex SleepRegex = new(@"(^|(\r\n)+)@sleep: (?<num>\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static readonly Regex ExtractRegex = new(@"(^|(\r\n)+)@extract: (?<path>.+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static readonly Regex CompressRegex = new(@"(^|(\r\n)+)@compress: (?<path>.+) (?<mode>\w+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static readonly Regex ChangeConnectionRegex = new(@"(^|(\r\n)+)@change_connection: (?<connectionName>\w+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static void SetConnectionList(bool force = false)
    {
        var generalApplicationData = App.GetRequiredService<IGeneralApplicationData>();
        var messageForUserTools = App.GetRequiredService<IMessageForUserTools>();
        var simpleLogger = App.GetRequiredService<ISimpleLogger>();
        if (_connectionsList is null || force)
        {
            _connectionsList ??= new();
            _connectionsList.Clear();
            foreach (var (item, value) in generalApplicationData.LoginDataDic)
            {
                DatabaseTypeEnum type = DatabaseServiceHelpers.StringToDatabaseTypeEnum(value.Driver);

                var conItem = new ConnectionItem(item, type)
                {
                    DefaultDatabase = value.Database,
                    DatabaseList = new ObservableCollection<string>()
                };
                if (!string.IsNullOrWhiteSpace(value.Database))
                {
                    conItem.DefaultDatabase = value.Database;
                    conItem.DatabaseList.Add(value.Database);
                }
                _connectionsList.Add(conItem);
            }
        }
        try
        {
            foreach (var item in generalApplicationData.GetDocumentsKeyValueCollection())
            {
                item.Value?.HotDocumentViewModelAsT<SqlDocumentViewModel>()?.RefreshConnectionList();
            }
        }
        catch (Exception ex1)
        {
            messageForUserTools.ShowSimpleMessageBoxInstance(ex1);
            simpleLogger.TrackError(ex1, isCrash: false);
        }
    }

    private static ObservableCollection<ConnectionItem> _connectionsList;
    public static ObservableCollection<ConnectionItem> ConnectionsList => _connectionsList;

    public static int GetConnectionIndex(ReadOnlySpan<char> word)
    {
        for (int i = 0; i < _connectionsList.Count; i++)
        {
            ConnectionItem item = _connectionsList[i];
            if (item.Name.AsSpan().Contains(word,StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }


    public static DbConnection OpenConnectionIfNeeded(IDatabaseService actualDatabaseService, DbConnection con, ISimpleLogger simpleLogger)
    {
        try
        {
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
        }
        catch (Exception ex1)
        {
            simpleLogger.TrackError(ex1, isCrash: false);
            if (ex1.Message == "Timeout while getting a connection from pool." || ex1.Message == "The Connection is broken.")
            {
                con = actualDatabaseService.GetConnection(null, pooling: false);
                con.Open();
            }
        }

        return con;
    }

}
