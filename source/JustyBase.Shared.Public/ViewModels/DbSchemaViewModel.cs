using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using System.Collections.ObjectModel;

namespace JustyBase.ViewModels.Tools;
public sealed partial class DbSchemaViewModel
{
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly ISimpleLogger _simpleLogger;
    private readonly IClipboardService _clipboardService;
    private readonly IMessageForUserTools _messageForUserTools;

    public ICommand ContextMenuActionCommand { get; set; }
    public ICommand RefreshTableListCommand { get; set; }
    public ICommand ShowConnectedOnlyCommand { get; set; }

    [ObservableProperty]
    public partial bool SchemaEnabled { get; set; }

    public bool ConnectedOnly
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (!ConnectedOnly)
            {
                _connectedMenuItem.Header = "Show connected only";
            }
            else
            {
                _connectedMenuItem.Header = "Show all connections";
            }
        }
    }
    private MenuItem _connectedMenuItem;

    [ObservableProperty]
    public partial ObservableCollection<Control> MenuItems { get; set; }
    private ObservableCollection<Control> MenuItemsForConnections { get; set; }
    private ObservableCollection<Control> MenuItemsForTableGroup { get; set; }
    private ObservableCollection<Control> MenuItemsForTable { get; set; }
    private ObservableCollection<Control> MenuItemsForView { get; set; }
    private ObservableCollection<Control> MenuItemsForViewGroups { get; set; }
    private ObservableCollection<Control> MenuItemsForProcedures { get; set; }
    private ObservableCollection<Control> MenuItemsForFluids { get; set; }
    private ObservableCollection<Control> MenuItemsForProceduresGroups { get; set; }
    private ObservableCollection<Control> MenuItemsForExternalTablesNz { get; set; }
    private ObservableCollection<Control> MenuItemsForExternalTablesNzGroups { get; set; }
    private ObservableCollection<Control> MenuItemsForSynonyms { get; set; }
    private ObservableCollection<Control> MenuItemsForSynonymsGroups { get; set; }
    private ObservableCollection<Control> MenuItemsForSequenceGroups { get; set; }
    private ObservableCollection<Control> TodoMenuItems { get; set; }


    public void PrepareContextMenu(object? data)
    {
        TypeInDatabaseEnum selRowType = GetSelectedType(data);
        switch (selRowType)
        {
            case TypeInDatabaseEnum.otherNoneEntry:
                return;
            case TypeInDatabaseEnum.Connection:
                MenuItems = MenuItemsForConnections;
                break;
            case TypeInDatabaseEnum.baseTables:
                MenuItems = MenuItemsForTableGroup;
                break;
            case TypeInDatabaseEnum.Table:
                MenuItems = MenuItemsForTable;
                break;
            case TypeInDatabaseEnum.View:
                MenuItems = MenuItemsForView;
                break;
            case TypeInDatabaseEnum.baseViews:
                MenuItems = MenuItemsForViewGroups;
                break;
            case TypeInDatabaseEnum.Procedure:
                MenuItems = MenuItemsForProcedures;
                break;
            case TypeInDatabaseEnum.Fluid:
                MenuItems = MenuItemsForFluids;
                break;
            case TypeInDatabaseEnum.baseProcedures:
                MenuItems = MenuItemsForProceduresGroups;
                break;
            case TypeInDatabaseEnum.ExternalTable:
                MenuItems = MenuItemsForExternalTablesNz;
                break;
            case TypeInDatabaseEnum.baseExternals:
                MenuItems = MenuItemsForExternalTablesNzGroups;
                break;
            case TypeInDatabaseEnum.Synonym:
                MenuItems = MenuItemsForSynonyms;
                break;
            case TypeInDatabaseEnum.baseSynonyms:
                MenuItems = MenuItemsForSynonymsGroups;
                break;
            case TypeInDatabaseEnum.baseSequence:
                MenuItems = MenuItemsForSequenceGroups;
                break;
            default:
                MenuItems = TodoMenuItems;
                break;
        }
    }

    private void GenerateContextMenu()
    {
        MenuItemsForTable =
        [
            new MenuItem()
            {
                Header = "User Scripts [TO DO]",
                ItemsSource = new Control[]
                {
                    new MenuItem(){Header = "Manage..." },
                    GetMenuSeparator(),
                    new MenuItem(){Header = "Script1" },
                },
            },
            new MenuItem()
            {
                Header = "Others",
                ItemsSource = new MenuItem[]
                {
                    new(){Header = "Groom table", Command = ContextMenuActionCommand, CommandParameter = "GROOM"  },
                    new(){Header = "Generate statistics", Command = ContextMenuActionCommand, CommandParameter = "STATS"  },
                    new(){Header = "Add comment", Command = ContextMenuActionCommand, CommandParameter = "COMMENT" },
                    new(){Header = "Drop Table", Command = ContextMenuActionCommand, CommandParameter = "DROP" },
                    new(){Header = "Empty table", Command = ContextMenuActionCommand, CommandParameter = "EMPTY"},
                }
            },
            GetMenuSeparator(),
            new MenuItem() { Header = "Create code (ddl) to new query window", Command = ContextMenuActionCommand, CommandParameter = "DDL_TABLE" },
            new MenuItem() { Header = "Create code (ddl)  to clipboard", Command = ContextMenuActionCommand, CommandParameter = "DDL_TABLE_CLIP" },
            new MenuItem() { Header = "Recreate to new query window", Command = ContextMenuActionCommand, CommandParameter = "RECREATE_TABLE" },
            new MenuItem() { Header = "Recreate to clipboard", Command = ContextMenuActionCommand, CommandParameter = "RECREATE_TABLE_CLIP" },
            GetMenuSeparator(),
            new MenuItem() { Header = "Select Top 100 to new query window", Command = ContextMenuActionCommand, CommandParameter = "SELECT" },
            new MenuItem() { Header = "Select Top 100 to clipboard", Command = ContextMenuActionCommand, CommandParameter = "SELECT_CLIP" },
            new MenuItem() { Header = "Select Top 100 to with text search ", Command = ContextMenuActionCommand, CommandParameter = "SELECT_SEARCH" },
            new MenuItem() { Header = "Select deleted rows to new query window", Command = ContextMenuActionCommand, CommandParameter = "DELETED" },
            new MenuItem() { Header = "Select duplicates to clipboard", Command = ContextMenuActionCommand, CommandParameter = "DUPLICATES_CLIP" },
            GetMenuSeparator(),
            new MenuItem() { Header = "Grant to clipboard", Command = ContextMenuActionCommand, CommandParameter = "GRANT_CLIP" },
            new MenuItem() { Header = "Organize to clipboard", Command = ContextMenuActionCommand, CommandParameter = "ORGANIZE_CLIP" },
            new MenuItem() { Header = "Get distribution code to clipboard", Command = ContextMenuActionCommand, CommandParameter = "DISTRIBUTE_CLIP" },
            new MenuItem() { Header = "Show distribution chart", Command = ContextMenuActionCommand, CommandParameter = "DISTRIBUTE_CHART_NZ" },
            GetMenuSeparator(),
            new MenuItem() { Header = "Add key code to clipboard", Command = ContextMenuActionCommand, CommandParameter = "KEY_CLIP" },
            new MenuItem() { Header = "Add unique constraint code to clipboard", Command = ContextMenuActionCommand, CommandParameter = "UNIQUE_CLIP" },
            GetMenuSeparator(),
            new MenuItem() { Header = "Import Data [TODO]", Command = ContextMenuActionCommand, CommandParameter = "IMPORT_DATA" },
            new MenuItem() { Header = "Export Data", Command = ContextMenuActionCommand, CommandParameter = "EXPORT_DATA" },
        ];

        MenuItemsForProceduresGroups =
        [
            new MenuItem() { Header = "Create to new query window", Command = ContextMenuActionCommand, CommandParameter = "CREATE_PROCEDURE" },
            new MenuItem() { Header = "Create all code (ddl) to new query window", Command = ContextMenuActionCommand, CommandParameter = "DDL_ALL_PROCEDURES" },
        ];

        MenuItemsForProcedures =
        [
            new MenuItem() { Header = "Create code (ddl) to new query window", Command = ContextMenuActionCommand, CommandParameter = "DDL_PROCEDURE" },
            new MenuItem() { Header = "Call/Execute to new query window", Command = ContextMenuActionCommand, CommandParameter = "CALL_PROCEDURE" },
        ];

        MenuItemsForFluids =
        [
            new MenuItem() { Header = "Show usage sample to new query window", Command = ContextMenuActionCommand, CommandParameter = "FLUID_SAMPLE" },
        ];

        MenuItemsForView =
        [
            new MenuItem() { Header = "Create code (ddl) to new query window", Command = ContextMenuActionCommand, CommandParameter = "DDL_VIEW" },
            new MenuItem() { Header = "Select Top 100 to new query window", Command = ContextMenuActionCommand, CommandParameter = "SELECT_VIEW" },
        ];

        MenuItemsForViewGroups =
        [
            new MenuItem() { Header = "Create all code (ddl) views", Command = ContextMenuActionCommand, CommandParameter = "DDL_ALL_VIEWS" },
        ];

        MenuItemsForSequenceGroups =
        [
            new MenuItem() { Header = "Create new to query window", Command = ContextMenuActionCommand, CommandParameter = "CREATE_SEQUENCE" },
        ];

        MenuItemsForSynonymsGroups =
        [
            new MenuItem() { Header = "Create to new query window", Command = ContextMenuActionCommand, CommandParameter = "CREATE_SYNONYM" },
            new MenuItem() { Header = "Create all code (ddl) to new query window", Command = ContextMenuActionCommand, CommandParameter = "DDL_ALL_SYNONYMS" },
        ];

        MenuItemsForSynonyms =
        [
            new MenuItem() { Header = "Create code (ddl) to new query window", Command = ContextMenuActionCommand, CommandParameter = "DDL_SYNONYM" },
        ];

        TodoMenuItems =
        [
            new MenuItem() { Header = "Copy text",Command = ContextMenuActionCommand, CommandParameter = "COPY_TEXT_CLIP" },
        ];

        MenuItemsForConnections = [];
        _connectedMenuItem = new MenuItem() { Header = "Show connected only", Command = ShowConnectedOnlyCommand };
#if AVALONIA
        MenuItemsForConnections.Add(new MenuItem() { Header = "Show/hide header", Command = ShowHideHeadersCommand });
#endif
        MenuItemsForConnections.Add(_connectedMenuItem);

        MenuItemsForTableGroup =
        [
            new MenuItem() { Header = "Create all code (ddl) tables", Command = ContextMenuActionCommand, CommandParameter = "DDL_ALL_TABLES" },
            new MenuItem() { Header = "Recreate all tables", Command = ContextMenuActionCommand, CommandParameter = "RECREATE_ALL_TABLES" },
            new MenuItem() { Header = "Search text in every table", Command = ContextMenuActionCommand, CommandParameter = "SELECT_ALL_SEARCH_TEXT" },
            new MenuItem() { Header = "Search number in every table", Command = ContextMenuActionCommand, CommandParameter = "SELECT_ALL_SEARCH_NUMBER" },
        ];

        MenuItemsForConnections.Add(new MenuItem() { Header = "Refresh table list", Command = RefreshTableListCommand });

        MenuItemsForExternalTablesNz =
        [
            new MenuItem() { Header = "Create code (ddl)", Command = ContextMenuActionCommand, CommandParameter = "DDL_EXTERNAL" },
        ];

        MenuItemsForExternalTablesNzGroups =
        [
            new MenuItem() { Header = "Create code (ddl)", Command = ContextMenuActionCommand, CommandParameter = "DDL_ALL_EXTERNALS" },
        ];


        MenuItems = TodoMenuItems;
    }

    public void SharedInit()
    {
        ContextMenuActionCommand = new AsyncRelayCommand<string>(ContextMenuActionAsync);
        RefreshTableListCommand = new AsyncRelayCommand(RefreshTableListAsync);
        ShowConnectedOnlyCommand = new RelayCommand(ShowConnectedOnly);
    }
}