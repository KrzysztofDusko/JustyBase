using JustyBase.Models.Tools;
using JustyBase.ViewModels.Tools;
using System.IO;

namespace JustyBase.Views.Tools;

public partial class FileExplorerView : UserControl
{
    public FileExplorerView()
    {
        InitializeComponent();
        fileSearchGrid.DoubleTapped += Fs_DoubleTapped;
        fileSearchGrid.Loaded += FileSearchGrid_Loaded;
        fileViewer.DoubleTapped += FileViewer_DoubleTapped;
    }
    private FileExplorerViewModel ViewModel => DataContext as FileExplorerViewModel;
    private void FileSearchGrid_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        fileSearchGrid.Columns[0].Sort(System.ComponentModel.ListSortDirection.Descending);
    }

    private void FileViewer_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (fileViewer.RowSelection.SelectedItem is FileTreeNodeModel selRow)
        {
            ViewModel?.OpenTxtPreviewFile(selRow.Path);
        }
    }
    private void Fs_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (fileSearchGrid.SelectedItem is SearchItem searchItem)
        {
            string path = searchItem.Name;
            if (searchItem.Type == "File" && File.Exists(path))
            {
                ViewModel?.OpenTxtPreviewFile(path);
            }
        }
    }
}
