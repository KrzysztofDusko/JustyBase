using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using JustyBase.Common.Contracts;
using JustyBase.Database.Sample.ViewModels;
using JustyBase.Editor;
using JustyBase.Helpers;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginDatabaseBase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JustyBase.Database.Sample.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
        sqlCodeEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("SQL");
        this.Loaded += MainWindow_LoadedAsync;
    }

    private async void MainWindow_LoadedAsync(object? sender, RoutedEventArgs e)
    {
        var ds = (this.DataContext as MainWindowViewModel).GetTestDatabaseService();
        if (ds is not null)
        {
            sqlCodeEditor.Initialize(new TestAutocompleteData(ds), new TestOptions());
            sqlCodeEditor.FoldingSetup();
            sqlCodeEditor.ForceUpdateFoldings();
            sqlCodeEditor.CollapseFoldings();
        }
        else
        {
            var win = new Window()
            {
                Content = new ConnectionData()
                {
                    DataContext = new ConnectionDataViewModel(App.GetRequiredService<IEncryptionHelper>())
                },
                Width = this.Width,
                Height = this.Height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            await win.ShowDialog(this);
        }
    }
    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= (DragDropEffects.Link);
        if (!e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }
    private void Drop(object? sender, DragEventArgs e)
    {
        e.DragEffects &= (DragDropEffects.Link);

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

public sealed class TestAutocompleteData(IDatabaseService databaseService) : ISqlAutocompleteData
{
    private readonly AutocompleteService _autocompleteService = new AutocompleteService();
    private readonly IDatabaseService? _databaseService = databaseService;
    private readonly string _connectionName = databaseService.Name;
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

    public List<string> TypoPatternList { get; set; } = ["SELECT", "WHERE", "HAVING", "PARTITION", "BETWEEN"];

    public Dictionary<string, string> VariablesDictionary { get; set; } = [];

    public bool CollapseFoldingOnStartup => true;

    public bool IsFromatterAvaiable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Task<string> GetFormatterSql(string txt)
    {
        throw new NotImplementedException();
    }
}
