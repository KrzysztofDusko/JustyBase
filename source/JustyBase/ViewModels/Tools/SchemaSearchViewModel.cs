using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.Common.Models;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Models;
using JustyBase.PluginDatabaseBase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Tools;

public sealed partial class SchemaSearchViewModel : Tool
{
    private readonly IMessageForUserTools _messageForUserTools;
    private readonly LogToolViewModel _logToolViewModel;
    public SchemaSearchViewModel(IFactory factory, IGeneralApplicationData generalApplicationData, IMessageForUserTools messageForUserTools,
        LogToolViewModel logToolViewModel)
    {
        _generalApplicationData = generalApplicationData;
        _messageForUserTools = messageForUserTools;
        _logToolViewModel = logToolViewModel;
        this.Factory = factory;

        SchemaSearchItemCollections = [];
        RefreshDbCmd = new AsyncRelayCommand(RefreshDb);
        GridEnabled = true;

        SchemaSearchItems = new DataGridCollectionView(SchemaSearchItemCollections)
        {
            GroupDescriptions =
            {
                new DataGridPathGroupDescription(nameof(SchemaSearchItem.Type))
            },
            Filter = FilterView
        };

        ConnectionName = _generalApplicationData.Config.ConnectionNameInSchemaSearch;
        CaseSensitive = _generalApplicationData.Config.CaseSensitive;
        SearchInSource = _generalApplicationData.Config.SearchInSource;
        WholeWord = _generalApplicationData.Config.WholeWords;
        RegexMode = _generalApplicationData.Config.RegexMode;
        RefreshStartup = _generalApplicationData.Config.RefreshOnStartupInSchemaSearch;
    }

    public async Task DoubleTappedAction(SchemaSearchItem searchItem)
    {
        string[] toExpandPath = searchItem.GetPath(ConnectionName);

        if (toExpandPath.Length > 0)
        {
            DbSchemaViewModel? dbChemaViewModel = Factory.Find(a => a is DbSchemaViewModel).FirstOrDefault() as DbSchemaViewModel;
            await dbChemaViewModel?.ExpandToNodeFull(toExpandPath);
        }
    }


    public DataGridCollectionView SchemaSearchItems { get; set; }
    public List<SchemaSearchItem> SchemaSearchItemCollections { get; set; }
    public AsyncRelayCommand RefreshDbCmd { get; set; }

    private async Task RefreshDb()
    {
        if (ConnectionName is null)
        {
            ConnectionName = "ENTER NAME";
            return;
        }
        RefreshEnabled = false;
        GridEnabled = false;
        SchemaSearchItemCollections.Clear();
        try
        {
            if (_generalApplicationData.LoginDataDic.ContainsKey(ConnectionName))
            {
                _service = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName));
                if (_service is not null)
                {
                    var databases = _service.GetDatabases("");
                    await SearchLoop(databases);

                    if (SearchInSource)
                    {
                        await _service.CacheAllObjects(new TypeInDatabaseEnum[] { TypeInDatabaseEnum.Procedure,
                            TypeInDatabaseEnum.View, TypeInDatabaseEnum.ExternalTable, TypeInDatabaseEnum.Synonym
                    });
                    }
                    AfterOptionsChange();
                }
                else
                {
                    _messageForUserTools.ShowSimpleMessageBoxInstance("cannot connect to database", "Warning");
                }
            }
        }
        catch (Exception e)
        {
            _generalApplicationData.GlobalLoggerObject.TrackError(e, isCrash: false);
            _logToolViewModel.AddLog(e.Message, LogMessageType.error, "Error", DateTime.Now, "schema search");
        }
        finally
        {
            RefreshEnabled = true;
            GridEnabled = true;
        }
    }


    public void TryGoupResults(int groupLimit = GROUP_LIMIT)
    {
        if (DG is not null)
        {
            foreach (var item in SchemaSearchItems.Groups.OfType<DataGridCollectionViewGroup>())
            {
                if (item.ItemCount > groupLimit)
                {
                    DG.CollapseRowGroup(item, true);
                }
            }
        }
    }

    public string SearchText
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (searchTimer is null)
            {
                searchTimer = new Avalonia.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(50)
                };
                if (SchemaSearchItems.Count <= 100_000)
                {
                    searchTimer.Interval = TimeSpan.FromMilliseconds(50);
                }
                else if (SchemaSearchItems.Count <= 1_000_000 && !SearchInSource)
                {
                    searchTimer.Interval = TimeSpan.FromMilliseconds(200);
                }
                else if (SchemaSearchItems.Count <= 1_000_000 && !SearchInSource)
                {
                    searchTimer.Interval = TimeSpan.FromMilliseconds(200);
                }
                else
                {
                    searchTimer.Interval = TimeSpan.FromMilliseconds(500);
                }
                searchTimer.Tick += Timer_Tick;
            }
            searchTimer.Stop();
            searchTimer.Start();
        }
    }

    private Avalonia.Threading.DispatcherTimer searchTimer;
    private const int GROUP_LIMIT = 20;
    public const int GIANT_GROUP_LIMIT = 100;

    public Avalonia.Controls.DataGrid DG;
    private void Timer_Tick(object? sender, EventArgs e)
    {
        searchTimer.Stop();
        //GridEnabled = false;
        RefreshRegex();
        AfterOptionsChange();
        //GridEnabled = true;
    }

    private void MarkFilteredItems()
    {
        Parallel.ForEach(SchemaSearchItemCollections, item =>
        {
            if (!IsFilterOk(item))
            {
                item.FilterNotOk = true;
            }
            else
            {
                item.FilterNotOk = false;
            }
        });
    }

    [ObservableProperty]
    public partial bool RefreshEnabled { get; set; } = true;

    partial void AfterOptionsChange()
    {
        MarkFilteredItems();
        SchemaSearchItems.Refresh();
        TryGoupResults();
    }

    [ObservableProperty]
    public partial bool ShowSettings { get; set; }

    private bool FilterView(object arg)
    {
        if (arg is not SchemaSearchItem)
        {
            return false;
        }
        var item = arg as SchemaSearchItem;

        return !item.FilterNotOk;
        //IsFilterOk(item);
    }

    private bool IsFilterOk(SchemaSearchItem item)
    {
        if (SearchText is null && item.Type != "Column")
        {
            return true;
        }
        if (SearchText is null || SearchText.Length <= 2 && item.Type == "Column")
        {
            return false;
        }

        if (!string.IsNullOrEmpty(SearchText))
        {
            if (WholeWord || RegexMode)
            {
                if (RxWholeWorld is not null)
                {
                    return
                        ColumnFilters(item) && (
                        item.Name is not null && RxWholeWorld.IsMatch(item.Name) ||
                        item.Desc is not null && RxWholeWorld.IsMatch(item.Desc) ||
                        SearchInSource && (item.Type == "Procedure" || item.Type == "View" || item.Type == "External table" || item.Type == "Synonym")
                        && _service.IsItemSourceContains(DatabaseServiceHelpers.FromStringEx(item.Type), item.Db, item.Schema, item.Name, item.Id, _currentStringComparation, null, RxWholeWorld));
                }
            }
            else
            {
                return
                    ColumnFilters(item) && (
                    item.Name is not null && item.Name.Contains(SearchText, _currentStringComparation) ||
                    item.Desc is not null && item.Desc.Contains(SearchText, _currentStringComparation) ||
                    SearchInSource && (item.Type == "Procedure" || item.Type == "View" || item.Type == "External table" || item.Type == "Synonym")
                    && _service.IsItemSourceContains(DatabaseServiceHelpers.FromStringEx(item.Type), item.Db, item.Schema, item.Name, item.Id, _currentStringComparation, SearchText, null));
            }
        }
        return true;
    }
    private bool ColumnFilters(SchemaSearchItem item)
    {
        return (string.IsNullOrEmpty(TypeFilterString) || item.Type?.Contains(TypeFilterString, _currentStringComparation) == true)
            && (string.IsNullOrEmpty(NameFilterString) || item.Name?.Contains(NameFilterString, _currentStringComparation) == true)
            && (string.IsNullOrEmpty(DbFilterString) || item.Db?.Contains(DbFilterString, _currentStringComparation) == true)
            && (string.IsNullOrEmpty(DescFilterString) || item.Desc?.Contains(DescFilterString, _currentStringComparation) == true)
            && (string.IsNullOrEmpty(SchemaFilterString) || item.Schema?.Contains(SchemaFilterString, _currentStringComparation) == true)
            && (string.IsNullOrEmpty(OwnerFilterString) || item.Owner?.Contains(OwnerFilterString, _currentStringComparation) == true)
            ;
    }
}

