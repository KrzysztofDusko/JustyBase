using Avalonia.Controls;
using Avalonia.Input;
using JustyBase.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using JustyBase.Database.Sample.ViewModels;
using JustyBase.Editor;
using System.Threading.Tasks;
using AvaloniaEdit.Highlighting;

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


public class TestAutocompleteData : ISqlAutocompleteData
{
    public async IAsyncEnumerable<CompletionDataSql> GetWordsList(string input, Dictionary<string, List<string>> aliasDbTable, Dictionary<string, List<string>> subqueriesHints, Dictionary<string, List<string>> withs, Dictionary<string, List<string>> tempTables)
    {
        yield return new CompletionDataSql("abcdefghi", "desc", false, Editor.CompletionProviders.Glyph.None, null);
        yield return new CompletionDataSql("defghi", "desc", false, Editor.CompletionProviders.Glyph.None, null);
        yield return new CompletionDataSql("qwerty", "desc", false, Editor.CompletionProviders.Glyph.None, null);
        await Task.CompletedTask;
    }
}

public class TestOptions : ISomeEditorOptions
{
    public Dictionary<string, (string snippetType, string? Description, string? Text, string? Keyword)> GetAllSnippets { get; set; } =  [];

    public Dictionary<string, string> FastReplaceDictionary { get; set; } = [];

    public List<string> TypoPatternList { get; set; } = [];

    public Dictionary<string, string> VariablesDictStatic { get; set; } = [];

    public bool CollapseFoldingOnStartup => true;
}
