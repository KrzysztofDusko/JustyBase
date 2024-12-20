//TODO : way to many code in this file, need to refactor
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using JustyBase.Common.Models;
using JustyBase.Common.Services;
using JustyBase.Common.Tools;
using JustyBase.Common.Tools.ImportHelpers;
using JustyBase.Common.Tools.ImportHelpers.XML;
using JustyBase.Editor;
using JustyBase.Editor.CompletionProviders;
using JustyBase.Helpers;
using JustyBase.Helpers.Interactions;
using JustyBase.Models.Tools;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Models;
using JustyBase.PluginCommons;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.Shared.Helpers;
using JustyBase.ViewModels.Tools;
using JustyBase.Views;

namespace JustyBase.ViewModels.Documents;

public sealed partial class SqlDocumentViewModel : ISqlAutocompleteData, ICleanableViewModel, IHotDocumentVm
{    
    private readonly IClipboardService _clipboardService;
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly ISimpleLogger _simpleLogger;
    private readonly HistoryService _historyService;
    private readonly AutocompleteService _autocompleteService;
    private readonly IMessageForUserTools  _messageForUserTools;

    private FileSystemWatcher _fileWatcher = new();
    private void MakeWatcher(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }
        _fileWatcher.EnableRaisingEvents = false;
        _fileWatcher.Dispose();
        _fileWatcher = new()
        {
            Path = Path.GetDirectoryName(path),
            Filter = Path.GetFileName(path),
            EnableRaisingEvents = true
        };
        _fileWatcher.Deleted += Watcher_Changed;
        _fileWatcher.Changed += Watcher_Changed;
    }
    private async void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
        _messageForUserTools.ShowSimpleMessageBoxInstance($"File was reloaded {e.FullPath}");
        await Task.Delay(100);
        if (File.Exists(e.FullPath))
        {
            LoadTextFromChangedFile(e.FullPath);
        }
    }
    public ObservableCollection<ConnectionItem> ConnectionsList { get; set; } = [];
    public void RefreshConnectionList()
    {
        ConnectionsList.Clear();
        foreach (var item in SqlDocumentViewModelHelper.ConnectionsList)
        {
            ConnectionsList.Add(item);
        }
    }

    public Action<object, bool>? InserTextAction;

    [ObservableProperty]
    public partial bool WordWrap { get; set; }
    [ObservableProperty]
    public partial int FontSize { get; set; } = ISomeEditorOptions.DEFAULT_DOCUMENT_FONT_SIZE;

    public Action ResetFontStyle { get; set; }

    [ObservableProperty]
    public partial string SqlGroup { get; set; } = "General";

    private string GetCarretInfo()
    {
        var c = SqlEditor.TextArea.Caret;
        return $"offset {c.Offset:N0} column {c.Column} line {c.Line}  ";
    }

    public List<MenuItemForCurrentOptions> CurrentOptionsList { get; init; } = [];

    public ICommand CommentLinesCommand { get; set; }

    private void VmSharedPreparation()
    {
        CommentLinesCommand = new RelayCommand(SqlEditor.CommentSelectedLines);
        LogItems = new ObservableCollection<LogMessage>();

        if (string.IsNullOrEmpty(SelectedDatabase))
        {
            SelectedConnectionIndexAdditionalLogic(SelectedConnectionIndex);
        }

        CurrentOptionsList.Add(new MenuItemForCurrentOptions()
        {
            OptionHeader = SqlDocumentViewModelHelper.CurrentOptionsListDROP,
            OptionCommand = new AsyncRelayCommand<string>(o => GetFunctionForClickedItem(o)),
        });
        CurrentOptionsList.Add(new MenuItemForCurrentOptions()
        {
            OptionHeader = SqlDocumentViewModelHelper.CurrentOptionsListDDL,
            OptionCommand = new AsyncRelayCommand<string>(o => GetFunctionForClickedItem(o))
        });
        CurrentOptionsList.Add(new MenuItemForCurrentOptions()
        {
            OptionHeader = SqlDocumentViewModelHelper.CurrentOptionsListRECREATE,
            OptionCommand = new AsyncRelayCommand<string>(o => GetFunctionForClickedItem(o))
        });
        CurrentOptionsList.Add(new MenuItemForCurrentOptions()
        {
            OptionHeader = SqlDocumentViewModelHelper.CurrentOptionsListRENAME,
            OptionCommand = new AsyncRelayCommand<string>(o => GetFunctionForClickedItem(o))
        });
        CurrentOptionsList.Add(new MenuItemForCurrentOptions()
        {
            OptionHeader = SqlDocumentViewModelHelper.CurrentOptionsListJUMP_TO,
            OptionCommand = new AsyncRelayCommand<string>(o => GetFunctionForClickedItem(o))
        });
        CurrentOptionsList.Add(new MenuItemForCurrentOptions()
        {
            OptionHeader = SqlDocumentViewModelHelper.CurrentOptionsListCREATE_FROM,
            OptionCommand = new AsyncRelayCommand<string>(o => GetFunctionForClickedItem(o))
        });
        CurrentOptionsList.Add(new MenuItemForCurrentOptions()
        {
            OptionHeader = SqlDocumentViewModelHelper.CurrentOptionsListGROOM,
            OptionCommand = new AsyncRelayCommand<string>(o => GetFunctionForClickedItem(o))
        });
        CurrentOptionsList.Add(new MenuItemForCurrentOptions()
        {
            OptionHeader = SqlDocumentViewModelHelper.CurrentOptionsListSELECT,
            OptionCommand = new AsyncRelayCommand<string>(o => GetFunctionForClickedItem(o))
        });
    }

    public async Task JumpToSelectedItem()
    {
        await GetFunctionForClickedItem(SqlDocumentViewModelHelper.CurrentOptionsListJUMP_TO);
    }
    public async Task DropSelectedItem()
    {
        await GetFunctionForClickedItem(SqlDocumentViewModelHelper.CurrentOptionsListDROP);
    }
    public async Task RenameSelectedItem()
    {
        await GetFunctionForClickedItem(SqlDocumentViewModelHelper.CurrentOptionsListRENAME);
    }
    public async Task GroomSelectedItem()
    {
        await GetFunctionForClickedItem(SqlDocumentViewModelHelper.CurrentOptionsListGROOM);
    }
    public async Task RecreateSelectedItem()
    {
        await GetFunctionForClickedItem(SqlDocumentViewModelHelper.CurrentOptionsListRECREATE);
    }
    public async Task DdlSelectedItem()
    {
        await GetFunctionForClickedItem(SqlDocumentViewModelHelper.CurrentOptionsListDDL);
    }
    public async Task CreateFromSelectedItem()
    {
        await GetFunctionForClickedItem(SqlDocumentViewModelHelper.CurrentOptionsListCREATE_FROM);
    }
    public async Task SelectSelectedItem()
    {
        await GetFunctionForClickedItem(SqlDocumentViewModelHelper.CurrentOptionsListSELECT);
    }

    private async Task GetFunctionForClickedItem(string optionName)
    {
        try
        {
            string tappedWord = this.SqlEditor.GetTappedWord();

            string txt = "no option found";

            if (_databaseService is null || _databaseService.Name != SelectedConnectionName)
            {
                _databaseService = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionName));
            }
            if (_databaseService is null)
            {
                _messageForUserTools.ShowSimpleMessageBoxInstance("Please make connection to database");
                return;
            }
            switch (optionName)
            {
                case SqlDocumentViewModelHelper.CurrentOptionsListDROP:
                    txt = _databaseService.GetTableDropCode(tappedWord);
                    break;
                case SqlDocumentViewModelHelper.CurrentOptionsListDDL:
                case SqlDocumentViewModelHelper.CurrentOptionsListRECREATE:
                    var (dbObject, schema) = FindFromName(tappedWord,true, out string database);
                    if (dbObject is not null)
                    {
                        if (optionName == "Ddl")
                        {
                            txt = await _databaseService.GetCreateTableText(database, schema, dbObject.Name);
                        }
                        else if (optionName == "Recreate")
                        {
                            txt = await _databaseService.GetReCreateTableText(database, schema, dbObject.Name);
                        }
                    }
                    else
                    {
                        txt = "to many or no results";
                    }
                    break;
                case SqlDocumentViewModelHelper.CurrentOptionsListRENAME:
                    txt = _databaseService.GetTableRenameCode(tappedWord);
                    break;
                case SqlDocumentViewModelHelper.CurrentOptionsListCREATE_FROM:
                    txt = _databaseService.GetCreateFromCode(tappedWord);
                    break;
                case SqlDocumentViewModelHelper.CurrentOptionsListGROOM:
                    txt = _databaseService.GetGroom(null, null, tappedWord);
                    break;
                case SqlDocumentViewModelHelper.CurrentOptionsListSELECT:
                    txt = _databaseService.GetShortSelectCode(tappedWord);
                    break;
                case SqlDocumentViewModelHelper.CurrentOptionsListJUMP_TO:
                    txt = null;
                    var (dbObject1, schema1) = FindFromName(tappedWord, true, out string database1);
                    if (dbObject1 is not null)
                    {
                        string nme = dbObject1.Name;
                        nme = _databaseService.CleanSqlWord(nme, _databaseService.AutoCompletDatabaseMode);
                        schema1 = _databaseService.CleanSqlWord(schema1, _databaseService.AutoCompletDatabaseMode);
                        database1 = _databaseService.CleanSqlWord(database1, _databaseService.AutoCompletDatabaseMode);

                        string[] toExpandPath = new SchemaSearchItem()
                        {
                            Name = nme,
                            Db = database1,
                            Schema = schema1,
                            Type = dbObject1.TypeInDatabase.ToStringEx()
                        }.GetPath(SelectedConnectionName);

                        if (toExpandPath.Length > 0)
                        {
                            await ExpandTo(toExpandPath);
                        }
                    }
                    break;
            }

            if (!string.IsNullOrWhiteSpace(txt))
            {
                SqlEditor.InsertTextToPrevLineAndSelect(txt);
            }
        }
        catch (Exception ex)
        {
            _simpleLogger.TrackError(ex,isCrash:true);
            _messageForUserTools.ShowSimpleMessageBoxInstance($"ERROR {ex.Message}");
        }
    }

    public async Task ImportFromFilePath(string path)
    {
        if (SqlDocumentViewModelHelper.NotSupportedFileExtension(path))
        {
            InserTextAction?.Invoke("\n" + "not imported", true);
            return;
        }

        try
        {
            var selectedConnectionName = SelectedConnectionName;
            if (string.IsNullOrEmpty(selectedConnectionName))
            {
                return;
            }
            ImportFromExcelFile importFrom = new (x => _messageForUserTools.ShowSimpleMessageBoxInstance(x), _simpleLogger)
            {
                StandardMessageAction = (msg) =>
                {
                    try
                    {
                        _messageForUserTools.DispatcherActionInstance(() => InserTextAction?.Invoke("\n" + DateTime.Now + ": " + msg, true));
                    }
                    catch (Exception ex)
                    {
                        _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                    }
                },
                FilePath = path
            };

            IDatabaseService service = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, selectedConnectionName, delayCache: true);

            await importFrom.PerformFastImportFromFileAsync(service.DatabaseType, service);

        }
        catch (Exception ex)
        {
            _simpleLogger.TrackError(ex, isCrash: false);
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex.Message);
        }
    }

    private (DatabaseObject dbObject, string schema) FindFromName(string tappedWord,bool cleanNames, out string database)
    {
        var m = SqlDocumentViewModelHelper.DatabaseSchemaTableRegex.Match(tappedWord);
        
        if (m.Success)
        {
            database = m.Groups["part1"].Value;
            var schema = m.Groups["part2"].Value;
            var name = m.Groups["part3"].Value;
            if (string.IsNullOrEmpty(database))
            {
                database = SelectedDatabase ?? _databaseService.Database;
            }

            var o = _databaseService.FindDbObject(database, schema, name, cleanNames);
            if (o.Count() == 1)
            {
                return o.FirstOrDefault();
            }
        }
        database = null;
        return (null,null);
    }

    public string SelectedConnectionName => SelectedConnectionIndex < 0 || SelectedConnectionIndex >= SqlDocumentViewModelHelper.ConnectionsList.Count ? "" : SqlDocumentViewModelHelper.ConnectionsList[SelectedConnectionIndex].Name;

    public bool TrySetConnection(string name)
    {
        for (int i = 0; i < SqlDocumentViewModelHelper.ConnectionsList.Count; i++)
        {
            if (SqlDocumentViewModelHelper.ConnectionsList[i].Name == name)
            {
                SelectedConnectionIndex = i;
                return true;
            }
        }
        return false;
    }

    public string HowManyRunningMessage => $"{HowManyRunning} running";


    [RelayCommand]
    private void ReplaceVariable()
    {
        SqlEditor.ReplaceVariable();
    }

    [RelayCommand]

    private async Task PasteAsInAsync(string pasteType)
    {
        string clip = await _clipboardService.GetTextAsync();
        if (clip is null)
        {
            return;
        }
        IsReadOnly = true;
        clip = clip.Trim();
        var result = StringExtension.PasteAsInHelper(pasteType, clip);
        IsReadOnly = false;
        SqlEditor?.Document.Insert(SqlEditor.CaretOffset, result);
    }

    [RelayCommand]
    private async Task PastClipAsSelectUnionAsync()
    {
        IsReadOnly = true;

        string clip = await _clipboardService.GetTextAsync();

        if (clip is null)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("clipboar is empty");
            return;
        }

        clip = clip.TrimEnd('\r', '\n');
        clip = clip.Replace("\r", "");
        //char escapechar = '\\';

        var lines = StringExtension.ClipboardTextToLinesArray(clip);
        if (lines is null)
        {
            return;
        }
        var firstRange = lines.FirstOrDefault();
        var headers = clip[firstRange].Split('\t').Select(arg => arg.Trim()).ToArray();

        var allLetters = !headers.Where(x => x.Length == 0 || char.IsAsciiLetter(x[0]) == false).Any();

        StringBuilder sb = new();
        sb.AppendLine("--REGION clipboard data");

        int i = 1;
        foreach (var actualRange in lines)
        {
            if (allLetters && i==1)
            {
                i++;
                continue;
            }
            var v1 = clip[actualRange].AsSpan().MySplit2('\t');

            if (actualRange.Start.Equals(actualRange.End))
            {
                continue;
            }

            if (i == 1)
            {
                sb.Append("SELECT");
            }
            else
            {
                sb.Append("UNION ALL SELECT");
            }
            for (int j = 0; j < v1.Count; j++)
            {
                var val = DbXMLImportJob.GetValueStringRepresentationWithType(out DbSimpleType nz, v1[j]);
                if (nz == DbSimpleType.Integer && v1[j].Trim().Length == 11 && headers[j].Contains("PESEL", StringComparison.OrdinalIgnoreCase))
                {
                    nz = DbSimpleType.Nvarchar;
                    val = $"'{v1[j].Trim()}'";
                }
                sb.Append($" {(val == "" ? "null" : val)} AS {headers[j].NormalizeDbColumnName().Trim()}");
                if (j != v1.Count - 1)
                {
                    sb.Append(',');
                }
            }
            sb.AppendLine();
            i++;
        }

        sb.AppendLine("--ENDREGION");
        IsReadOnly = false;
        SqlEditor.Document.Insert(SqlEditor.TextArea.Caret.Offset, sb.ToString());

    }

    [ObservableProperty]
    public partial bool IsRunEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HowManyRunningMessage))]
    public partial int HowManyRunning { get; set; }

    public bool ShowDetailsButtonX => _generalApplicationData.Config.ShowDetailsButton;


    [ObservableProperty]
    public partial string SelectedDatabase { get; set; }

    private int _selectedConnectionIndex = 0;

    public int SelectedConnectionIndex
    {
        get => _selectedConnectionIndex;
        set
        {
            if (_selectedConnectionIndex != value)
            {
                if (!KeepConnectionOpen)
                {
                    _cachedDbConnection = null;
                }
                else
                {
                    try
                    {
                        _cachedDbConnection?.Connection?.Close();
                    }
                    catch (Exception ex)
                    {
                        _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                    }
                }
            }
            SelectedConnectionIndexAdditionalLogic(value);
        }
    }

    public void SelectedConnectionIndexAdditionalLogic(int value1)
    {
        if (value1 >= 0 && value1 < SqlDocumentViewModelHelper.ConnectionsList.Count)
        {
            SetProperty(ref _selectedConnectionIndex, value1, nameof(SelectedConnectionIndex));
        }
        OnPropertyChanged(nameof(DatabasesList));
        if (SelectedConnectionIndex >= 0 && SelectedConnectionIndex < SqlDocumentViewModelHelper.ConnectionsList.Count)
        {
            SelectedDatabase = SqlDocumentViewModelHelper.ConnectionsList[SelectedConnectionIndex].DefaultDatabase;
        }
    }

    [ObservableProperty]
    public partial bool SingleCommand { get; set; } = false;

    [ObservableProperty]
    public partial bool ContinueOnError { get; set; }


    [ObservableProperty]
    public partial bool KeepConnectionOpen { get;set; } = true;

    partial void OnKeepConnectionOpenChanged(bool value)
    {
        if (!KeepConnectionOpen)
        {
            try
            {
                if (_cachedDbConnection is not null && _cachedDbConnection.Connection is not null && _cachedDbConnection.Connection.State == ConnectionState.Open)
                {
                    _cachedDbConnection.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                _simpleLogger.TrackCrashMessagePlusOpenNotepad(ex, "close failed", isCrash: false);
            }
            finally
            {
                _cachedDbConnection = null;
            }
        }
    }



    [ObservableProperty]
    public partial bool IsReadOnly { get; set; }

    [ObservableProperty]
    public partial bool DoPooling { get; set; }

    public bool IsStopEnabled => TasksToAbort > 0;


    private readonly DataTable _tableToCompute = new();
    private object Evaluate(string expression)
    {
        object result = ReplaceSessionVariables(expression);
        try
        {
            result = _tableToCompute.Compute(expression, "");
        }
        catch (Exception)
        {
        }

        return result;
    }

    private async ValueTask AddSessionVariable(Match m, DbConnection? con, string localTile)
    {
        string variableValue = m.Groups["sessionValue"].Value;
        string val = ReplaceSessionVariables(variableValue);
        object val2 = val;
        try
        {
            if (!val.StartsWith("SQL_"))
            {
                val2 = Evaluate(val);
            }
            else
            {
                if (con is not null)
                {
                    IDatabaseService service = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionName));
                    con = service.GetConnection(null);
                    con.Open();
                }
                if (val.StartsWith("SQL_RESULT["))
                {
                    string sql = val["SQL_RESULT[".Length..^1];
                    using (var cmd = con.CreateCommand())
                    {
                        SetTimeoutForCommand(localTile, _databaseService, cmd);
                        cmd.CommandText = sql;
                        val2 = await Task.Run(() => cmd.ExecuteScalar());
                    }
                }
                else if (val.StartsWith("SQL_RECORDS_AFFECTED["))
                {
                    string sql = val["SQL_RECORDS_AFFECTED[".Length..^1];
                    using (var cmd = con.CreateCommand())
                    {
                        SetTimeoutForCommand(localTile, _databaseService, cmd);
                        cmd.CommandText = sql;
                        val2 = await Task.Run(() => cmd.ExecuteNonQuery());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _simpleLogger.TrackError(ex, isCrash: false);
        }
        var vvm = App.GetRequiredService<VariablesViewModel>();
        vvm.AddVariable(m.Groups["sessionVar"].Value[1..], val2.ToString());
    }

    private readonly VariablesViewModel _variablesViewModel;
    public string ReplaceVariablesP2(string query, List<string> toAsk)
    {
        // $DATA2, before $DATA
        toAsk.Sort(delegate (string x, string y)
        {
            if (x.Length != y.Length) return y.Length.CompareTo(x.Length);
            else return y.CompareTo(x);
        });

        foreach (var variableTxt in toAsk)
        {
            _variablesViewModel.AddVariable(variableTxt[1..], SqlDocumentViewModelHelper.KnownParams[variableTxt]);
        }

        return query.ReplaceVariablesInSql(toAsk, SqlDocumentViewModelHelper.KnownParams);
    }
    public string ReplaceSessionVariables(string query)
    {
        var tab = _variablesViewModel.GetVariablesDictStatic();
        return query.ReplaceVariablesInSql(tab.Keys.ToList(), tab, variableStart: '&');
    }


    private IDatabaseService _databaseService;

    public async IAsyncEnumerable<CompletionDataSql> GetWordsList(string input, Dictionary<string, List<string>> aliasDbTable,
    Dictionary<string, List<string>> subqueryHints,
    Dictionary<string, List<string>> withHints,
    Dictionary<string, List<string>> tempTableHints)
    {
        if (SelectedConnectionIndex == -1)
        {
            yield break;
        }

        if (_databaseService is null || _databaseService.Name != SelectedConnectionName)
        {
            _databaseService = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionName));
            yield return new CompletionDataSql("", "", false, Glyph.None, null);
        }

        var wordsList = _autocompleteService.GetWordsList(input, aliasDbTable, subqueryHints, withHints, tempTableHints,
            _databaseService, SelectedDatabase);
        foreach (var item in wordsList)
        {
            yield return item;
        }
    }
    
    //move this logic to service class
    private readonly Lock _runningQueriesLock = new();
    public int TasksToAbort
    {
        get
        {
            int res = 0;
            for (int i = _globalAbortUBound; i < _globalQueryNumber; i++) 
            {
                if (!_querieDic[i].FullFinish)
                {
                    res++;
                }
            }
            return res;
        }
    }

    private int _globalAbortUBound = 0;
    private readonly Dictionary<int, QueryInfo> _querieDic = [];
    private int _globalQueryNumber = 0;
    private const int CANCELATION_TIMEOUT_SEC = 5;
    private async Task AbortSqlHelper(int pevAbortUbound)
    {
        for (int i = pevAbortUbound; i < _globalAbortUBound; i++)
        {
            if (i >= _querieDic.Count)
            {
                return;
            }
            var q = _querieDic[i];
            if (q.FullFinish)
            {
                continue;
            }

            foreach (var cmd in q.DbCommands.Keys)
            {
                if (q.DbCommands[cmd] == SqlCommandState.finished)
                {
                    continue;
                }
                await Task.Run(() =>
                {
                    try
                    {
                        cmd.Cancel();
                    }
                    catch (Exception ex) 
                    {
                        _simpleLogger.TrackError(ex, isCrash: false);
                        _messageForUserTools.ShowSimpleMessageBoxInstance($"Error with command cancelation\r\n{ex.Message}");
                    }
                }).WaitAsync(TimeSpan.FromSeconds(CANCELATION_TIMEOUT_SEC));

                if (q.FullFinish)
                {
                    break;
                }
            }
        }
    }
    private void RunningP1(int actualQueryNum)
    {
        lock (_runningQueriesLock)
        {
            _globalQueryNumber++;
            HowManyRunning++;
            _querieDic[actualQueryNum] = new QueryInfo
            {
                FullFinish = false,
                DbCommands = new Dictionary<DbCommand, SqlCommandState>()
            };
        }
    }

    public void InserSnippet(string text)
    {
        var snippet =new CodeSnippet("ABC","DEF",text,"GHI");
        var editorSnippet = snippet.CreateAvalonEditSnippet();

        using (SqlEditor.TextArea.Document.RunUpdate())
        {
            editorSnippet.Insert(SqlEditor.TextArea);
        }
    }

    [ObservableProperty]
    public partial ObservableCollection<LogMessage> LogItems { get; set; }

    public LogMessage AddLogMessage(string msg, LogMessageType logMessageType, DateTime dateTime, string title)
    {
        var logItem = new LogMessage()
        {
            Timestamp = dateTime,
            Message = msg,
            Title = title,
            MessageType = logMessageType,
            Source = this.Id
        };
        AddLogMesage(logItem);
        return logItem;
    }

    [RelayCommand]

    private void ShowInExplorer()
    {
        _messageForUserTools.ShowOrShowInExplorerHelper(FilePath);
    }

    private record CachedDbConnection(IDatabaseService DbService, DbConnection Connection, string DatabaseName);

    private CachedDbConnection? _cachedDbConnection = null;
    private DbConnection GetConToGo(bool doPooling, bool keepConnectionOpenLocal, IDatabaseService service)
    {
        DbConnection con;
        if (keepConnectionOpenLocal && _cachedDbConnection is not null && _cachedDbConnection.Connection.State == ConnectionState.Open)
        {
            con = _cachedDbConnection.Connection;
        }
        else
        {
            con = service.GetConnection(null, pooling: doPooling);
            if (keepConnectionOpenLocal)
            {
                _cachedDbConnection = new CachedDbConnection(service,con, null);
            }
        }
        return con;
    }

    private void ShowProgress(long x, long y)
    {
        //_messageForUserTools.DispatcherActionInstance(() =>
        //{
        //    ProgressValue = (int)(100 * x / y);
        //});
        ProgressValue = (int)(100 * x / y);
    }

    [ObservableProperty]
    public partial int ProgressValue { get; set; } = 0;

    [RelayCommand(AllowConcurrentExecutions =true)]
    private async Task RunSqlAsync(string option)
    {
        if (SqlEditor is null || SelectedConnectionIndex == -1)
        {
            ReturnPhase();
            return;
        }
        if (SelectedConnectionIndex == -1)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("please select connection");
            ReturnPhase();
            return;
        }
        bool keepConnectionOpenLocal = KeepConnectionOpen;
        string localTitle = GetTile();
        bool localDoPooling = DoPooling;
        bool singleCommandLocal = SingleCommand || option?.Contains("|SingleBath") == true;
        
        if (keepConnectionOpenLocal) // only one SQL at same time
        {
            IsRunEnabled = false;
        }

        LogMessage? currentLogMessage = AddLogMessage($"Started with: {option}", LogMessageType.ok, DateTime.Now, localTitle);
        string filePathToExport = "";
        if (option.StartsWith(".xlsb") || option.Contains(".csv") || option.StartsWith(".parquet"))
        {
            filePathToExport = await ChoseExportPath(option);
            if (filePathToExport is null)
            {
                ReturnPhase();
                return;
            }
        }

        string query = SqlEditor.SelectQueryPhase(out int currentSqlPosiotionInEditor);
        if (string.IsNullOrWhiteSpace(query) || query.Length < 7)
        {
            ReturnPhase();
            return;
        }

        Match variableDevineMatch = SqlDocumentViewModelHelper.RxSessionVariableDefine.Match(query);
        if (variableDevineMatch.Success)
        {
            await AddSessionVariable(variableDevineMatch, null, localTitle);
            ReturnPhase();
            return;
        }

        bool TABS_WITH_ROWS = query.StartsWith(DatabaseService.TABS_WITH_ROWS);
        bool TIMEOUT_OVERRIDE = query.Contains(DatabaseService.TIMEOUT_OVERRIDE);

        if (query.Contains(DatabaseService.CONTINUE_ON_ERROR))
        {
            ContinueOnError = true;
        }
        bool continueOnErrorLocal = ContinueOnError;

        int? FORCED_TIMEOUT = SqlDocumentViewModelHelper.FindForcedTimeout(query);

        if (SqlEditor.ErrorWaningsPahse1())
        {
            return;
        }

        query = await AskAndReplaceVariablesFromUser(query);

        List<string> sqls =  SqlDocumentViewModelHelper.ConvertSqlTextToListOfSqls(singleCommandLocal, query);
        int actualqlobalQueryNum = _globalQueryNumber;//it is possible to run another query before this ends, so we have to remember this number
        try
        {
            RunningP1(actualqlobalQueryNum);
            OnPropertyChanged(nameof(TasksToAbort));
            OnPropertyChanged(nameof(IsStopEnabled));
            currentLogMessage = AddLogMessage("Running", LogMessageType.inProgress, System.DateTime.Now, localTitle);
            currentLogMessage?.AddInnerMessageInUiThread("Started", System.DateTime.Now);
#if AVALONIA
#else
            AnimationRefresh();
#endif
            await _generalApplicationData.LoadPluginsIfNeeded(PluginsDownloadInfo);

            IDatabaseService actualDatabaseService = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionName, delayCache: false));
            if (actualDatabaseService is null)
            {
                AddWarningResult(localTitle, actualqlobalQueryNum, actualDatabaseService);
                currentLogMessage?.AddInnerMessageInUiThread("cannot establish connection", System.DateTime.Now);
                ReturnPhase();
                return;
            }

            lock (DatabasesList)
            {
                RefreshDataseList(actualDatabaseService);
            }

            if (actualqlobalQueryNum >= _globalAbortUBound) // query is not canceled
            {
                await Task.Run(async () =>
                {
                    DbConnection con = GetConToGo(localDoPooling, keepConnectionOpenLocal, actualDatabaseService);
                    try
                    {
                        try
                        {
                            var res = actualDatabaseService.ChangeDatabaseIfNeeded(con, SelectedDatabase);
                            if (string.IsNullOrWhiteSpace(res))
                            {
                                SelectedDatabase = res;
                            }
                        }
                        catch (Exception ex)
                        {
                            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                        }
                        con = SqlDocumentViewModelHelper.OpenConnectionIfNeeded(actualDatabaseService, con, _simpleLogger);

                        actualDatabaseService.DbMessageAction += o =>
                        {
                            if (o?.StartsWith("QUERY PLAN:") == true)
                            {
                                _messageForUserTools.ShowSimpleMessageBoxInstance(o);
                            }
                            else
                            {
                                currentLogMessage?.AddInnerMessageInUiThread(o, System.DateTime.Now);
                            }
                        };

                        ClosePreviousResultyIfNeeded();

                        for (int currentLocalSqlNumber = 0; currentLocalSqlNumber < sqls.Count; currentLocalSqlNumber++)
                        {
                            ShowProgress(currentLocalSqlNumber, sqls.Count);
                            string sql = sqls[currentLocalSqlNumber];
                            if (string.IsNullOrWhiteSpace(sql) || sql.IsAllSqlComment())
                            {
                                continue;
                            }
                            var m1 = SqlDocumentViewModelHelper.RxSessionVariableDefine.Match(sql);
                            if (m1.Success)
                            {
                                await AddSessionVariable(m1, con,localTitle);
                                await Task.Delay(20);
                                continue;
                            }
                            sql = ReplaceSessionVariables(sql);

                            var m = SqlDocumentViewModelHelper.SleepRegex.Match(sql);
                            if (m.Success && int.TryParse(m.Groups["num"].Value, out var time))
                            {
                                await Task.Delay(time);
                                continue;
                            }
                            m = SqlDocumentViewModelHelper.ExtractRegex.Match(sql);
                            if (m.Success)
                            {
                                await AdHocCompressionHelper.Extract(m.Groups["path"].Value, ShowProgress);
                                continue;
                            }
                            m = SqlDocumentViewModelHelper.CompressRegex.Match(sql);
                            if (m.Success)
                            {
                                await AdHocCompressionHelper.Compress(m.Groups["path"].Value, m.Groups["mode"].Value, ShowProgress);
                                continue;
                            }

                            var connectionChangeMatch = SqlDocumentViewModelHelper.ChangeConnectionRegex.Match(sql);
                            if (connectionChangeMatch.Success)
                            {
                                string connectionToSwitch = connectionChangeMatch.Groups["connectionName"].Value;
                                int index = SqlDocumentViewModelHelper.ConnectionsList.Select(o => o.Name).ToList().IndexOf(connectionToSwitch);
                                if (index != -1)
                                {
                                    con.Close();// close prev connection
                                    SelectedConnectionIndex = index;
                                    actualDatabaseService = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionName, delayCache: true));
                                    con = GetConToGo(localDoPooling, keepConnectionOpenLocal, actualDatabaseService);
                                    if (con.State != ConnectionState.Open)
                                    {
                                        con.Open();
                                    }
                                }
                                continue;
                            }

                            using var cmd = con.CreateCommand();
                            if (actualDatabaseService is INetezzaDotnet netezza)
                            {
                                netezza.OptimizeCommandBuffer(cmd, !option.StartsWith(".csv")); //experimental
                            }
                            
                            _querieDic[actualqlobalQueryNum].DbCommands[cmd] = SqlCommandState.created;

                            SetTimeoutForCommand(localTitle, actualDatabaseService, cmd, FORCED_TIMEOUT);

                            long? exportUpFrontRowCount = null;
                            cmd.CommandText = sql;
                            if (actualqlobalQueryNum < _globalAbortUBound)//query was canceled
                            {
                                return;
                            }
                            try
                            {
                                string forceAnotherOption = "";
                                var inlineExportMatch = SqlDocumentViewModelHelper.rxExportCsvXlsx.Match(sql);
                                if (inlineExportMatch.Success)
                                {
                                    //___expCsv|___expXlsx|___expParquet
                                    forceAnotherOption = inlineExportMatch.Groups["exportName"].Value;
                                    cmd.CommandText = inlineExportMatch.Groups["sql"].Value;
                                    filePathToExport = inlineExportMatch.Groups["filePath"].Value;
                                    if (inlineExportMatch.Groups["options"].Value.Contains("#upFrontRowsCount true", StringComparison.OrdinalIgnoreCase))
                                    {
                                        try
                                        {
                                            var cmdX = con.CreateCommand();
                                            cmdX.CommandTimeout = 60;
                                            cmdX.CommandText = $"SELECT COUNT(1) FROM ({inlineExportMatch.Groups["sql"].Value}) TMP";
                                            exportUpFrontRowCount = cmdX.ExecuteScalar() as long?;
                                            currentLogMessage?.AddInnerMessageInUiThread($" {exportUpFrontRowCount:N0} rows to export..", System.DateTime.Now);
                                            currentLogMessage?.AddInnerMessageInUiThread($" command timeout is set to {new TimeSpan(0, 0, cmd.CommandTimeout):g}", System.DateTime.Now);
                                        }
                                        catch (Exception ex)
                                        {
                                            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                                        }
                                    }
                                }

                                _querieDic[actualqlobalQueryNum].DbCommands[cmd] = SqlCommandState.started;

                                AddToHistory(actualDatabaseService.Name, con.Database, cmd.CommandText);
                                CommandBehavior cb = CommandBehavior.SequentialAccess;
                                if (option.StartsWith(".csv"))
                                {
                                    cb = CommandBehavior.Default;
                                }

                                using var rdr = cmd.ExecuteReader(cb);
                   
                                _querieDic[actualqlobalQueryNum].DbCommands[cmd] = SqlCommandState.executed;

                                if (actualqlobalQueryNum < _globalAbortUBound)
                                {
                                    return;
                                }
                                int len = cmd.CommandText.Length;
                                len = Math.Min(len, 100);
                                string shortQuery = cmd.CommandText[..len].Trim().ReplaceLineEndings(" ");
                                currentLogMessage?.AddInnerMessageInUiThread($"started {shortQuery}", System.DateTime.Now);

                                do
                                {
                                    if (string.IsNullOrEmpty(forceAnotherOption) && option.StartsWith("Grid") && rdr.FieldCount > 0)
                                    {
                                        HandleStandardGrid(actualDatabaseService, $"{localTitle}_{currentLocalSqlNumber}", query, currentLogMessage, TABS_WITH_ROWS, actualqlobalQueryNum, rdr, cmd, shortQuery);
                                    }
                                    else if ((forceAnotherOption == "@expXlsx" || option.StartsWith(".xlsb")) && !String.IsNullOrWhiteSpace(filePathToExport))
                                    {
                                        var timestamp = Stopwatch.GetTimestamp();
                                        void progressAction(int n)
                                        {
                                            MessageForUserTools.DispatcherAction(() =>
                                            {
                                                if (Stopwatch.GetElapsedTime(timestamp).Seconds >= 10)
                                                {
                                                    AddLogMessage($"Exporting... {n:N0}", LogMessageType.ok, DateTime.Now, localTitle);
                                                    timestamp = Stopwatch.GetTimestamp();
                                                }
                                            });
                                        }
                                        rdr.HandleExcelOutput(filePathToExport, sql, "Justy", progressAction);
                                    }
                                    else if ((forceAnotherOption == "@expCsv" || option.Contains(".csv") || option.StartsWith(".parquet")) && !String.IsNullOrWhiteSpace(filePathToExport))
                                    {
                                        AdvancedExportOptions? opt = null;
                                        if (inlineExportMatch.Success)
                                        {
                                            string optionsString = inlineExportMatch.Groups["options"].Value;
                                            opt = AdvancedExportOptions.ParseFromString(optionsString);
                                        }

                                        Stopwatch sw = Stopwatch.StartNew();
                                        void innerAction(long localN)
                                        {
                                            if (exportUpFrontRowCount is long longRows)
                                            {
                                                AddLogMessage($" Exporting...  {((double)localN / longRows):P1}", LogMessageType.ok, DateTime.Now, localTitle);
                                                AddLogMessage($" {(1_000 * localN / sw.Elapsed.TotalMilliseconds):N0} rows per sec", LogMessageType.ok, DateTime.Now, localTitle);
                                                if (localN > 0)
                                                {
                                                    long elapsedTics = (long)(((double)(longRows - localN) / localN) * sw.Elapsed.Ticks);
                                                    AddLogMessage($" {new TimeSpan(elapsedTics):g} to finish", LogMessageType.ok, DateTime.Now, localTitle);
                                                }
                                                ShowProgress((int)(100 * localN / longRows), 100);
                                            }
                                            else
                                            {
                                                AddLogMessage($"Exporting...  {localN:N0}", LogMessageType.ok, DateTime.Now, localTitle);
                                                AddLogMessage($" {(1_000 * localN / sw.Elapsed.TotalMilliseconds):N0} rows per sec", LogMessageType.ok, DateTime.Now, localTitle);
                                            }
                                        }
                                        void progressAction2(long n)
                                        {
                                            long localN = n;
                                            MessageForUserTools.DispatcherAction(() =>
                                            {
                                                innerAction(localN);
                                            });
                                        }

                                        rdr.HandleCsvOrParquetOutput(filePathToExport, opt, progressAction2);
                                    }
                                    if (rdr.RecordsAffected != -1)
                                    {
                                        HandleAnotherResult(currentLogMessage, rdr);
                                    }
                                } while (actualqlobalQueryNum >= _globalAbortUBound && rdr.NextResult());

                                currentLogMessage?.AddInnerMessageInUiThread($"finished [{cmd.CommandText[..len].Trim().Replace('\n', ' ').Replace('\r', ' ')} ...]", System.DateTime.Now);

                            }
                            catch (Exception exx1)
                            {
                                if (exx1.Message is null || exx1.Message is not null
                                && exx1.Message != "ERROR: Transaction rolled back by client"
                                && exx1.Message != "ERROR: Query was cancelled."
                                && !exx1.Message.StartsWith("ERROR: 15 : Header precompile failed.")
                                && !exx1.Message.StartsWith("ERROR: relation does not exist")
                                && exx1.StackTrace?.Contains("at NZdotNET.ForwardsOnlyDataReader.Read()") == false
                                && exx1.StackTrace?.Contains("at NZdotNET.NZdotNETState.ProcessBackendResponses_Ver_3") == false
                                && !exx1.Message.StartsWith("ERROR [42704] [IBM][DB2/NT64]")
                                && !exx1.Message.StartsWith("ERROR: Attribute ")
                                && !exx1.Message.StartsWith("A timeout has occured. If you were establishing a connection")
                                && !exx1.Message.StartsWith("The CommandText to be set should not be null or Empty!")
                                )
                                {
                                    _messageForUserTools.ShowSimpleMessageBoxInstance(exx1);
                                }

                                if ((actualDatabaseService.DatabaseType == DatabaseTypeEnum.NetezzaSQL || actualDatabaseService.DatabaseType == DatabaseTypeEnum.NetezzaSQLOdbc) && con is not null && exx1.Message.StartsWith("A timeout has occured. If you were establishing a connection"))
                                {
                                    _messageForUserTools.ShowSimpleMessageBoxInstance("Due to NPS driver limitation connection have to be reopened", "Error");
                                    con.Close();
                                    con.Open();
                                }

                                if (exx1.Message == "ERROR: Query was cancelled.")
                                {
                                    _messageForUserTools.ShowSimpleMessageBoxInstance(exx1.Message, "Error");
                                }
                                else
                                {
                                    var (position, length) = actualDatabaseService.HanleExceptions(sql, exx1);
                                    if (position != -1)
                                    {
                                        int localPos = currentSqlPosiotionInEditor;
                                        MessageForUserTools.DispatcherAction(() => SqlEditor.SelectError(localPos + position - 1, length));
                                    }
                                }
                                ErrorMessageToUi(localTitle, currentLogMessage, actualqlobalQueryNum, actualDatabaseService, currentLocalSqlNumber, sql, cmd, exx1);
                                if (!continueOnErrorLocal && exx1.Message != "ERROR: Query was cancelled.")
                                {
                                    break;
                                }
                            }
                            finally
                            {
                                _querieDic[actualqlobalQueryNum].DbCommands[cmd] = SqlCommandState.finished;
                                currentSqlPosiotionInEditor += sqls[currentLocalSqlNumber].Length + 1;// 1 for ';'
                            }
                        }
                        ShowProgress(sqls.Count, sqls.Count); // = 100%
                        try
                        {
                            _messageForUserTools.FlashWindowExIfNeeded();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                        currentLogMessage?.AddInnerMessageInUiThread(ex.Message, System.DateTime.Now);
                        if (currentLogMessage is not null)
                        {
                            currentLogMessage.MessageType = LogMessageType.error;
                        }
                    }
                    finally
                    {
                        if (!keepConnectionOpenLocal)
                        {
                            con.Close();
                        }
                    }
                });
            }
        }
        catch (Exception exx2)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance($"{exx2.Message}");
            _messageForUserTools.ShowSimpleMessageBoxInstance($"{exx2.StackTrace}");
#if AVALONIA
            if (exx2.Message != "Operation is not supported on this platform." || exx2.Source != "NZdotNETSlim")
            {
                ActualDockFactory.AddNewResult((null, null, exx2.Message), this.Id, actualqlobalQueryNum, ref _globalAbortUBound, null, null, localTitle);
            }
#else
            AddLogMessage(exx2.Message, LogMessageType.error, DateTime.Now, localTitle);
#endif
        }
        finally
        {
            _querieDic[actualqlobalQueryNum].FullFinish = true;
            OnPropertyChanged(nameof(TasksToAbort));
#if AVALONIA
#else
            OnPropertyChanged(nameof(IsStopEnabled));
            AnimationRefresh();
#endif
            HowManyRunning--;

            currentLogMessage?.AddInnerMessageInUiThread("Finished", System.DateTime.Now);

            if (currentLogMessage is not null)
            {
                if (currentLogMessage.MessageType != LogMessageType.error)
                {
                    currentLogMessage.MessageType = LogMessageType.ok;
                }

                currentLogMessage.Message = $"Finished {DateTime.Now}";
            }

            if (!IsRunEnabled)
            {
                IsRunEnabled = true;
            }
        }

        ReturnPhase();
    }

    private void RefreshDataseList(IDatabaseService actualDatabaseService)
    {
        foreach (var item in actualDatabaseService.GetDatabases(""))
        {
            if (!DatabasesList.Contains(item))
            {
                DatabasesList.Add(item);
            }
        }
        if (string.IsNullOrWhiteSpace(SelectedDatabase) && DatabasesList.Count == 1)
        {
            SelectedDatabase = DatabasesList[0];
        }
    }

    private async Task<string> AskAndReplaceVariablesFromUser(string query)
    {
        List<string> toAsk = SqlDocumentViewModelHelper.GetVariableValuesP1(query);

        if (toAsk.Count > 0)
        {
            var parametrViewModel = new SqlParametrViewModel(toAsk, SqlDocumentViewModelHelper.KnownParams);
            var paramWindow = new SqlParametrWindow
            {
                DataContext = parametrViewModel
            };
            await HandleVariableDialog(paramWindow);
            if (parametrViewModel.IsCancel)
            {
                ReturnPhase();
                return query;
            }

            query = ReplaceVariablesP2(query, toAsk);
        }
        return query;
    }

    private async Task<string?> ChoseExportPath(string option)
    {
        string? path;
        if (option.StartsWith(".xlsb"))
        {
            path = await ChoseXlsbPath();
        }
        else if (option.StartsWith(".parquet"))
        {
            path = await ChoseParquetPath();
        }
        else
        {
            var csvEnum = option.GetCsvCompressionEnum();
            path = await ChoseCsvPath(csvEnum);
        }

        return path;
    }

    private void AddToHistory(string serviceName, string database, string commandText)
    {
        try
        {
            _historyService.AddHistoryEntry(commandText, database, serviceName);
        }
        catch (Exception ex)
        {
            _simpleLogger.TrackError(ex, isCrash: false);
        }
    }

    private void SetTimeoutForCommand(string localTitle, IDatabaseService service, DbCommand cmd, int? forcedTimeout = null)
    {
        if ((service.DatabaseType == DatabaseTypeEnum.NetezzaSQL || service.DatabaseType == DatabaseTypeEnum.NetezzaSQLOdbc) && !OperatingSystem.IsWindows())
        {
            AddLogMessage($"TO DO CommandTimeout on nonWindows", LogMessageType.ok, System.DateTime.Now, localTitle);
        }
        else
        {
            if (forcedTimeout is not null)
            {
                cmd.CommandTimeout = (int)forcedTimeout;
            }
            else if (service.DatabaseType == DatabaseTypeEnum.NetezzaSQL || service.DatabaseType == DatabaseTypeEnum.NetezzaSQLOdbc)
            {
                //to mitigate weird NZ timeout problem
                int random = Random.Shared.Next(0, (int)(_generalApplicationData.Config.CommandTimeout * 0.05));
                cmd.CommandTimeout = _generalApplicationData.Config.CommandTimeout + random;
            }
            else
            {
                cmd.CommandTimeout = _generalApplicationData.Config.CommandTimeout;
            }
        }
    }

    private readonly char[] _variavleEndings = [' ', '\r', '\n'];
    private void SelectConnectionFromContext()
    {
        if (SqlEditor.Text.Length > 50 && SqlEditor.Text.StartsWith("--"))
        {
            var startPart = SqlEditor.Text.AsSpan().Slice(2, 48);
            int index = startPart.IndexOfAny(_variavleEndings);
            if (index > 0)
            {
                ReadOnlySpan<char> word = startPart[..index];

                SelectedConnectionIndex = SqlDocumentViewModelHelper.GetConnectionIndex(word);
            }
        }
    }
    private void SharedCleanup()
    {
        _generalApplicationData.RemoveDocumentById(Id);
        _fileWatcher.EnableRaisingEvents = false;
        _fileWatcher.Dispose();

        try
        {
            Task.Run(() =>
            {
                try
                {
                    AbortSqlAsync().Wait(TimeSpan.FromSeconds(5));
                    _cachedDbConnection?.Connection?.Close();
                }
                catch (Exception)
                {
                    try
                    {
                        if (_cachedDbConnection?.Connection is not null && _cachedDbConnection.DbService is INetezzaDotnet dService)
                        {
                            dService.DropConnectionEmergencyModeAsync(_cachedDbConnection.Connection).Wait(TimeSpan.FromSeconds(5));
                        }
                    }
                    catch (Exception ex2)
                    {
                        _simpleLogger.TrackError(ex2, isCrash: false);
                        _messageForUserTools.ShowSimpleMessageBoxInstance(ex2.Message);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _simpleLogger.TrackError(ex, isCrash: false);
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex.Message);
        }
    }

    [RelayCommand]
    private async Task FormatSqlAsync()
    {
        if (!_generalApplicationData.IsFromatterAvaiable)
        {
            return;
        }
        IsReadOnly = true;
        try
        {
            if (SqlEditor.SelectionLength == 0)
            {
                SqlEditor.SelectAll();
            }

            string selectedSql = SqlEditor.SelectedText;
            int start = SqlEditor.SelectionStart;
            int len = SqlEditor.SelectionLength;
            var res = await _generalApplicationData.GetFormatterSql(selectedSql);
            SqlEditor.Document.Replace(start, len, res);
        }
        finally
        {
            IsReadOnly = false;
        }
    }

    public string? TextFromDocumentVM => SqlEditor?.Text;

}

