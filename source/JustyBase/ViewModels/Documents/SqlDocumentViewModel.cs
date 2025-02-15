using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using JustyBase.Common.Contracts;
using JustyBase.Common.Models;
using JustyBase.Common.Services;
using JustyBase.Common.Tools.ImportHelpers;
using JustyBase.Common.Tools.ImportHelpers.XML;
using JustyBase.Editor;
using JustyBase.Helpers;
using JustyBase.Models.Tools;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommons;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.Services;
using JustyBase.Shared.Helpers;
using JustyBase.Themes;
using JustyBase.ViewModels.Tools;
using JustyBase.Views;
using JustyBase.Views.Documents;
using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Documents;

public sealed partial class SqlDocumentViewModel : DocumentBaseVM
{
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly LogToolViewModel _logToolViewModel;

    public SqlDocumentViewModel(IFactory factory, IClipboardService clipboardService, IAvaloniaSpecificHelpers avaloniaSpecificHelpers,
       IGeneralApplicationData generalApplicationData, HistoryService historyService,
       AutocompleteService autocompleteService, IMessageForUserTools messageForUserTools, ISimpleLogger simpleLogger,
       VariablesViewModel variablesViewModel, LogToolViewModel logToolViewModel)
    {
        _clipboardService = clipboardService;
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        _generalApplicationData = generalApplicationData;
        _historyService = historyService;
        _autocompleteService = autocompleteService;
        _messageForUserTools = messageForUserTools;
        _simpleLogger = simpleLogger;
        this.Factory = factory;
        _variablesViewModel = variablesViewModel;
        _logToolViewModel = logToolViewModel;

        if (factory is DockFactory dock1 && dock1.ActiveSqlDocumentViewModel is null)
        {
            dock1.ActiveSqlDocumentViewModel = this;
        }

        SqlDocumentViewModelHelper.SetConnectionList();
        RefreshConnectionList();

        WordWrap = false;


        CutCommand = new RelayCommand(() => SqlEditor?.Cut());
        CopyCommand = new RelayCommand(() => SqlEditor?.Copy());
        CopyWithFormatsCommand = new AsyncRelayCommand(CopyWithFormats);
        PasteCommand = new RelayCommand(() =>
        {
            SqlEditor?.Paste();
        });
        UndoCommand = new RelayCommand(() => SqlEditor?.Undo());
        RedoCommand = new RelayCommand(() => SqlEditor?.Redo());

        ContinueOnError = false;
        IsRunEnabled = true;
        PeriodicIntervalText = "00:00:10";
        VmSharedPreparation();
        InserTextAction = InsertTextRequest;
    }
    public void InsertTextRequest(object data, bool rawMode)
    {
        //SqlEditor.Focus();
        SqlEditor.TextArea?.Focus();
        SqlEditor.SelectedText = "";
        if (rawMode)
        {
            SqlEditor.Document.Insert(SqlEditor.CaretOffset, data.ToString());
        }
        else
        {
            string res = StringExtension.ConvertAsSqlCompatybile(data);
            SqlEditor.Document.Insert(SqlEditor.CaretOffset, res);
        }
    }

    private void LoadTextFromChangedFile(string? filePath)
    {
        _messageForUserTools.DispatcherActionInstance(() => SqlEditor.Text = File.ReadAllText(filePath), DispatcherPriority.MaxValue);
    }

    [ObservableProperty]
    public partial SqlCodeEditor SqlEditor { get; set; }

    partial void OnSqlEditorChanged(SqlCodeEditor value)
    {
        value.TextArea.Caret.PositionChanged += (_, _) => ActualDockFactory?.AtCharAction(GetCarretInfo());
        value.KeyDown += MarkTabEdited;
        value.TextArea.GotFocus += (_, _) => (Factory as DockFactory)?.ResultsFromActiveTab(this);
        value.TextArea.GotFocus += (_, _) => SqlCodeEditorHelpers.LastFocusedEditor = value;
        value.GotFocus += (_, _) => SqlCodeEditorHelpers.LastFocusedEditor = value;
        value.ContolShiftvAction = ImportFromClipboardAsync;
        value.GoToLineAsyncAction = () => GoToLineAsyncAction();
        value.Initialize(this, _generalApplicationData);

        if (_generalApplicationData.TryGetDocumentById(Id, out var offlineTabData) && offlineTabData.SqlText is not null)
        {
            value.Document.Text = offlineTabData.SqlText;
            //SqlEditor.AppendText(offlineTabData.SqlText);
        }

        Preparations();

        ResetFontStyle = () => _messageForUserTools.DispatcherActionInstance(() => ResetFontInView());
        ResetFontStyle.Invoke();
    }

    private void ResetFontInView()
    {
        try
        {
            foreach (var font in SettingsView.AvaiableFonts)
            {
                if (font.Name == _generalApplicationData.Config.DocumentFontName)
                {
                    SqlEditor.FontFamily = font;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _generalApplicationData.GlobalLoggerObject.TrackCrashMessagePlusOpenNotepad(ex, "font problem", isCrash: false);
        }
    }


    private void MarkTabEdited(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.LeftCtrl || Title.EndsWith('*'))
        {
            return;
        }
        Title += "*";
    }

    private static async Task<int> GoToLineAsyncAction()
    {
        var d = new JustyBase.Views.OtherDialogs.AskForFileName(gotoLine: true);
        await d.ShowDialog(App.GetRequiredService<IAvaloniaSpecificHelpers>().GetMainWindow());
        _ = int.TryParse(d.ReturnedName, out var res);
        return res;
    }

    private async Task CopyWithFormats()
    {
        try
        {
            var highlighter = new AvaloniaEdit.Highlighting.DocumentHighlighter(SqlEditor.Document,
                AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("SQL"));

            var baseHtmlText = AvaloniaEdit.Highlighting.HtmlClipboard.CreateHtmlFragment(SqlEditor.Document, highlighter,
                new SimpleSegment(SqlEditor.SelectionStart, SqlEditor.SelectionLength),
                new AvaloniaEdit.Highlighting.HtmlOptions(SqlEditor.TextArea.Options));

            string bcgColor = "white";
            string frColor = "black";
            if (FluentThemeManager.IsDark)
            {
                bcgColor = "black";
                frColor = "white";
            }

            CopyHtmlOrTextClipboard copyHtmlOrTextClipboard = new(SqlEditor.SelectedText,
                $"<br/><div style=\"border-radius: 5px;border: 1px dashed gray; padding: 15px; background-color:{bcgColor};color:{frColor};\">{baseHtmlText}</div><br/>"
                );
            await _avaloniaSpecificHelpers.GetClipboard().SetDataObjectAsync(copyHtmlOrTextClipboard);
        }
        catch (Exception ex)
        {
            _simpleLogger.TrackError(ex, isCrash: false);
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
        }
    }

    private string GetTile()
    {
        return Title;
    }

    private void ReturnPhase()
    {
        if (!IsRunEnabled)
        {
            IsRunEnabled = true;
        }
        if ((this.Factory as DockFactory)?.IsActiveDockable(this) == false)
        {
            IsRecentlyFinished = true;
        }
    }

    public bool TxtPreview { get; set; }

    [ObservableProperty]
    public partial bool ShowDetails { get; set; }

    public ObservableCollection<string> DatabasesList => SelectedConnectionIndex == -1 || SelectedConnectionIndex >= SqlDocumentViewModelHelper.ConnectionsList.Count
        ? [] :
        SqlDocumentViewModelHelper.ConnectionsList[SelectedConnectionIndex].DatabaseList;


    public ICommand CutCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand CopyWithFormatsCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }

    [RelayCommand]
    private async Task ImportFromClipboardAsync()
    {
        var formats = await _clipboardService.GetFormatsAsync();
        if (formats.Contains("XML Spreadsheet") || formats.Contains("Text"))
        {
            AddLogMessage("waiting for database service", LogMessageType.ok, System.DateTime.Now, Title);

            IDatabaseService service = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionName));
            if (service is null)
            {
                return;
            }

            if (service is INetezza && service.Connection is not null && SelectedDatabase != service.Connection.Database)
            {
                service.ChangeDatabaseSpecial(service.Connection, SelectedDatabase);
            }

            AddLogMessage("import in progress", LogMessageType.ok, System.DateTime.Now, Title);
            string res = "";
            AddLogMessage("gathering data from clipboard", LogMessageType.ok, System.DateTime.Now, Title);
            if (formats.Contains("XML Spreadsheet"))
            {
                object xmlData = await _clipboardService.GetDataAsync("XML Spreadsheet");
                if (xmlData is byte[] xmlBytes)
                {
                    res = await service.PerformImportFromXmlAsync(new DbXMLImportJob(), xmlBytes,
                        (s) =>
                        {
                            _messageForUserTools.DispatcherActionInstance
                            (
                                () =>
                                AddLogMessage(s, LogMessageType.ok, System.DateTime.Now, Title)
                            );
                        });
                }
            }
            else
            {
                string textData = await _clipboardService.GetTextAsync();
                string path = Path.GetTempFileName();
                File.WriteAllText(path, textData);
                var _currentImport = new ImportFromExcelFile(x => _messageForUserTools.ShowSimpleMessageBoxInstance(x), _simpleLogger)
                {
                    FilePath = path
                };

                if (!_currentImport.InitImport(encoding: Encoding.UTF8))
                {
                    AddLogMessage($"IMPORT FAILED to {res}", LogMessageType.error, System.DateTime.Now, Title);
                    return;
                }

                string randomName = StringExtension.RandomSuffix("IMP_");
                try
                {
                    await _currentImport.ImportFromFileAllSteps(service.DatabaseType, service, "", randomName);
                    res = randomName;
                }
                catch (Exception ex)
                {
                    _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                    _simpleLogger.TrackError(ex, isCrash: false);
                }

                try
                {
                    File.Delete(path);
                }
                catch (Exception)
                {
                    _currentImport.DoFileDispose();
                    File.Delete(path);
                }
            }
            AddLogMessage($"imported to {res}", LogMessageType.ok, System.DateTime.Now, Title);
            SqlEditor.Document.Insert(SqlEditor.TextArea.Caret.Offset, res);
        }
    }

    public void SelectedTabAction()
    {
        if (SqlEditor is null)
        {
            return;
        }
        Task.Run(() =>
        {
            Task.Delay(20).Wait();
            _messageForUserTools.DispatcherActionInstance(() =>
            {
                //SqlEditor?.Focus();
                SqlEditor?.TextArea?.Focus();
            });
        });
    }

    public override void OnSelected()
    {
        base.OnSelected();
        //SqlEditor.TextArea.GotFocus += (_, _) => (Factory as DockFactory)?.ResultsFromActiveTab(this);
        SelectedTabAction();
    }
    public void InserTextToEditor(object data, bool rawMode)
    {
        InserTextAction?.Invoke(data, rawMode);
    }

    [RelayCommand]
    private async Task AbortSqlAsync()
    {
        int actualQueryNum = _globalQueryNumber;
        int pevAbortUbound = _globalAbortUBound;
        _globalAbortUBound = actualQueryNum;

        OnPropertyChanged(nameof(TasksToAbort));

        await AbortSqlHelper(pevAbortUbound);
    }

    public DockFactory ActualDockFactory => (this.Factory as DockFactory);

    //public Interaction<SqlParametrViewModel, string?> ShowParametrDialog { get; }

    [ObservableProperty]
    public partial bool RunEvery { get; set; }

    [ObservableProperty]
    public partial string PeriodicIntervalText { get; set; }

    private DispatcherTimer _periodicTimer;

    [RelayCommand]
    private void RunSqlInTimer(string? option)
    {
        if (_periodicTimer is null)
        {
            _periodicTimer = new DispatcherTimer();
            _periodicTimer.Tick += (_, _) =>
            {
                RunSqlCommand.Execute("Grid");//.Wait();
                _periodicTimer.Stop();
                if (TimeSpan.TryParse(PeriodicIntervalText, out TimeSpan ts))
                {
                    _periodicTimer.Interval = ts;
                    _periodicTimer.Start();
                }
                else
                {
                    RunEvery = false;
                    PeriodicIntervalText = "00:00:10";
                }
            };
        }
        if (RunEvery)
        {
            _periodicTimer.Interval = TimeSpan.FromSeconds(3);
            _periodicTimer.Start();
        }
        else
        {
            _periodicTimer.Stop();
        }
    }

    private void PluginsDownloadInfo()
    {
        var mv = _avaloniaSpecificHelpers.GetMainWindow();
        mv.IsEnabled = !mv.IsEnabled;
    }

    private void AddWarningResult(string? localTitle, int actualQueryNum, IDatabaseService? actualDatabaseService)
    {
        ActualDockFactory.AddNewResult((actualDatabaseService, null, "cannot establish connection"), this.Id, actualQueryNum, ref _globalAbortUBound, null, null, localTitle);
    }

    private void ClosePreviousResultyIfNeeded()
    {
        ActualDockFactory.ClosePrevResults(this.Id);
    }

    private void HandleStandardGrid(IDatabaseService actualDatabaseService, string? resTitle, string? query, LogMessage currentLogMessage, bool TABS_WITH_ROWS,
        int actualQueryNum, DbDataReader rdr, DbCommand cmd, string? shortQuery)
    {

        if (shortQuery.StartsWith("--REGION RESULT_NAME:"))
        {
            int start = "--REGION RESULT_NAME:".Length;
            int ind = shortQuery.IndexOf(' ', start);
            if (ind != -1)
            {
                resTitle = shortQuery[start..ind];
            }
        }
        if (rdr.HasRows)
        {
            currentLogMessage?.AddInnerMessageInUiThread($"loaded rows from  [{shortQuery} ...]", System.DateTime.Now);
        }
        if (rdr.HasRows || !TABS_WITH_ROWS)
        {
            ActualDockFactory.AddNewResult((actualDatabaseService, rdr, ""), this.Id, actualQueryNum, ref _globalAbortUBound, query, cmd, resTitle);
        }
    }
    private void HandleAnotherResult(LogMessage currentLogMessage, DbDataReader rdr)
    {
        currentLogMessage?.AddInnerMessageInUiThread($"records affected {rdr.RecordsAffected:N0}", System.DateTime.Now);
    }
    private void ErrorMessageToUi(string? localTitle, LogMessage currentLogMessage, int actualQueryNum, IDatabaseService actualDatabaseService, int currentSqlNumber, string? sql, DbCommand cmd, Exception exx1)
    {
        int commandLength = cmd.CommandText.Length;
        commandLength = Math.Min(commandLength, 100);
        string shortQuery = cmd.CommandText[..commandLength].Trim().Replace("\n", " ").Replace("\r", " ");
        string resTitle = $"{localTitle}_{currentSqlNumber}";
        if (shortQuery.StartsWith("--REGION RESULT_NAME:"))
        {
            int start = "--REGION RESULT_NAME:".Length;
            int ind = shortQuery.IndexOf(' ', start);
            if (ind != -1)
            {
                resTitle = shortQuery[start..ind];
            }
        }
        if (exx1.Message != "ERROR: Query was cancelled.")
        {
            ActualDockFactory.AddNewResult((actualDatabaseService, null, exx1.Message), this.Id, actualQueryNum, ref _globalAbortUBound, sql, null, resTitle);
        }
        currentLogMessage?.AddInnerMessageInUiThread($"⛔ {exx1.Message}", System.DateTime.Now);
        if (currentLogMessage is not null)
        {
            currentLogMessage.MessageType = LogMessageType.error;
        }
    }

    private async Task<string> ChoseCsvPath(CompressionEnum csvEnum)
    {
        string path;
        path = csvEnum switch
        {
            CompressionEnum.None => await GetPathPathFromUser("csv files", "*.csv", "csv"),
            CompressionEnum.Brotli => await GetPathPathFromUser("csv files", "*.csv.br", "csv.br"),
            CompressionEnum.Gzip => await GetPathPathFromUser("csv files", "*.csv.gz", "csv.gz"),
            CompressionEnum.Zstd => await GetPathPathFromUser("csv files", "*.csv.zst", "csv.zst"),
            CompressionEnum.Zip => await GetPathPathFromUser("csv files", "*.csv.zip", "csv.zip"),
            _ => throw new NotImplementedException()
        };
        return path;
    }
    private async Task<string> ChoseParquetPath()
    {
        var path = await GetPathPathFromUser("parquet files", "*.parquet", "parquet");
        return path;
    }
    private async Task<string> ChoseXlsbPath()
    {
        var path = await GetPathPathFromUser("excel files", "*.xlsb", "xlsb");
        return path;
    }

    private async Task<string> GetPathPathFromUser(string? ft, string? pattern, string? defaultExtension)
    {
        var saveFile = await _avaloniaSpecificHelpers.GetStorageProvider().SaveFilePickerAsync(
            new FilePickerSaveOptions()
            {
                FileTypeChoices = [new(ft) { Patterns = [pattern] }],
                DefaultExtension = defaultExtension,
                ShowOverwritePrompt = true
            });
        string path = "";
        if (saveFile is null)
        {
            path = null;
        }
        else
        {
            path = saveFile.Path.LocalPath;
        }

        return path;
    }

    private void AddLogMesage(LogMessage logItem)
    {
        LogItems?.Add(logItem);
        _logToolViewModel.AddLog(logItem);
    }

    private async Task HandleVariableDialog(SqlParametrWindow paramWindow)
    {
        await paramWindow.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
    }

    private async Task ExpandTo(string[] toExpandPath)
    {
        DbSchemaViewModel? dbChemaViewModel = Factory.Find(a => a is DbSchemaViewModel).FirstOrDefault() as DbSchemaViewModel;
        await dbChemaViewModel?.ExpandToNodeFull(toExpandPath);
        await Task.Delay(200);
    }

    public string FilePath
    {
        get;
        set
        {
            SetProperty(ref field, value);
            Preparations();
        }
    }

    private bool _stopReloadFileOnSaving = false;
    public void Preparations()
    {
        _fileWatcher.EnableRaisingEvents = false;
        if (!_stopReloadFileOnSaving && SqlEditor is not null && FilePath is not null && File.Exists(FilePath))
        {
            using (FileStream fs = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                SqlEditor.Load(fs);
            }
            var fileExtension = Path.GetExtension(FilePath)[1..].ToUpperInvariant();
            SqlEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition(fileExtension);

            SqlEditor.FoldingSetup();
            SqlEditor.ForceUpdateFoldings();
            SqlEditor.CollapseFoldings();
            SelectConnectionFromContext();
        }
        else if (SqlEditor is not null && TxtPreview)
        {
            SqlEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("TXT");
        }
        else if (SqlEditor is not null)
        {
            SqlEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("SQL");
            SqlEditor.FoldingSetup();
        }
        MakeWatcher(FilePath);
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var openFile = await _avaloniaSpecificHelpers.GetStorageProvider().OpenFilePickerAsync(
        new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            FileTypeFilter =
            [
                 new FilePickerFileType("sql files") { Patterns = ["*.sql"] } ,
                 new FilePickerFileType("all files") { Patterns = ["*"] }
            ]
        });
        if (openFile.Count == 0)
        {
            return;
        }

        string filepath = openFile[0].Path.LocalPath;

        if (_generalApplicationData.TryGetOpenedDocumentVmByFilePath(filepath, out _))
        {
            return;
        }

        if (filepath is not null && !string.IsNullOrWhiteSpace(filepath))
        {
            FilePath = filepath;
            Title = Path.GetFileName(FilePath);
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync(string? option)
    {
        var editor = SqlEditor;
        if (editor is null)
        {
            return;
        }
        string fileFullPath = null;
        if (FilePath is null || option == "SaveAs")
        {
            var saveFile = await _avaloniaSpecificHelpers.GetStorageProvider().SaveFilePickerAsync(
            new FilePickerSaveOptions()
            {
                FileTypeChoices = [new FilePickerFileType("sql files") { Patterns = ["*.sql"] }],
                DefaultExtension = "sql",
                ShowOverwritePrompt = true
            });

            if (saveFile?.Path is not null)
            {
                fileFullPath = saveFile?.Path.LocalPath;
            }
        }
        else
        {
            fileFullPath = FilePath;
        }

        _fileWatcher.EnableRaisingEvents = false;
        if (fileFullPath is not null)
        {
            try
            {
                using StreamWriter fs = new(fileFullPath, false, System.Text.Encoding.UTF8);
                editor.Document.WriteTextTo(fs);
            }
            catch (Exception ex)
            {
                _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
            }
        }
        if (fileFullPath is not null)
        {
            Title = Path.GetFileName(fileFullPath);
        }

        if (FilePath != fileFullPath)
        {
            _stopReloadFileOnSaving = true;
            FilePath = fileFullPath;
            _stopReloadFileOnSaving = false;
        }
        if (File.Exists(FilePath))
        {
            _fileWatcher.EnableRaisingEvents = true;
        }
    }
    public string TitleFromDocumentVm => Title;

    public void RemoveAsterixFromTitleFromDocumentVM()
    {
        if (Title?.EndsWith('*') == true && (Title.Length > 1))
        {
            Title = Title[..^1] ?? "NO TITLE FOUND !";
        }
    }

    public void DoCleanup()
    {
        _periodicTimer?.Stop();
        SharedCleanup();
    }
}
