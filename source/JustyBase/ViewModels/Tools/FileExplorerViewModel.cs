using Avalonia.Collections;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.Common.Models;
using JustyBase.Converters;
using JustyBase.Models.Tools;
using JustyBase.Services;
using JustyBase.Views.OtherDialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Tools;

public partial class FileExplorerViewModel : Tool
{
    private readonly ISearchInFiles _searchInFiles;
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly IMessageForUserTools _messageForUserTools;
    private readonly LogToolViewModel _logToolViewModel;
    public FileExplorerViewModel(IFactory factory, ISearchInFiles searchInFiles, IAvaloniaSpecificHelpers avaloniaSpecificHelpers, IGeneralApplicationData generalApplicationData, IMessageForUserTools messageForUserTools,
        LogToolViewModel logToolViewModel)
    {
        this.Factory = factory;
        _searchInFiles = searchInFiles;
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        _generalApplicationData = generalApplicationData;
        _messageForUserTools = messageForUserTools;
        _logToolViewModel = logToolViewModel;
        RefreshFileListCmd = new AsyncRelayCommand(RefreshFileList);
        SearchInFilesCommand = new AsyncRelayCommand(DoSearchInFiles);
        OpenDirectoryDialogCmd = new AsyncRelayCommand(OpenDirectoryDialog);

        ShowInExplorerCommand = new RelayCommand(OpenInExplorer);
        OpenInExplorerGridCmd = new RelayCommand(OpenInExplorerGrid);
        RemoveFileOrDirectoryCmd = new AsyncRelayCommand(RemoveFileOrDirectory);

        using (var fileStream = AssetLoader.Open(new Uri("avares://JustyBase/Assets/file.png")))
        using (var folderStream = AssetLoader.Open(new Uri("avares://JustyBase/Assets/folder.png")))
        using (var folderOpenStream = AssetLoader.Open(new Uri("avares://JustyBase/Assets/folder-open.png")))
        {
            var fileIcon = new Bitmap(fileStream);
            var folderIcon = new Bitmap(folderStream);
            var folderOpenIcon = new Bitmap(folderOpenStream);

            _folderIconConverter = new FolderIconConverter(fileIcon, folderOpenIcon, folderIcon);
        }

        WholeWords = false;
        Source = new HierarchicalTreeDataGridSource<FileTreeNodeModel>([])
        {
            Columns =
                {
                    //new TemplateColumn<FileTreeNodeModel>(
                    //    null,
                    //    new FuncDataTemplate<FileTreeNodeModel>(FileCheckTemplate, true),
                    //    options: new ColumnOptions<FileTreeNodeModel>
                    //    {
                    //        CanUserResizeColumn = false,
                    //    }),
                    new HierarchicalExpanderColumn<FileTreeNodeModel>(
                        new TemplateColumn<FileTreeNodeModel>(
                            "Name",
                            new FuncDataTemplate<FileTreeNodeModel>(FileNameTemplate, supportsRecycling:false),
                            null,
                            new GridLength(1, GridUnitType.Star),
                            new TemplateColumnOptions<FileTreeNodeModel>
                            {
                                CompareAscending = FileTreeNodeModel.SortAscending(x => x.Name),
                                CompareDescending = FileTreeNodeModel.SortDescending(x => x.Name),
                                IsTextSearchEnabled = true,
                                TextSearchValueSelector = x => x.Name
                            }),
                        x => x.Children,
                        x => x.HasChildren,
                        x => x.IsExpanded),
                    new TextColumn<FileTreeNodeModel, string>(
                        "Size",
                        x => x.FormattedSize,
                        options: new TextColumnOptions<FileTreeNodeModel>
                        {
                            CompareAscending = FileTreeNodeModel.SortAscending(x => x.Size),
                            CompareDescending = FileTreeNodeModel.SortDescending(x => x.Size),
                        }),
                    new TextColumn<FileTreeNodeModel, DateTimeOffset?>(
                        "Modified",
                        x => x.Modified,
                        options: new TextColumnOptions<FileTreeNodeModel>
                        {
                            CompareAscending = FileTreeNodeModel.SortAscending(x => x.Modified),
                            CompareDescending = FileTreeNodeModel.SortDescending(x => x.Modified),
                        }),
            }
        };

        //https://github.com/AvaloniaUI/Avalonia.Controls.TreeDataGrid/blob/master/docs/selection.md
        //Source.Selection = new TreeDataGridCellSelectionModel<FileTreeNodeModel>(Source)
        //{
        //    SingleSelect = false
        //};

        SearchItemCollections = [];
        SearchItems = new DataGridCollectionView(SearchItemCollections)
        {
            GroupDescriptions =
            {
                    new DataGridPathGroupDescription(nameof(SearchItem.Type))
            },
            Filter = FilterView
        };
        //var sortOrder = DataGridSortDescription.FromPath("Last write time", ListSortDirection.Descending);
        //SearchItems.SortDescriptions.Add(sortOrder);

        if (_generalApplicationData.Config.StartsFolderPaths?.Count > 0 && Directory.Exists(_generalApplicationData.Config.StartsFolderPaths[0]))
        {
            InitialFilePath = string.Join(';', _generalApplicationData.Config.StartsFolderPaths);
        }
        else
        {
            InitialFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        _startupTimer.Interval = TimeSpan.FromSeconds(1);
        _startupTimer.Tick += Timer_Tick;
        _startupTimer.Start();
    }
    public HierarchicalTreeDataGridSource<FileTreeNodeModel> Source { get; }
    public DataGridCollectionView SearchItems { get; set; }
    public List<SearchItem> SearchItemCollections { get; set; }

    private readonly FolderIconConverter? _folderIconConverter;
    //private FileTreeNodeModel? _root;
    //private FileTreeNodeModel? _rootData;

    [ObservableProperty]
    public partial string InitialFilePath { get; set; }
    public string SearchText
    {
        get;
        set
        {
            SetProperty(ref field, value);
            SearchInFiles = false;
            SearchItems.Refresh();
        }
    }

    public ICommand RefreshFileListCmd { get; set; }
    public ICommand SearchInFilesCommand { get; set; }
    public ICommand OpenDirectoryDialogCmd { get; set; }
    public ICommand ShowInExplorerCommand { get; set; }
    public ICommand OpenInExplorerGridCmd { get; set; }
    public ICommand RemoveFileOrDirectoryCmd { get; set; }

    private readonly List<SearchItem> _filesList = [];
    private readonly List<SearchItem> _directoryList = [];

    private async Task RefreshFileList()
    {
        _filesList.Clear();
        _directoryList.Clear();
        await DoInitialSearch();
        SearchItemCollections.Clear();
        SearchItemCollections.AddRange(_filesList);
        SearchItemCollections.AddRange(_directoryList);
        SearchItems.Refresh();
        _messageForUserTools.DispatcherActionInstance(() =>
        {
            IsSearchInitializes = true;
            SearchItems.Refresh();
        });
    }

    [ObservableProperty]
    public partial bool IsSearchInitializes { get; set; }

    [ObservableProperty]
    public partial bool SearchInProgress { get; set; }

    [ObservableProperty]
    public partial bool WholeWords { get; set; }

    [ObservableProperty]
    public partial bool SearchInSqlComments { get; set; }

    [ObservableProperty]
    public partial object SelectedItem { get; set; }

    private const int SearchFileSizeLimit = 10 * 1024 * 1024;
    private bool SearchInFiles = false;
    private async Task DoSearchInFiles()
    {
        SearchInProgress = true;
        SearchInFiles = true;

        await Task.Run(() =>
        {
            Parallel.ForEach(SearchItemCollections, item =>
            {
                try
                {
                    string ext = System.IO.Path.GetExtension(item.Name).ToLower();
                    if (item.Type != "File")
                    {
                        item.IsFounded = false;
                    }
                    else if (Path.GetFileName(item.Name).Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        item.IsFounded = true;
                    }
                    else if (string.IsNullOrWhiteSpace(SearchText))
                    {
                        item.IsFounded = true;
                    }
                    else if (WholeWords && IGeneralApplicationData.REGISTERED_EXTENSIONS.ContainsKey(ext) && item.Length <= SearchFileSizeLimit)
                    {
                        item.IsFounded = _searchInFiles.IsWholeWordInFile(item.Name, SearchText, SearchInSqlComments);
                    }
                    else if (IGeneralApplicationData.REGISTERED_EXTENSIONS.ContainsKey(ext) && item.Length <= SearchFileSizeLimit)
                    {
                        item.IsFounded = _searchInFiles.IsWordInFile(item.Name, SearchText, SearchInSqlComments);
                    }
                }
                catch (Exception e)
                {
                    _logToolViewModel.AddLog(e.Message, LogMessageType.error, "Error", DateTime.Now, "file search");
                }
            }
            );
        });

        SearchInProgress = false;
        SearchItems.Refresh();
    }

    private async Task OpenDirectoryDialog()
    {
        var direcoryList = await _avaloniaSpecificHelpers.GetStorageProvider().OpenFolderPickerAsync(new FolderPickerOpenOptions() { AllowMultiple = false });
        if (direcoryList is null || direcoryList.Count < 1)
        {
            return;
        }
        var newAddedPath = direcoryList[0].Path.LocalPath;

        //OpenFolderDialog d = new OpenFolderDialog();
        //var path = await d.ShowAsync(JustyBase.Views.MainWindow.mainWindow);
        if (!string.IsNullOrWhiteSpace(newAddedPath) && Directory.Exists(newAddedPath))
        {
            _generalApplicationData.Config.StartsFolderPaths?.Clear();
            if (!string.IsNullOrWhiteSpace(InitialFilePath))
            {
                _generalApplicationData.Config.StartsFolderPaths = InitialFilePath.Split(';').ToList();
            }

            _generalApplicationData.Config.StartsFolderPaths.Add(newAddedPath);
            InitialFilePath = string.Join(';', _generalApplicationData.Config.StartsFolderPaths);

            if (Directory.Exists(newAddedPath))
            {
                InitTreeWithRoots();
            }
            await RefreshFileList();
        }
    }

    private void OpenInExplorer()
    {
        _messageForUserTools.ShowOrShowInExplorerHelper(Source.RowSelection.SelectedItem.Path);
    }

    public void OpenTxtPreviewFile(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        bool supportedExtension = IGeneralApplicationData.REGISTERED_EXTENSIONS.ContainsKey(ext);
        if (supportedExtension)
        {
            var atr = File.GetAttributes(path);
            if (!atr.HasFlag(FileAttributes.Directory) && File.Exists(path))
            {
                (this.Factory as DockFactory)?.AddNewDocumentFromFile([path]);
            }
        }
        else if (ext == ".csv" || ext == ".txt")
        {
            (this.Factory as DockFactory)?.AddNewDocumentFromTxtPreview(path);
        }

        else if (!supportedExtension)
        {
            _messageForUserTools.OpenInExplorerHelper(path);
        }
    }


    private void OpenInExplorerGrid()
    {
        if (SelectedItem is SearchItem searchItem)
        {
            _messageForUserTools.ShowOrShowInExplorerHelper(searchItem.Name);
        }
    }

    private async Task RemoveFileOrDirectory()
    {
        string path = Source.RowSelection.SelectedItem.Path;
        var d = new AskForConfirm();
        var vm = new AskForConfirmViewModel
        {
            Title = "Remove permanently?",
            TextMessage = $"{path}\r\n will be deleted from the disk permanently"
        };
        d.DataContext = vm;
        await d.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
        if (vm.ResultAsString == "Yes")
        {
            try
            {
                var atr = File.GetAttributes(path);
                if (atr.HasFlag(FileAttributes.Directory))
                {
                    Directory.Delete(path);
                }
                else
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                _generalApplicationData.GlobalLoggerObject.TrackError(ex, isCrash: false);
            }
        }
    }

    private void InitTreeWithRoots()
    {
        try
        {
            if (_generalApplicationData.Config.StartsFolderPaths is null)
            {
                return;
            }
            List<FileTreeNodeModel> arr = [];
            foreach (var dirPath in _generalApplicationData.Config.StartsFolderPaths)
            {
                arr.Add(new FileTreeNodeModel(dirPath, isDirectory: true, isRoot: true));
            }
            // _root = new FileTreeNodeModel(InitialFilePath, isDirectory: true, isRoot: true);
            var rootData = new FileTreeNodeModel(IGeneralApplicationData.DataDirectory, isDirectory: true, isRoot: true);
            arr.Add(rootData);
            Source.Items = arr.ToArray();
        }
        catch (Exception ex)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
            _generalApplicationData.GlobalLoggerObject.TrackError(ex, isCrash: false);
        }
    }


    private string GetShortStart(List<string> list)
    {
        if (list is null || list.Count == 0)
        {
            return "";
        }
        var res = list[0].AsSpan();

        for (int i = 1; i < list.Count; i++)
        {
            var tmp = list[i];
            for (int j = 0; j < res.Length && j < tmp.Length; j++)
            {
                if (res[j] != tmp[j])
                {
                    res = res[..j];
                    break;
                }
            }
        }

        return res.ToString();
    }

    private async Task DoInitialSearch()
    {
        var rootDirectoryList = _generalApplicationData.Config.StartsFolderPaths;
        if (rootDirectoryList is not null && rootDirectoryList.Count > 0)
        {
            await Task.Run(() =>
            {
                string shortStart = GetShortStart(rootDirectoryList);
                Stack<string> dirs = new Stack<string>(128);
                try
                {
                    foreach (string dane in rootDirectoryList)
                    {
                        dirs.Clear();
                        dirs.Push(dane);

                        int pozomPom = dane.Count(arg => arg == '\\');
                        while (dirs.Count > 0)
                        {
                            var akt = dirs.Pop();
                            string currentDir = akt;

                            if (!Directory.Exists(currentDir))
                            {
                                continue;
                            }

                            string[] subDirs = null;
                            try
                            {
                                subDirs = System.IO.Directory.GetDirectories(currentDir);
                                List<string> tmp = [];
                                for (int i = 0; i < subDirs.Length; i++)
                                {
                                    if (subDirs[i].Contains("\\."))
                                    {
                                        continue;
                                    }
                                    tmp.Add(subDirs[i]);
                                }
                                subDirs = tmp.ToArray();
                                tmp = null;
                            }
                            catch (UnauthorizedAccessException /*exc*/)
                            {
                                continue;
                            }
                            catch (DirectoryNotFoundException /*exc*/)
                            {
                                continue;
                            }
                            catch (Exception /*exc*/)
                            {
                                continue;
                            }

                            (string FullName, DateTime LastWriteTime, long Length)[] files = new DirectoryInfo(currentDir).GetFiles().OrderByDescending(f => f.LastWriteTime).Select(f => (f.FullName, f.LastWriteTime, f.Length)).ToArray();

                            foreach ((string FullName, DateTime LastWriteTime, long Length) in files)
                            {
                                string ext = System.IO.Path.GetExtension(FullName).ToLower();
                                if (ext is not null && (IGeneralApplicationData.REGISTERED_EXTENSIONS.ContainsKey(ext) || IGeneralApplicationData.ADDITIONAL_EXTENSIONS.Contains(ext))
                                )
                                {
                                    string fileName = System.IO.Path.GetFileName(FullName);
                                    _filesList.Add(new SearchItem()
                                    {
                                        Name = FullName,
                                        ShortName = fileName,
                                        ShortPath = FullName[shortStart.Length..^(fileName.Length)],
                                        Type = "File",
                                        LastWriteTime = LastWriteTime,
                                        IsFounded = true,
                                        Length = Length
                                    });
                                }
                            }

                            foreach (string dirPath in subDirs)
                            {
                                _directoryList.Add(new SearchItem()
                                {
                                    Name = dirPath,
                                    ShortName = System.IO.Path.GetFileName(dirPath),
                                    ShortPath = dirPath[shortStart.Length..],
                                    Type = "Directory",
                                    LastWriteTime = Directory.GetLastWriteTime(dirPath),
                                    IsFounded = true
                                }
                                );
                                dirs.Push(dirPath);
                            }
                        }
                    }
                }
                catch (Exception ex2)
                {
                    _messageForUserTools.ShowSimpleMessageBoxInstance(ex2);
                }
            });
        }
    }
    private void Timer_Tick(object? sender, EventArgs e)
    {
        _startupTimer.Stop();
        if (Directory.Exists(InitialFilePath))
        {
            InitTreeWithRoots();
        }
        _ = RefreshFileList();
    }

    private readonly DispatcherTimer _startupTimer = new DispatcherTimer();

    private bool FilterView(object arg)
    {
        if (arg is not SearchItem)
        {
            return false;
        }
        var item = arg as JustyBase.ViewModels.Tools.SearchItem;
        if (!SearchInFiles)
        {
            if (SearchText is null || item.ShortName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                item.IsFounded = true;
            }
            else
            {
                item.IsFounded = false;
            }
        }

        return item.IsFounded;
    }

    //private IControl FileCheckTemplate(FileTreeNodeModel node, INameScope ns)
    //{
    //    return new CheckBox
    //    {
    //        MinWidth = 0,
    //        [!CheckBox.IsCheckedProperty] = new Binding(nameof(FileTreeNodeModel.IsChecked)),
    //    };
    //}
    private Control FileNameTemplate(FileTreeNodeModel node, INameScope ns)
    {
        return new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
                {
                    new Image
                    {
                        [!Image.SourceProperty] = new MultiBinding
                        {
                            Bindings =
                            {
                                new Binding(nameof(node.IsDirectory)),
                                new Binding(nameof(node.IsExpanded)),
                            },
                            Converter = _folderIconConverter,
                        },
                        Margin = new Thickness(0, 0, 4, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new TextBlock
                    {
                        [!TextBlock.TextProperty] = new Binding(nameof(FileTreeNodeModel.Name)),
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                }
        };
    }
}

public sealed class SearchItem
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string ShortPath { get; set; }
    public long Length { get; set; }
    public DateTime? LastWriteTime { get; set; }
    public bool IsFounded { get; set; }
}
