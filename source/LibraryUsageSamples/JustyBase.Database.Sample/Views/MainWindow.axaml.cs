using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using System.Linq;
using System;
using JustyBase.Database.Sample.ViewModels;
using JustyBase.Editor;
using System.Threading.Tasks;
using JustyBase.PluginDatabaseBase.Database;
using PluginDatabaseBase.Models;
using JustyBase.Common.Helpers;
using JustyBase.Helpers;

namespace JustyBase.Database.Sample.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
        sqlCodeEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("SQL");
        sqlCodeEditor.Initialize(new TestAutocompleteData(GetTestDatabaseService("NetezzaTest")), new TestOptions());
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


    private IDatabaseService GetTestDatabaseService(string _connectionName)
    {
        IDatabaseService _databaseService = new JustyBase.Services.Database.NetezzaOdbc
            (LoginDataDic[_connectionName].UserName!,
            LoginDataDic[_connectionName].Password!,
            port: "5480",
            LoginDataDic[_connectionName].Server!,
            "JUST_DATA", connectionTimeout: 10
            );
        _databaseService.Name = _connectionName;
        return _databaseService;
    }

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
        },
            { "NetezzaTest", new LoginDataModel()
            {
                ConnectionName = "NetezzaTest",
                DefaultIndex = 0,
                Driver = "NetezzaOdbc",
                Password = EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("NetezzaTestPass")!),
                Server=EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("NetezzaTestServer")!),
                UserName= EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("NetezzaTestUser")!)
            }
        }
    };
}

public sealed class TestAutocompleteData : ISqlAutocompleteData
{
    private readonly AutocompleteService _autocompleteService = new AutocompleteService();
    private readonly IDatabaseService? _databaseService;
    private readonly string _connectionName;
    public TestAutocompleteData(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        _connectionName = databaseService.Name;
    }

    private bool _inProgress = false;
    public async IAsyncEnumerable<CompletionDataSql> GetWordsList(string input, Dictionary<string, List<string>> aliasDbTable, Dictionary<string, List<string>> subqueryHints,
        Dictionary<string, List<string>> withHints, Dictionary<string, List<string>> tempTables)
    {
        if (!_inProgress && _databaseService is not null)
        {
            _inProgress = true;
            _ = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(null, _connectionName, ownDatabaseService: _databaseService));
            _inProgress = false;
        }

        var wordsList = _autocompleteService.GetWordsList(input, aliasDbTable, subqueryHints, withHints, tempTables, _databaseService!, null);
        foreach (var item in wordsList)
        {
            yield return item;
        }
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
        { "sx", "select" },
        { "fx", "from" },
        { "wx", "where" },
    };

    public List<string> TypoPatternList { get; set; } = ["SELECT", "WHERE", "HAVING", "PARTITION","BETWEEN"];

    public Dictionary<string, string> VariablesDictStatic { get; set; } = [];

    public bool CollapseFoldingOnStartup => true;
}
