using CommunityToolkit.Mvvm.ComponentModel;
using JustyBase.Helpers.Interactions;
using JustyBase.PluginCommon.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace JustyBase.Models.Tools;

public partial class FileTreeNodeModel : ObservableObject
{

    private FileSystemWatcher? _watcher;
    private ObservableCollection<FileTreeNodeModel>? _children;

    private static readonly ISimpleLogger _simpleLogger;

    static FileTreeNodeModel()
    {
        _simpleLogger = App.GetRequiredService<ISimpleLogger>();
    }

    public FileTreeNodeModel(
        string path,
        bool isDirectory,
        bool isRoot = false)
    {
        Path = path;
        Name = isRoot ? path : System.IO.Path.GetFileName(Path);
        IsExpanded = false;
        IsDirectory = isDirectory;

        if (!isDirectory)
        {
            var info = new FileInfo(path);
            Size = info.Length;
            Modified = info.LastWriteTimeUtc;
        }
    }

    [ObservableProperty]
    public partial string Path { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial long? Size { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? Modified { get; set; }

    [ObservableProperty]
    public partial bool HasChildren { get; set; } = true;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public string FormattedSize
    {
        get
        {
            if (Size is null)
            {
                return string.Empty;
            }
            else
            {
                if (Size <= 1024 * 1024)
                {
                    double l = (double)(Size / 1024.0);
                    return l.ToString("N0") + " KB";
                }
                else
                {
                    double l = (double)(Size / 1024.0 / 1024.0);
                    return l.ToString("N1") + " MB";
                }
            }
        }
    }


    public bool IsDirectory { get; }
    public IReadOnlyList<FileTreeNodeModel> Children => _children ??= LoadChildren();

    private ObservableCollection<FileTreeNodeModel> LoadChildren()
    {
        if (!IsDirectory)
        {
            throw new NotSupportedException();
        }

        var options = new EnumerationOptions { IgnoreInaccessible = true };
        var result = new ObservableCollection<FileTreeNodeModel>();

        foreach (var d in Directory.EnumerateDirectories(Path, "*", options))
        {
            result.Add(new FileTreeNodeModel(d, true));
        }

        foreach (var f in Directory.EnumerateFiles(Path, "*", options))
        {
            var pp = new FileTreeNodeModel(f, false)
            {
                HasChildren = false
            };
            result.Add(pp);
        }

        _watcher = new FileSystemWatcher
        {
            Path = Path,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnCreated;
        _watcher.Deleted += OnDeleted;
        _watcher.Renamed += OnRenamed;
        try
        {
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            _simpleLogger.TrackError(ex, isCrash: false);
        }


        if (result.Count == 0)
            HasChildren = false;

        return result;
    }

    public static Comparison<FileTreeNodeModel?> SortAscending<T>(Func<FileTreeNodeModel, T> selector)
    {
        return (x, y) =>
        {
            if (x is null && y is null)
                return 0;
            else if (x is null)
                return -1;
            else if (y is null)
                return 1;
            if (x.IsDirectory == y.IsDirectory)
                return Comparer<T>.Default.Compare(selector(x), selector(y));
            else if (x.IsDirectory)
                return -1;
            else
                return 1;
        };
    }

    public static Comparison<FileTreeNodeModel?> SortDescending<T>(Func<FileTreeNodeModel, T> selector)
    {
        return (x, y) =>
        {
            if (x is null && y is null)
                return 0;
            else if (x is null)
                return 1;
            else if (y is null)
                return -1;
            if (x.IsDirectory == y.IsDirectory)
                return Comparer<T>.Default.Compare(selector(y), selector(x));
            else if (x.IsDirectory)
                return -1;
            else
                return 1;
        };
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Changed && File.Exists(e.FullPath))
        {
            MessageForUserTools.DispatcherAction(() =>
            {
                foreach (var child in _children!)
                {
                    if (child.Path == e.FullPath)
                    {
                        if (!child.IsDirectory)
                        {
                            try
                            {
                                var info = new FileInfo(e.FullPath);
                                child.Size = info.Length;
                                child.Modified = info.LastWriteTimeUtc;
                            }
                            catch (Exception ex)
                            {
                                _simpleLogger.TrackError(ex, isCrash: false);
                            }
                        }
                        break;
                    }
                }
            });
        }
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (File.Exists(e.FullPath) || Directory.Exists(e.FullPath))
        {
            MessageForUserTools.DispatcherAction(() =>
            {
                var node = new FileTreeNodeModel(e.FullPath,
                    File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory));
                _children!.Add(node);
            });
        }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        MessageForUserTools.DispatcherAction(() =>
        {
            for (var i = 0; i < _children!.Count; ++i)
            {
                if (_children[i].Path == e.FullPath)
                {
                    _children.RemoveAt(i);
                    System.Diagnostics.Debug.WriteLine($"Removed {e.FullPath}");
                    break;
                }
            }
        });
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        MessageForUserTools.DispatcherAction(() =>
        {
            foreach (var child in _children!)
            {
                if (child.Path == e.OldFullPath)
                {
                    child.Path = e.FullPath;
                    child.Name = e.Name ?? string.Empty;
                    break;
                }
            }
        });
    }
}
