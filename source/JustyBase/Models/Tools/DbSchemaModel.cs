using CommunityToolkit.Mvvm.ComponentModel;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace JustyBase.Models.Tools;

public sealed partial class DbSchemaModel
{
    [ObservableProperty]
    public partial DbSchemaModel Self { get; set; }

    [ObservableProperty]
    public partial string Info { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsExpandedable { get; set; }

    private readonly BackgroundWorker _backgroundWorker = new();

    public DbSchemaModel(TypeInDatabaseEnum typeInDatabase, DatabaseTypeEnum databaseTypeEnum)
    {
        _backgroundWorker.DoWork += BackgroundWorker_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        DatabaseTypeEnumValue = databaseTypeEnum;
        ActualTypeInDatabase = typeInDatabase;
        IsExpandedable = GetExpInfo();
        Self = this;
    }

    private bool _blockExpanding = false;

    private bool GetExpInfo()
    {
        if (_blockExpanding)
            return false;

        return ActualTypeInDatabase switch
        {
            TypeInDatabaseEnum.ColumnDataType => false,
            TypeInDatabaseEnum.ColumnDataTypeNullInfo => false,
            TypeInDatabaseEnum.ColumnComment => false,
            TypeInDatabaseEnum.otherNoneEntry => false,
            _ => true
        };
    }

    private bool _initialized = false;
    public void ClearChildren()
    {
        _children.Clear();
        _initialized = false;
    }

    private string backgroudWorkerConnectionName = "";

    private readonly ObservableCollection<DbSchemaModel>? _children = [];
    public ObservableCollection<DbSchemaModel> Children
    {
        get
        {
            if (!_initialized && _children.Count == 0 && ActualTypeInDatabase == TypeInDatabaseEnum.Connection
                && DatabaseServiceHelpers.GetDatabaseConnectedLevel(Name) < DatabaseConnectedLevel.ConnectedDatabaseObjects
                )
            {
                _initialized = true;
                backgroudWorkerConnectionName = Name;
                _children.Add(new DbSchemaModel(TypeInDatabaseEnum.otherNoneEntry, this.DatabaseTypeEnumValue)
                {
                    Name = "Loading...",
                    Parent = this,
                    ConnectionName = this.ConnectionName
                });
                _backgroundWorker.RunWorkerAsync();
                
                return _children;
            }
            if (_children.Count == 0)
            {
                LoadChildren(_children);
            }

            return _children;
        }
    }

    private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        //to cache database connection
        _blockExpanding = true;
        Dispatcher.UIThread.Invoke(() => IsExpandedable = false);
        _ = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, backgroudWorkerConnectionName);
    }

    private void BackgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        //ObservableCollection<DbSchemaModel> childrens = [];
        try
        {
            _children.Clear();
            LoadChildren(_children);
            _blockExpanding = false;
            Dispatcher.UIThread.Invoke(() => IsExpandedable = GetExpInfo());
        }
        catch (Exception)
        {
            //ignore
        }
    }
}
