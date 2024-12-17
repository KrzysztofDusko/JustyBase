using JustyBase.Common.Models;
using JustyBase.PluginCommon.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JustyBase.Common.Contracts;

public interface IGeneralApplicationData : IDatabaseInfo, ISomeEditorOptions, IRuntimeDocumentsContainer
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

    string SelectedTabIdFromStart { get; set; }
    
    bool AddToOrEditLoginData(string name, string database, string driver, string password, string userName, string server);
    void ClearTempSippetsObjects();
    bool DeleteFromLoginData(string name);
    void SaveConfig();
    void SaveCredentials();

    string DownloadPluginsBasePath { get; }

    public static readonly List<string> ADDITIONAL_EXTENSIONS =
    [
        ".xlsb",".xlsx",".xls",".xlsm",".accdb",".mdb",".csv"
    ];

    string GetCurrentCopyVersion();

}