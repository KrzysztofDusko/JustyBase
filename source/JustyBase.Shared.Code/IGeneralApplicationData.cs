using JustyBase.Common.Models;
using JustyBase.Editor;
using JustyBase.PluginDatabaseBase;
using System;
using System.Collections.Generic;

namespace JustyBase;

public interface IGeneralApplicationData : IDatabaseInfo, ISomeEditorOptions
{
    public static readonly string ConfigDirectoryEvo = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\JustDataEvo";
    public static readonly string ColorsPath = $"{ConfigDirectoryEvo}\\colors.json";
    public static readonly string ConfigEvoFile = $"{ConfigDirectoryEvo}\\config.json.enc";
    public static readonly string DataDirectory = $"{ConfigDirectoryEvo}\\data";
    public static readonly string BackupPath = $"{ConfigDirectoryEvo}\\backup";
    public static readonly string MessagesPath = $"{ConfigDirectoryEvo}\\messages";
    public static readonly string StartupPath = $"{ConfigDirectoryEvo}\\simpleStartup.manysql.enc";
    public static readonly string CredentialsPathEvo = $"{ConfigDirectoryEvo}\\credentials.json.enc";
    public static readonly string HistoryDatFilePath = $"{ConfigDirectoryEvo}\\history.dat.zst";
    public static readonly string PluginsDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\JB_PLUGINS";

    AppOptions Config { get; set; }
    Dictionary<string, SavedTabData> DictionaryOfDocuments { get; set; }

    Dictionary<string, (string snippetType, string? Description, string? Text, string? Keyword)> AllSnippets { get; }

    string SelectedTabIdFromStart { get; set; }
    string AddNewDocument(string title);
    bool AddToOrEditLoginData(string name, string database, string driver, string password, string userName, string server);
    void ClearTempSippetsObjects();
    bool DeleteFromLoginData(string name);
    bool IsFileAlreadyOpened(string path);
    void SaveConfig();
    void SaveCredentials();
    void SaveStartupSqlAndFiles(string[] tabsNames, string? selectedTabId = null);
    void SaveStartupSqlAndFilesSpecific(ManySQL mn, string[] tabsNames);

    string DownloadPluginsBasePath { get;}

    public static readonly List<string> ADDITIONAL_EXTENSIONS =
    [
        ".xlsb",".xlsx",".xls",".xlsm",".accdb",".mdb",".csv"
    ];

    protected static string NewDocumentId => $"DOC_ID_{Guid.NewGuid()}";

}