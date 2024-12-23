using CommunityToolkit.Mvvm.ComponentModel;
using JustyBase.Common.Contracts;
using JustyBase.Common.Models;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Models;
using JustyBase.PluginDatabaseBase.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Tools;

public sealed partial class SchemaSearchViewModel
{
    private IDatabaseService _service;
    public IDatabaseService Service => _service;
    private readonly IGeneralApplicationData _generalApplicationData;

    public bool RefreshStartup
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.RefreshOnStartupInSchemaSearch = RefreshStartup;
        }
    }

    public bool SearchInSource
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.SearchInSource = SearchInSource;
            AfterOptionsChange();
        }
    }
    private ObservableCollection<ConnectionItem> GetConnections()
    {
        _connectionList = [];
        foreach (var (item, value) in _generalApplicationData.LoginDataDic)
        {
            var type = DatabaseServiceHelpers.StringToDatabaseTypeEnum(value.Driver);

            var conItem = new ConnectionItem(item, type)
            {
                DefaultDatabase = value.Database,
                DatabaseList = []
            };
            if (!string.IsNullOrWhiteSpace(value.Database))
            {
                conItem.DefaultDatabase = value.Database;
                conItem.DatabaseList.Add(value.Database);
            }
            _connectionList.Add(conItem);
        }
        return _connectionList;
    }
    private ObservableCollection<ConnectionItem> _connectionList;
    public ObservableCollection<ConnectionItem> ConnectionList => _connectionList ??= GetConnections();

    public string ConnectionName
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (ActualConnectionItem is null)
            {
                foreach (var item in ConnectionList)
                {
                    if (item.Name == ConnectionName)
                    {
                        ActualConnectionItem = item;
                        break;
                    }
                }
            }
            _generalApplicationData.Config.ConnectionNameInSchemaSearch = ConnectionName;
        }
    }

    public ConnectionItem ActualConnectionItem
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (ConnectionName != ActualConnectionItem.Name)
            {
                ConnectionName = ActualConnectionItem.Name;
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchText))]
    public partial string TypeFilterString { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchText))]
    public partial string NameFilterString { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchText))]
    public partial string DbFilterString { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchText))]
    public partial string DescFilterString { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchText))]
    public partial string SchemaFilterString { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchText))]
    public partial string OwnerFilterString { get; set; }

    private async Task SearchLoop(IEnumerable<string> databases)
    {
        foreach (var database in databases)
        {
            var schemas = _service.GetSchemas(database, "");
            foreach (var schema in schemas)
            {
                var obejctType = new TypeInDatabaseEnum[]
                {
                    TypeInDatabaseEnum.Table,
                    TypeInDatabaseEnum.View,
                    TypeInDatabaseEnum.Procedure,
                    TypeInDatabaseEnum.ExternalTable,
                    TypeInDatabaseEnum.Synonym,
                    TypeInDatabaseEnum.Function,
                    TypeInDatabaseEnum.Fluid
                };

                for (int i = 0; i < obejctType.Length; i++)
                {
                    var tpe = obejctType[i];
                    var objects = _service.GetDbObjects(database, schema, "", tpe);
                    foreach (DatabaseObject item in objects)
                    {
                        if (tpe == TypeInDatabaseEnum.Procedure)
                        {
                            var ll = await _service.GetProceduresSignaturesFromName(database, schema, item.Name);
                            foreach (var item2 in ll)
                            {
                                SchemaSearchItemCollections.Add(new SchemaSearchItem()
                                {
                                    Id = item.Id,
                                    Type = tpe.ToStringEx(),
                                    Name = string.IsNullOrEmpty(item2.ProcedureSignature) ? item.Name : item2.ProcedureSignature,
                                    Db = database,
                                    Desc = item.Desc,
                                    Schema = schema,
                                    Owner = item.Owner,
                                    CreationDateTime = item.CreateDateTime
                                });
                            }
                        }
                        else
                        {
                            SchemaSearchItemCollections.Add(new SchemaSearchItem()
                            {
                                Id = item.Id,
                                Type = tpe.ToStringEx(),
                                Name = item.Name,
                                Db = database,
                                Desc = item.Desc,
                                Schema = schema,
                                Owner = item.Owner,
                                CreationDateTime = item.CreateDateTime
                            });
                        }
                    }
                }

                var columnItems = _service.GetColumnsFromAllTablesAndSchemas(database, schema);
                foreach (var (column, databaseObject) in columnItems)
                {
                    SchemaSearchItemCollections.Add(new SchemaSearchItem()
                    {
                        Id = -1,
                        Type = "Column",
                        Name = column.Name,
                        Db = database,
                        Desc = column.Desc,
                        Schema = schema,
                        Owner = databaseObject.Owner,
                        ParentType = databaseObject.TypeInDatabase.ToStringEx(),
                        ParentName = databaseObject.Name,
                        MoreInfo = $"column from {databaseObject.Name}({databaseObject.TypeInDatabase.ToStringEx()})",
                        CreationDateTime = databaseObject.CreateDateTime
                    });
                }
            }
        }
    }

    private void RefreshRegex()
    {
        if (SearchText is null)
        {
            return;
        }
        try
        {
            if (WholeWord)
            {
                string txt = Regex.Escape(SearchText);
                if (CaseSensitive)
                {
                    RxWholeWorld = new Regex(@$"(\b|_){txt}(\b|_)", RegexOptions.Compiled);
                }
                else
                {
                    RxWholeWorld = new Regex(@$"(\b|_){txt}(\b|_)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
            }
            else if (RegexMode)
            {
                if (CaseSensitive)
                {
                    RxWholeWorld = new Regex(SearchText, RegexOptions.Compiled);
                }
                else
                {
                    RxWholeWorld = new Regex(SearchText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
            }
        }
        catch (System.Text.RegularExpressions.RegexParseException)
        {

        }
    }

    [ObservableProperty]
    public partial bool GridEnabled { get; set; }

    private StringComparison _currentStringComparation = StringComparison.OrdinalIgnoreCase;

    public Regex RxWholeWorld { get; private set; }

    partial void AfterOptionsChange();

    public bool CaseSensitive
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _currentStringComparation = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            _generalApplicationData.Config.CaseSensitive = CaseSensitive;
            RefreshRegex();
            AfterOptionsChange();
        }
    }

    public bool WholeWord
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.WholeWords = WholeWord;
            RefreshRegex();
            AfterOptionsChange();
        }
    }

    public bool RegexMode
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.RegexMode = RegexMode;
            RefreshRegex();
            AfterOptionsChange();
        }
    }
}

