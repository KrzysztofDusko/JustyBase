using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.Converters;
using JustyBase.Models.Tools;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.Services.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Tools;
public sealed partial class DbSchemaViewModel : Tool
{
    public HierarchicalTreeDataGridSource<DbSchemaModel> SchemaSource { get; }

    private TypeInDatabaseEnum GetSelectedType(object data) // evo..
    {
        if (SchemaSource.RowSelection.SelectedItem is null)
        {
            MenuItems = TodoMenuItems;
            return TypeInDatabaseEnum.otherNoneEntry;
        }
        return SchemaSource.RowSelection.SelectedItem.ActualTypeInDatabase;
    }

    public Action FocusAndBringSelectionIntoView;

    private void ExpandToNodeTest(string[] path, bool doExtraAction)
    {
        int[] indexes = new int[path.Length];
        int tempNum = 0;
        DbSchemaModel tempDbModel = null;
        IEnumerable<DbSchemaModel> tempDbModels = SchemaSource.Items;

        try
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                if (i > 0)
                {
                    tempDbModels = tempDbModel.Children;
                }
                tempNum = 0;
                foreach (var item in tempDbModels)
                {
                    if (item.Name == path[i])
                    {
                        indexes[i] = tempNum;
                        tempDbModel = item;
                        break;
                    }
                    tempNum++;
                }
            }
            SchemaSource.Expand(new IndexPath(indexes));
            if (doExtraAction)
            {
                SchemaSource.RowSelection.Select(new IndexPath(indexes));
                FocusAndBringSelectionIntoView?.Invoke();
            }

            //SchemaSource.RowSelection.AnchorIndex = new IndexPath(indexes);
            //SchemaSource.Selection = new SchemaSelection(tempDbModel);
            //SchemaSource.RowSelection.SelectedIndex = new IndexPath(indexes);
        }
        catch (Exception ex)
        {
            _simpleLogger?.TrackError(ex, isCrash: false);
        }
    }

    public async Task ExpandToNodeFull(string[] toExpandPath)
    {
        ShowThis();
        ExpandToNodeTest(toExpandPath, false);
        await Task.Delay(150);
        ExpandToNodeTest(toExpandPath, false);
        await Task.Delay(150);
        ExpandToNodeTest(toExpandPath, true);
    }

    private DbSchemaModel LastItemConrtextMenuReq => SchemaSource.RowSelection.SelectedItem;

    public void ShowThis()
    {
        try
        {
            if (this.Owner is ToolDock toolDock)
            {
                toolDock.ActiveDockable = this;
            }
        }
        catch (Exception)
        {
        }
    }

    private MenuItem GetMenuSeparator() => new() { Header = "-" };

    private void ShowConnectedOnly()
    {
        if (!ConnectedOnly)
        {
            for (int i = 0; i < _connectionCollection.Count; i++)
            {
                if (!DatabaseServiceHelpers.IsDatabaseConnected(_connectionCollection[i].Name))
                {
                    _connectionCollection.RemoveAt(i);
                    i--;
                }
            }
            ConnectedOnly = true;
        }
        else
        {
            IntitSchema(skipConnected: true);
            ConnectedOnly = false;
        }
    }
    private async Task RefreshTableListAsync()
    {
        var selectedItem = SchemaSource.RowSelection.SelectedItem;
        bool wasExpanded = selectedItem.IsExpanded;
        SchemaSource.Collapse(SchemaSource.RowSelection.SelectedIndex);
        SchemaEnabled = false;
        selectedItem.ClearChildren();
        _ = await Task.Run(() => DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, selectedItem.ConnectionName, forceRefresh: true));
        SchemaEnabled = true;
        selectedItem.IsExpanded = wasExpanded;
    }

    private readonly ObservableCollection<DbSchemaModel> _connectionCollection = [];


    [ObservableProperty]
    public partial bool ShowHeader { get; set; } = false;
    public ICommand ShowHideHeadersCommand { get; set; }


    public DbSchemaViewModel(Dock.Model.Core.IFactory factory, IClipboardService clipboard, IGeneralApplicationData generalApplicationData,
        ISimpleLogger simpleLogger, IMessageForUserTools messageForUserTools)
    {
        _clipboardService = clipboard;
        _generalApplicationData = generalApplicationData;
        _simpleLogger = simpleLogger;
        this.Factory = factory;
        _messageForUserTools = messageForUserTools;

        SharedInit();
        ShowHideHeadersCommand = new RelayCommand(() => ShowHeader = !ShowHeader);
        SchemaEnabled = true;

        GenerateContextMenu();

        IntitSchema();

        SchemaSource = new HierarchicalTreeDataGridSource<DbSchemaModel>(_connectionCollection)
        {
            //Columns =
            //{
            //    //new HierarchicalExpanderColumn<DbSchemaModel>(
            //        //new TemplateColumn<DbSchemaModel>(
            //        //    "Name",
            //        //    new FuncDataTemplate<DbSchemaModel>(DbItemTemplate, true),
            //        //    new GridLength(1, GridUnitType.Star)
            //        //    ),
            //            new HierarchicalExpanderColumn<DbSchemaModel>(
            //             new TextColumn<DbSchemaModel, string>("Name", x => x.Name)
            //             ,x => x.Children,
            //             hasChildrenSelector: x=>x.IsExpandedable, isExpandedSelector: x=> x.IsExpanded),
            //        //x => x.Children
            //        //,hasChildrenSelector: x=>x.IsExpandedable, x=> x.IsExpanded
            //        //),
            //    new TextColumn<DbSchemaModel, string>("Info", x => x.Info)
            //}

            Columns =
            {
                //new HierarchicalExpanderColumn<DbSchemaModel>(
                    //new TemplateColumn<DbSchemaModel>(
                    //    "Name",
                    //    new FuncDataTemplate<DbSchemaModel>(DbItemTemplate, true),
                    //    new GridLength(1, GridUnitType.Star)
                    //    ),
                    //x => x.Children
                    //,hasChildrenSelector: x=>x.IsExpandedable, x=> x.IsExpanded
                    //),

                    new HierarchicalExpanderColumn<DbSchemaModel>(
                        new TemplateColumn<DbSchemaModel>(
                            "Name",
                            new FuncDataTemplate<DbSchemaModel>(DbItemTemplate, supportsRecycling:false),
                            //new FuncDataTemplate<DbSchemaModel>(DbEditItemTemplate, supportsRecycling:false),
                            cellEditingTemplate:null,
                            new GridLength(1, GridUnitType.Star),
                            new TemplateColumnOptions<DbSchemaModel>()
                            {
                                //BeginEditGestures = BeginEditGestures.F2,
                                IsTextSearchEnabled = true,
                                TextSearchValueSelector = a=> a.Name,
                                MaxWidth = new GridLength(800, GridUnitType.Pixel),
                            }),
                        childSelector:x => x.Children,
                        hasChildrenSelector: x=>x.IsExpandedable,
                        x => x.IsExpanded),


                new TextColumn<DbSchemaModel, string>("Info", x => x.Info,GridLength.Auto,
                new()
                {
                    TextAlignment = Avalonia.Media.TextAlignment.Right,
                    MaxWidth = new GridLength(100, GridUnitType.Pixel)
                })
            }

        };
        //SchemaSource.RowCollapsed += SchemaSource_RowCollapsed;
    }

    private async Task ContextMenuActionAsync(string optionName)
    {
        if (LastItemConrtextMenuReq is null)
        {
            return;
        }

        string CONNECTION_NAME = LastItemConrtextMenuReq.ConnectionName;

        if (CONNECTION_NAME is null)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("CONNECTION_NAME is null", "Error");
        }

        if (optionName == "DISTRIBUTE_CHART_NZ")
        {
            return; //TODO 
        }

        var sql = await IDatabaseSchemaItem.GetCode(LastItemConrtextMenuReq, CONNECTION_NAME, optionName);

        if (optionName.EndsWith("CLIP"))
        {
            await _clipboardService.SetTextAsync(sql);
        }
        else
        {
            (this.Factory as DockFactory).AddNewDocument("");
            (this.Factory as DockFactory).InsertSnippetTextToActiveDocument(sql, CONNECTION_NAME);
        }
    }


    //private void SchemaSource_RowCollapsed(object? sender, RowEventArgs<HierarchicalRow<DbSchemaModel>> e)
    //{
    //    SchemaSource.Columns.SetColumnWidth(0, new GridLength(80, GridUnitType.Pixel));
    //    SchemaSource.Columns.SetColumnWidth(0, new GridLength(80, GridUnitType.Auto));
    //}

    private void IntitSchema(bool skipConnected = false)
    {
        ConnectedOnly = false;
        foreach (var item in _generalApplicationData.LoginDataDic)
        {
            if (skipConnected)
            {
                if (DatabaseServiceHelpers.IsDatabaseConnected(item.Value.ConnectionName))
                {
                    continue;
                }
            }

            DatabaseTypeEnum dbType = DatabaseServiceHelpers.StringToDatabaseTypeEnum(item.Value.Driver);

            _connectionCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.Connection, dbType)
            {
                Name = item.Key,
                Info = "connection",
                ConnectionName = item.Key
            });
        }
    }

    //reset after added/deleted new
    public void ResedConnectionList()
    {
        _connectionCollection.Clear();
        foreach (var item in _generalApplicationData.LoginDataDic)
        {
            if (ConnectedOnly)
            {
                if (DatabaseServiceHelpers.IsDatabaseConnected(item.Value.ConnectionName))
                {
                    continue;
                }
            }
            DatabaseTypeEnum dbType = DatabaseServiceHelpers.StringToDatabaseTypeEnum(item.Value.Driver);
            if (!_connectionCollection.Select(a => a.Name).Contains(item.Key))
            {
                _connectionCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.Connection, dbType)
                {
                    Name = item.Key,
                    Info = "connection",
                    ConnectionName = item.Key
                });
            }
        }
        OnPropertyChanged(nameof(_connectionCollection));
        OnPropertyChanged(nameof(SchemaSource));
    }

    private Control DbItemTemplate(DbSchemaModel node, INameScope ns)
    {
        var target = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children =
            {
                    new Image
                    {
                        [!Image.SourceProperty] =
                        //new Binding(nameof(node.TypeInDatabase))
                        new Binding(nameof(node.Self))
                        {
                            Converter = App.Current.Resources["databaseIconConverter"] as DatabaseIconConverter
                        },
                        //Source = SelectBitmap(node),
                        Margin = new Thickness(0, 0, 4, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Stretch = Avalonia.Media.Stretch.None
                    },
                    new Border()
                    {
                        Child =
                        new TextBlock
                        {
                            [!TextBlock.TextProperty] = new Binding(nameof(DbSchemaModel.Name)),
                            VerticalAlignment = VerticalAlignment.Center,
                        },
                        [!ToolTip.TipProperty] = new Binding(nameof(DbSchemaModel.ToolTipText))
                    }
                }
        };

        return target;
    }
    //private Control DbEditItemTemplate(DbSchemaModel node, INameScope ns)
    //{
    //    if (node.ActualTypeInDatabase != TypeInDatabaseEnum.ColumnComment || node.DatabaseTypeEnumValue != DatabaseTypeEnum.NetezzaSQL || node.DatabaseTypeEnumValue != DatabaseTypeEnum.NetezzaSQLOdbc)
    //    {
    //        return DbItemTemplate(node, ns);
    //    }

    //    var target = new TextBox
    //    {
    //        [!TextBox.TextProperty] = new Binding(nameof(DbSchemaModel.Name)),
    //        VerticalAlignment = VerticalAlignment.Center,
    //        HorizontalAlignment= HorizontalAlignment.Center,
    //        Padding = new Thickness(0.0),
    //        Margin = new Thickness(3,1),
    //        Height = 24,
    //        MinHeight = 24,
    //        FontSize= 12
    //    };

    //    return target;
    //}

}
