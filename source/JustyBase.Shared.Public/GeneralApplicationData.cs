using JustyBase.Common;
using JustyBase.Common.Contracts;
using JustyBase.Common.Helpers;
using JustyBase.Common.Models;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Models;
using JustyBase.PluginDatabaseBase;
using JustyBase.PluginDatabaseBase.Database;
using NetezzaOdbcPlugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace JustyBase;
public class GeneralApplicationData : IGeneralApplicationData
{
    private readonly ISimpleLogger _simpleLogger;
    private readonly IMessageForUserTools _messageForUserTools;
    private readonly OtherHelpers _otherHelpers;
    private readonly IEncryptionHelper _encryptionHelper;
    public ISimpleLogger GlobalLoggerObject => _simpleLogger;


    private static bool _pluginWasLoaded = false;
    public async Task LoadPluginsIfNeeded(Action? uiAction)
    {
        if (!_pluginWasLoaded && Config.AllowToLoadPlugins)
        {
            if (!Directory.Exists(IGeneralApplicationData.PluginsDirectory) && Config.AutoDownloadPlugins && !string.IsNullOrWhiteSpace(DownloadPluginsBasePath))
            {
                uiAction?.Invoke();
                await _otherHelpers.DownloadAllPlugins(IGeneralApplicationData.PluginsDirectory, DownloadPluginsBasePath);
                uiAction?.Invoke();
            }
            try
            {

#if DEBUG
                PluginLoadHelper.LoadPlugins(Environment.GetEnvironmentVariable("DEBUG_PLUGIN_BASE_PATH"));
#else
                PluginLoadHelper.LoadPlugins(IGeneralApplicationData.PluginsDirectory);
#endif
                //_pluginWasLoaded = true;
            }
            catch (Exception ex)
            {
                _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                //Config.ResetPlugins = true;
            }
            finally
            {
                _pluginWasLoaded = true;
            }
        }
    }

    private Dictionary<string, LoginDataModel> _loginTmp;

    public Dictionary<string, LoginDataModel> LoginDataDic => _loginTmp ??= GenerateLoginDic();

    private Dictionary<string, LoginDataModel> GenerateLoginDic()
    {
        _loginTmp = new Dictionary<string, LoginDataModel>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(IGeneralApplicationData.CredentialsPathEvo))
        {
            try
            {
                string plainText = _encryptionHelper.GetEncodedContentOfTextFile(IGeneralApplicationData.CredentialsPathEvo);
                List<LoginDataModel> credentialsList = JsonSerializer.Deserialize(plainText, MyJsonContextLoginDataModelList.Default.ListLoginDataModel) ?? [];
                foreach (LoginDataModel credentailItem in credentialsList)
                {
                    _loginTmp[credentailItem.ConnectionName.ToUpper()] = credentailItem;
                }
            }
            catch (Exception ex)
            {
                _simpleLogger.TrackError(ex, isCrash: false);
            }
        }
        else
        {
            _loginTmp["SAMPLE_CONNECTION"] = new LoginDataModel()
            {
                Database = "name_of_database",
                DefaultIndex = 0,
                Driver = "NetezzaSQL",
                ConnectionName = "SAMPLE_CONNECTION",
                Password = "password",
                UserName = "login",
                Server = "123.456.7.89"
            };
        }
        return _loginTmp;
    }

    public bool AddToOrEditLoginData(string name, string database, string driver, string password, string userName, string server)
    {
        name = name.ToUpper();
        LoginDataModel element;
        if (LoginDataDic.TryGetValue(name, out LoginDataModel? outVal1))
        {
            element = outVal1;
            element.ConnectionName = name;
            element.Driver = driver;
        }
        else
        {
            element = new LoginDataModel()
            {
                ConnectionName = name,
                Driver = driver,
            };
            LoginDataDic[name] = element;
        }
        element.Database = database;
        element.DefaultIndex = 0;
        element.Password = password;
        element.UserName = userName;
        element.Server = server;
        return true;
    }

    public bool DeleteFromLoginData(string name)
    {
        name = name.ToUpper();
        return LoginDataDic.Remove(name);
    }

    public AppOptions Config { get; set; }
    public string SelectedTabIdFromStart { get; set; }

    private Dictionary<string, string>? _fastReplace;

    private List<string>? _typoList;
    public Dictionary<string, string> FastReplaceDictionary => _fastReplace ?? Config.AllSnippets.Where(x => x.Value.snippetType == SnippetModel.FAST_STRING).Select(x => new KeyValuePair<string, string>(x.Key, x.Value.Text)).ToDictionary();

    public List<string> TypoPatternList => _typoList ?? Config.AllSnippets.Where(x => x.Value.snippetType == SnippetModel.TYPO_STRING).Select(x => x.Key).ToList();


    public bool CollapseFoldingOnStartup => Config.CollapseFoldingOnStartup;
    public Dictionary<string, (string snippetType, string? Description, string? Text, string? Keyword)> GetAllSnippets => Config.AllSnippets;

    public Dictionary<string, string> VariablesDictionary { get; set; } = [];

    public string DownloadPluginsBasePath => Environment.GetEnvironmentVariable("JB_DOWNLOAD_BASE_PATH") ?? "";

    public bool IsFromatterAvaiable { get; set; }

    private Assembly _cachedFormatter;

    public void ClearTempSippetsObjects()
    {
        _typoList = null;
        _fastReplace = null;
    }

    public string? GetCurrentCopyVersion()
    {
        //Assembly assembly = Assembly.GetExecutingAssembly();
        //return FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
        var filename = Environment.ProcessPath;
        return FileVersionInfo.GetVersionInfo(filename).ProductVersion;
    }

    public GeneralApplicationData(IMessageForUserTools messageForUserTools, OtherHelpers otherHelpers, ISimpleLogger simpleLogger, IEncryptionHelper encryptionHelper)
    {
        _messageForUserTools = messageForUserTools;
        _otherHelpers = otherHelpers;
        _simpleLogger = simpleLogger;
        _encryptionHelper = encryptionHelper;

        if (!Directory.Exists(IGeneralApplicationData.ConfigDirectoryEvo))
        {
            Directory.CreateDirectory(IGeneralApplicationData.ConfigDirectoryEvo);
        }
        if (!Directory.Exists(IGeneralApplicationData.DataDirectory))
        {
            Directory.CreateDirectory(IGeneralApplicationData.DataDirectory);
        }
        if (!Directory.Exists(IGeneralApplicationData.BackupPath))
        {
            Directory.CreateDirectory(IGeneralApplicationData.BackupPath);
        }
        if (!Directory.Exists(IGeneralApplicationData.MessagesPath))
        {
            Directory.CreateDirectory(IGeneralApplicationData.MessagesPath);
            foreach (string item in Directory.GetFiles(IGeneralApplicationData.ConfigDirectoryEvo))
            {
                try
                {
                    var fileName = Path.GetFileName(item);
                    if (fileName.StartsWith("message_"))
                    {
                        File.Move(item, Path.Combine(IGeneralApplicationData.MessagesPath, fileName));
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            IncludeFields = true,
        };
        MyJsonContextAppOptions context = new MyJsonContextAppOptions(options);
        if (File.Exists(IGeneralApplicationData.ConfigEvoFile))
        {
            try
            {
                Config = JsonSerializer.Deserialize(_encryptionHelper.GetEncodedContentOfTextFile(IGeneralApplicationData.ConfigEvoFile), context.AppOptions);
            }
            catch (Exception ex)
            {
                _simpleLogger.TrackError(ex, isCrash: false);
            }
        }
        Config ??= new AppOptions();
        Config.AddDefaultValues();


        if (File.Exists(IGeneralApplicationData.StartupPath))
        {
            try
            {
                OfflineDocumentContainer offlineDocumentContainer = JsonSerializer.Deserialize(_encryptionHelper.GetEncodedContentOfTextFile(IGeneralApplicationData.StartupPath), MyJsonContextOfflineDocumentContainer.Default.OfflineDocumentContainer) ?? new OfflineDocumentContainer();
                SelectedTabIdFromStart = offlineDocumentContainer.SelectedTabId;
                _dictionaryOfDocuments = offlineDocumentContainer.SqlOfflineDocumentDictionary;
            }
            catch (Exception ex)
            {
                _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
            }
        }

        if (_dictionaryOfDocuments.Count == 0)
        {
            var id = IGeneralApplicationData.NewDocumentId;
            _dictionaryOfDocuments[id] = new OfflineTabData()
            {
                MyId = id,
                Title = "Document1",
                SqlFilePath = null,
                SqlText = null
            };
        }

        if (Config.ResetPlugins && Directory.Exists(IGeneralApplicationData.PluginsDirectory))
        {
            Config.ResetPlugins = false;
            try
            {
                Directory.Delete(IGeneralApplicationData.PluginsDirectory, true);
            }
            catch (Exception ex)
            {
                _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
            }
        }

        if (_cachedFormatter is null && !File.Exists(@"PoorMansNetEasy.dll"))
        {
            IsFromatterAvaiable = false;
        }
        else
        {
            IsFromatterAvaiable = true;
        }


        //register implementations
        DatabaseServiceHelpers.AddDatabaseImplementation(DatabaseTypeEnum.NetezzaSQLOdbc, (string userName, string password, string port, string ip, string db, int connectionTimeout) => new NetezzaOdbc(userName, password, "5480", ip, db, connectionTimeout));
        //DatabaseServiceHelpers.AddDatabaseImplementation(DatabaseTypeEnum.Oracle, (string userName, string password, string port, string ip, string db, int connectionTimeout) => new JustyBase.Services.Database.Oracle(userName, password, "", ip, db, connectionTimeout));
        //DatabaseServiceHelpers.AddDatabaseImplementation(DatabaseTypeEnum.NetezzaSQL, (string userName, string password, string port, string ip, string db, int connectionTimeout) => new Netezza(userName, password, "5480", ip, db, connectionTimeout));
        //DatabaseServiceHelpers.AddDatabaseImplementation(DatabaseTypeEnum.DB2, (string userName, string password, string port, string ip, string db, int connectionTimeout) => new DB2(userName, password, "", ip, db, connectionTimeout));
        //DatabaseServiceHelpers.AddDatabaseImplementation(DatabaseTypeEnum.PostgreSql, (string userName, string password, string port, string ip, string db, int connectionTimeout) => new Postgres(userName, password, "", ip, db, connectionTimeout));
        //DatabaseServiceHelpers.AddDatabaseImplementation(DatabaseTypeEnum.Sqlite, (string userName, string password, string port, string ip, string db, int connectionTimeout) => new Sqlite(userName, password, "", ip, db, connectionTimeout));
    }

    public string AddNewDocument(string title, string initText = null)
    {
        string id = IGeneralApplicationData.NewDocumentId;
        _dictionaryOfDocuments[id] = new OfflineTabData()
        {
            MyId = id,
            Title = title,
            SqlText = initText,
            SqlFilePath = null
        };
        return id;
    }
    public bool TryGetOpenedDocumentVmByFilePath(string path, out IHotDocumentVm? openedVm)
    {
        foreach (var (_, val) in _dictionaryOfDocuments)
        {
            if (val.HotDocumentViewModel.FilePath == path)
            {
                openedVm = val.HotDocumentViewModel;
                return true;
            }
        }
        openedVm = null;
        return false;
    }

    public void SaveConfig()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
        MyJsonContextAppOptions context = new(options);
        string json = JsonSerializer.Serialize(Config, context.AppOptions);
        _encryptionHelper.SaveTextFileEncoded(IGeneralApplicationData.ConfigEvoFile, json);
        SaveCredentials();
    }

    public void SaveCredentials()
    {
        File.Delete(IGeneralApplicationData.CredentialsPathEvo);
        List<LoginDataModel> credentialsList = [.. LoginDataDic.Values];
        string content = JsonSerializer.Serialize(credentialsList, MyJsonContextLoginDataModelList.Default.ListLoginDataModel);
        _encryptionHelper.SaveTextFileEncoded(IGeneralApplicationData.CredentialsPathEvo, content);
    }

    public string GetDataDir() => IGeneralApplicationData.DataDirectory;

    private readonly Dictionary<string, OfflineTabData> _dictionaryOfDocuments = [];
    public bool RemoveDocumentById(string id)
    {
        return _dictionaryOfDocuments.Remove(id);
    }

    public OfflineTabData GetDocumentVmById(string id)
    {
        return _dictionaryOfDocuments[id];
    }

    public IEnumerable<KeyValuePair<string, OfflineTabData>> GetDocumentsKeyValueCollection()
    {
        foreach (KeyValuePair<string, OfflineTabData> item in _dictionaryOfDocuments)
        {
            yield return item;
        }
    }

    public bool TryGetDocumentById(string id, out OfflineTabData? savedTabData)
    {
        if (id is null)
        {
            savedTabData = null;
            return false;
        }
        return _dictionaryOfDocuments.TryGetValue(id, out savedTabData);
    }

    public int GetDocumentIndexById(string id)
    {
        int num = 0;
        foreach (var item in _dictionaryOfDocuments.Keys)
        {
            if (item == id)
            {
                return num;
            }

            num++;
        }
        return -1;
    }

    public void AddProblemDocument(string id, IHotDocumentVm documentViewModel)
    {
        _dictionaryOfDocuments[id] = new OfflineTabData()
        {
            MyId = id,
            Title = "problem",
            SqlText = "problem",
            SqlFilePath = null,
            HotDocumentViewModel = documentViewModel
        };
    }

    public OfflineDocumentContainer GetOfflineDocumentContainer(string selectedTabId)
    {
        OfflineDocumentContainer mn = new()
        {
            SelectedTabId = selectedTabId
        };
        List<string> documentToRemove = [];
        foreach (var (_, value) in _dictionaryOfDocuments)
        {
            if (!value.RefreshDocumentColdState()) // seomething wrong with this document
            {
                documentToRemove.Add(value.MyId);
            }
        }
        foreach (var item in documentToRemove)
        {
            _dictionaryOfDocuments.Remove(item);
        }

        mn.SqlOfflineDocumentDictionary = _dictionaryOfDocuments;
        return mn;
    }

    public async Task<string> GetFormatterSql(string txt)
    {
        Assembly specialFormatterAssembly = _cachedFormatter ??= Assembly.LoadFrom(@"PoorMansNetEasy.dll");
        dynamic dynamicFormatter = specialFormatterAssembly.CreateInstance("ExtraFormatter.Formatter");
        var res = await Task.Run(() => dynamicFormatter.DoFormat(txt));
        return res;
    }
}
