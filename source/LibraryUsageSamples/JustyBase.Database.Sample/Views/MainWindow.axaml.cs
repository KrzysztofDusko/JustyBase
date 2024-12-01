using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using System.Linq;
using System;
using JustyBase.Database.Sample.ViewModels;
using JustyBase.Editor;
using System.Threading.Tasks;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.PluginDatabaseBase;
using JustyBase.PluginCommon.Contracts;
using PluginDatabaseBase.Models;
using JustyBase.Common.Helpers;
using JustyBase.Helpers;
using System.IO;
using Parquet.Meta;

namespace JustyBase.Database.Sample.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
        sqlCodeEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("SQL");
        sqlCodeEditor.Initialize(new TestAutocompleteData(), new TestOptions());
        this.Loaded += MainWindow_Loaded;
    }
    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        sqlCodeEditor.FoldingSetup();
        sqlCodeEditor.ForceUpdateFoldings();
        sqlCodeEditor.CollapseFoldings();
    }
    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DragEffects & (DragDropEffects.Link);
        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }
    private void Drop(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DragEffects & (DragDropEffects.Link);
        
        if (e.Data.Contains(DataFormats.Files))
        {
            if (this.DataContext is not null)
            {
                var filenameX = e.Data.GetFiles();
                if (filenameX is not null)
                {
                    try
                    {
                        List<string> filenamesToOpen = filenameX.Select(o => o.Path.LocalPath).ToList();
                        foreach (var filePath in filenamesToOpen)
                        {
                            (this.DataContext as MainWindowViewModel)?.ImportFromPath(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (this.DataContext is MainWindowViewModel mainWindowViewModel)
                        {
                            mainWindowViewModel.Info += ex.Message;
                        }
                    }
                }
            }
        }
    }
}

public sealed class TestAutocompleteData : ISqlAutocompleteData
{
    private readonly AutocompleteService _autocompleteService = new AutocompleteService();
    private readonly IDatabaseInfo _databaseInfo = new DatabaseInfo();
    private IDatabaseService? _databaseService;
    private JustyBase.Services.Database.Oracle _oracle;// TODO

    public async IAsyncEnumerable<CompletionDataSql> GetWordsList(string input, Dictionary<string, List<string>> aliasDbTable, Dictionary<string, List<string>> subqueryHints,
        Dictionary<string, List<string>> withHints, Dictionary<string, List<string>> tempTables)
    {
        if (_oracle is not null && _databaseService is null)
            yield break;

        _oracle ??= new JustyBase.Services.Database.Oracle
            (_databaseInfo.LoginDataDic["OracleTest"].UserName!,
            _databaseInfo.LoginDataDic["OracleTest"].Password!,
            port:"???",
            _databaseInfo.LoginDataDic["OracleTest"].Server!,
            null!, connectionTimeout:10
            );



        _databaseService ??= await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_databaseInfo, "OracleTest"
            , ownDatabaseService : _oracle
            ));

        var wordsList = _autocompleteService.GetWordsList(input, aliasDbTable, subqueryHints, withHints, tempTables, _databaseService!, null);
        foreach (var item in wordsList)
        {
            yield return item;
        }
    }
}

public sealed class DatabaseInfo : IDatabaseInfo
{
    public ISimpleLogger GlobalLoggerObject => ISimpleLogger.EmptyLogger;

    public Dictionary<string, LoginDataModel> LoginDataDic { get; set; } = new Dictionary<string, LoginDataModel>()
    {
        { "OracleTest", new LoginDataModel()
            {
                ConnectionName = "OracleTest",
                DefaultIndex = 0,
                Driver = "Oracle",
                Password = EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("OracleTestPass")!),
                Server=EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("OracleTestServer")!),
                UserName= EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("OracleTestUser")!)
            }
        }
    };

    public string GetDataDir()
    {
        return Path.GetTempPath();
    }

    private readonly string _pluginDirectory = Environment.GetEnvironmentVariable("DEBUG_PLUGIN_BASE_PATH")!;
    public async Task LoadPluginsIfNeeded(Action? uiAction)
    {
        PluginLoadHelper.LoadPlugins(_pluginDirectory);
        await Task.CompletedTask;
    }
}

public sealed class TestOptions : ISomeEditorOptions
{
    public Dictionary<string, (string snippetType, string? Description, string? Text, string? Keyword)> GetAllSnippets { get; set; } = [];

    public Dictionary<string, string> FastReplaceDictionary { get; set; } = new Dictionary<string, string>() 
    { 
        { "SX", "SELECT" },
        { "FX", "FROM" },
        { "WX", "WHERE" },
    };

    public List<string> TypoPatternList { get; set; } = ["SELECT","WHERE","HAVING"];

    public Dictionary<string, string> VariablesDictStatic { get; set; } = [];

    public bool CollapseFoldingOnStartup => true;
}
