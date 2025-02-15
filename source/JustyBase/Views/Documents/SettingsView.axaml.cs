using JustyBase.Services;
using JustyBase.ViewModels.Documents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JustyBase.Views.Documents;

public partial class SettingsView : UserControl
{
    public static List<FontFamily> AvaiableFonts { get; set; } = [];

    static SettingsView()
    {
        var fontManager = FontManager.Current;
        if (App.Current.Resources["JetBrainsMono"] is FontFamily font)
        {
            AvaiableFonts.Add(font);
        }
        AvaiableFonts.AddRange(fontManager.SystemFonts);
    }
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    public SettingsView()
    {
        _avaloniaSpecificHelpers = App.GetRequiredService<IAvaloniaSpecificHelpers>();
        InitializeComponent();
        treeView.SelectionChanged += TreeView_SelectionChanged;
        btEditSnippets.Click += BtEditSnippets_Click;
        btOk.Click += BtOk_Click;

        fontDropDown.ItemsSource = AvaiableFonts;
        this.DataContextChanged += SettingsView_DataContextChanged;
        fontDropDown.SelectionChanged += FontDropDown_SelectionChanged;
    }

    private SettingsViewModel ViewModel => DataContext as SettingsViewModel;
    private void FontDropDown_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ViewModel.DocumentFontName = (fontDropDown.SelectedItem as FontFamily)?.Name ?? "Cascadia Code";
    }

    private void SettingsView_DataContextChanged(object? sender, EventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        foreach (var font in fontDropDown.ItemsSource.OfType<FontFamily>())
        {
            if (font.Name == ViewModel.DocumentFontName)
            {
                fontDropDown.SelectedItem = font;
                break;
            }
        }
    }

    private static Flyout _savedFlyout;
    private void BtOk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _savedFlyout ??= new Flyout
        {
            Content = new TextBlock
            {
                Text = $"Saved{Environment.NewLine}you can now close this tab"
            },
            FlyoutPresenterClasses =
            {
                "GoodVsibleFlyout"
            }
        };

        _savedFlyout.ShowAt(btOk);
    }

    private async void BtEditSnippets_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var sn = new SnippetWindow();
        await sn.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
    }

    private void TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var en = treeView.SelectedItems.GetEnumerator();
        en.MoveNext();
        var item = en.Current;
        if (item is TreeViewItem viewItem)
        {
            var panel = this.Find<Control>(viewItem.Tag.ToString());
            panel?.BringIntoView();
        }
    }
}
