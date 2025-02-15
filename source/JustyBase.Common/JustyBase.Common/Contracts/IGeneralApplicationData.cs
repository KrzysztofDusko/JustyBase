using JustyBase.PluginCommon.Contracts;

namespace JustyBase.Common.Contracts;

public interface IGeneralApplicationData : IDatabaseInfo, ISomeEditorOptions, IRuntimeDocumentsContainer
{
    static readonly string ConfigDirectoryEvo = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\JustDataEvo";
    static readonly string ColorsPath = $"{ConfigDirectoryEvo}\\colors.json";
    static readonly string ConfigEvoFile = $"{ConfigDirectoryEvo}\\config.json.enc";
    static readonly string DataDirectory = $"{ConfigDirectoryEvo}\\data";
    static readonly string BackupPath = $"{ConfigDirectoryEvo}\\backup";
    static readonly string MessagesPath = $"{ConfigDirectoryEvo}\\messages";
    static readonly string StartupPath = $"{ConfigDirectoryEvo}\\simpleStartup.manysql.enc";
    static readonly string CredentialsPathEvo = $"{ConfigDirectoryEvo}\\credentials.json.enc";
    static readonly string HistoryDatFilePath = $"{ConfigDirectoryEvo}\\history.dat.zst";
    static readonly string PluginsDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\JB_PLUGINS";

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