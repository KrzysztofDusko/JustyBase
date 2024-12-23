using CommunityToolkit.Mvvm.ComponentModel;
using JustyBase.Common.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Models;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.Services.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace JustyBase.Models.Tools;

public sealed partial class DbSchemaModel : ObservableObject, IDatabaseSchemaItem
{
    public string Name { get; set; }

    private static readonly IGeneralApplicationData _generalApplicationData;
    static DbSchemaModel()
    {
        _generalApplicationData = App.GetRequiredService<IGeneralApplicationData>();
    }


    [ObservableProperty]
    public partial TypeInDatabaseEnum ActualTypeInDatabase { get; set; }
    public DatabaseTypeEnum DatabaseTypeEnumValue { get; set; }
    public DbSchemaModel? Parent { get; set; }
    public required string ConnectionName { get; set; }
    public string Database { get; set; }
    public string CurrentSchema { get; set; }
    public string Owner { get; set; }
    public string Comment { get; set; }
    public string ToolTipText => $"Comment: {Comment ?? "no desc"}";
    public ObservableCollection<DbSchemaModel> LoadChildren(ObservableCollection<DbSchemaModel>? newNodeCollection = null)
    {
        DatabaseTypeEnum dbType = DatabaseTypeEnumValue;//GetDatabaseType();
        return LoadChildren(dbType, newNodeCollection);
    }

    private ObservableCollection<DbSchemaModel> LoadChildren(DatabaseTypeEnum databaseTypeEnum, ObservableCollection<DbSchemaModel>? newNodeCollection)
    {
        newNodeCollection ??= new ObservableCollection<DbSchemaModel>();
        switch (ActualTypeInDatabase)
        {
            case TypeInDatabaseEnum.Connection:
                var service = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, Name);
                var databases = service.GetDatabases("");

                foreach (var item in databases)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.dbase, this.DatabaseTypeEnumValue) { Parent = this, Name = item, Info = "database", ConnectionName = Name });
                }
                break;
            case TypeInDatabaseEnum.dbase:
                var schemas = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetSchemas(Name, "");
                foreach (var item in schemas)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.Schema, this.DatabaseTypeEnumValue) { Parent = this, Name = item, Info = "schema", ConnectionName = ConnectionName, Database = Name });
                }
                break;
            case TypeInDatabaseEnum.Schema:
                var currSchema = Name;
                var itemsCollection = new List<(string name, string info, TypeInDatabaseEnum typeInDatabase)>
                {
                    ("Tables","tables", TypeInDatabaseEnum.baseTables),//0
                    ("External tables","external tables", TypeInDatabaseEnum.baseExternals),//1
                    ("Views","views", TypeInDatabaseEnum.baseViews),//2
                    ("Procedures","procedures", TypeInDatabaseEnum.baseProcedures),//3
                    ("Sequences","sequences", TypeInDatabaseEnum.baseSequence),//4
                    ("Functions","functions", TypeInDatabaseEnum.baseFunctions),//5
                    ("Synonyms","synonyms", TypeInDatabaseEnum.baseSynonyms),//6
                    ("Aggregate","aggregate",TypeInDatabaseEnum.baseAggregates),//7
                    ("Fluid Query Data Sources","fluids",TypeInDatabaseEnum.baseFluides),//8
                    ("Others","others", TypeInDatabaseEnum.otherNoneGroup)//9
                };
                if (databaseTypeEnum != DatabaseTypeEnum.NetezzaSQL && databaseTypeEnum != DatabaseTypeEnum.NetezzaSQLOdbc)
                {
                    itemsCollection.RemoveAt(8); //fluids
                    itemsCollection.RemoveAt(1); //external
                }

                foreach (var item in itemsCollection)
                {
                    newNodeCollection.Add(new DbSchemaModel(item.typeInDatabase, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.name,
                        Info = item.info,
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = currSchema
                    });
                }
                break;
            case TypeInDatabaseEnum.baseTables:
                var type = TypeInDatabaseEnum.Table;
                var tables = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", type).OrderBy(o => o.Owner.PadRight(20) + o.Name);

                foreach (var item in tables)
                {
                    newNodeCollection.Add(new DbSchemaModel(type, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s table",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Owner = item.Owner,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.baseExternals:
                var typeEx = TypeInDatabaseEnum.ExternalTable;
                var externals = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", typeEx).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in externals)
                {
                    newNodeCollection.Add(new DbSchemaModel(typeEx, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s external table",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.baseViews:
                var views = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", TypeInDatabaseEnum.View).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in views)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.View, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s view",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.baseProcedures:
                var procedures = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", TypeInDatabaseEnum.Procedure).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in procedures)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.Procedure, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s procedure",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.baseSequence:
                var sequences = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", TypeInDatabaseEnum.Sequence).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in sequences)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.Sequence, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s sequence",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.baseFunctions:
                var functions = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", TypeInDatabaseEnum.Function).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in functions)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.Function, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s function",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.baseSynonyms:
                var synonyms = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", TypeInDatabaseEnum.Synonym).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in synonyms)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.Synonym, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s synonym",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.baseFluides:
                var fluides = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", TypeInDatabaseEnum.Fluid).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in fluides)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.Fluid, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s fluid",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.otherNoneGroup:
                var others = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", TypeInDatabaseEnum.otherNoneGroup).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in others)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.otherNoneEntry, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s {item.TextType}",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema,
                        Comment = item.Desc
                    });
                }
                break;
            case TypeInDatabaseEnum.baseAggregates:
                var aggregate = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetDbObjects(Database, CurrentSchema, "", TypeInDatabaseEnum.thisAggregate).OrderBy(o => o.Owner.PadRight(20) + o.Name);
                foreach (var item in aggregate)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.thisAggregate, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = $"{item.Owner}'s aggregate",
                        ConnectionName = ConnectionName,
                        Database = Database,
                        CurrentSchema = CurrentSchema
                    });
                }
                break;
            case TypeInDatabaseEnum.Table:
                newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.columnInTables, this.DatabaseTypeEnumValue)
                {
                    Parent = this,
                    Name = "Columns",
                    Info = "columns",
                    ConnectionName = ConnectionName,
                    Database = Database,
                    CurrentSchema = CurrentSchema
                });
                newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.distributionColumns, this.DatabaseTypeEnumValue)
                {
                    Parent = this,
                    Name = "Distributed On",
                    Info = "distribution",
                    ConnectionName = ConnectionName,
                    Database = Database,
                    CurrentSchema = CurrentSchema
                });
                newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.organizeColumns, this.DatabaseTypeEnumValue)
                {
                    Parent = this,
                    Name = "Organized On",
                    Info = "organization",
                    ConnectionName = ConnectionName,
                    Database = Database,
                    CurrentSchema = CurrentSchema
                });
                newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.references, this.DatabaseTypeEnumValue)
                {
                    Parent = this,
                    Name = "References",
                    Info = "references",
                    ConnectionName = ConnectionName,
                    Database = Database,
                    CurrentSchema = CurrentSchema
                });
                newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.DbItemMoreInfo, this.DatabaseTypeEnumValue)
                {
                    Parent = this,
                    Name = $"Owner : {this.Owner ?? "empty owner"}",
                    Info = "more info",
                    ConnectionName = ConnectionName,
                    Database = Database,
                    CurrentSchema = CurrentSchema
                });
                break;
            case TypeInDatabaseEnum.columnInTables:
                var columns = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetColumns(Database, CurrentSchema, Parent?.Name, "");
                foreach (var item in columns)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.columnInThisTable, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = "column"
                    ,
                        Comment = item.Desc,
                        ConnectionName = this.ConnectionName
                    });
                }
                break;
            case TypeInDatabaseEnum.View:
                var columns2 = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetColumns(Database, CurrentSchema, Name, "");
                foreach (var item in columns2)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.columnInThisView, this.DatabaseTypeEnumValue)
                    {
                        Parent = this,
                        Name = item.Name,
                        Info = "column",
                        ConnectionName = this.ConnectionName
                    });
                }
                break;
            case TypeInDatabaseEnum.ExternalTable:
                var columns3 = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName).GetColumns(Database, CurrentSchema, Name, "");
                foreach (var item in columns3)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.columnInThisExternal, this.DatabaseTypeEnumValue)
                    { Parent = this, Name = item.Name, Info = "column", ConnectionName = this.ConnectionName });
                }
                break;
            case TypeInDatabaseEnum.columnInThisTable:
            case TypeInDatabaseEnum.columnInThisView:
            case TypeInDatabaseEnum.columnInThisExternal:
                string name = Parent?.Name;
                if (ActualTypeInDatabase == TypeInDatabaseEnum.columnInThisTable)
                {
                    name = Parent?.Parent?.Name;
                }
                IEnumerable<DatabaseColumn> columnsX = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, Parent?.ConnectionName).GetColumns(Parent?.Database, Parent?.CurrentSchema, name, "");
                DatabaseColumn colTemp = null;
                foreach (var item in columnsX)
                {
                    if (item.Name == Name)
                    {
                        colTemp = item;
                        break;
                    }
                }
                if (colTemp is not null)
                {
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.ColumnDataType, this.DatabaseTypeEnumValue)
                    { Name = colTemp.FullTypeName, Info = "data type", ConnectionName = this.ConnectionName });
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.ColumnDataTypeNullInfo, this.DatabaseTypeEnumValue)
                    { Name = colTemp.ColumnNotNull.ToString(), Info = "not null", ConnectionName = this.ConnectionName });

                    var colDesc = String.IsNullOrWhiteSpace(colTemp.Desc) ? "No description" : colTemp.Desc;
                    newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.ColumnComment, this.DatabaseTypeEnumValue)
                    { Name = colDesc, Info = "comment", ConnectionName = this.ConnectionName });
                }
                break;
            case TypeInDatabaseEnum.distributionColumns:
                var nzService = (DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName) as INetezza);
                if (nzService is not null)
                {
                    if (nzService.DistributionDictionary.TryGetValue(Database, out var dc0) &&
                        dc0.TryGetValue(CurrentSchema, out var dic1) && Parent?.Name != null && dic1.TryGetValue(Parent.Name, out var distList))
                    {
                        foreach (var item in distList)
                        {
                            newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.thisDistributionCollumn, this.DatabaseTypeEnumValue)
                            { Parent = this, Name = item, Info = "", ConnectionName = this.ConnectionName });
                        }
                    }
                }
                break;
            case TypeInDatabaseEnum.organizeColumns:
                var nzService1 = (DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName) as INetezza);
                if (nzService1 is not null)
                {
                    if (nzService1.OrganizeDictionary.TryGetValue(Database, out var dc0) &&
                        dc0.TryGetValue(CurrentSchema, out var dic1) && Parent is not null && dic1.TryGetValue(Parent.Name, out var organizeList))
                    {
                        foreach (var item in organizeList)
                        {
                            newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.thisOrganizeCollumn, this.DatabaseTypeEnumValue)
                            { Parent = this, Name = item, Info = "", ConnectionName = this.ConnectionName });
                        }
                    }
                }
                break;
            case TypeInDatabaseEnum.references:
                var nzService2 = (DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, ConnectionName) as INetezza);
                if (nzService2 is not null)
                {
                    if (nzService2.KeysDictionary.TryGetValue(Database, out var dict1) && dict1.TryGetValue(CurrentSchema, out var dict2)
                && Parent is not null && Parent.Name is not null && dict2.TryGetValue(Parent.Name, out var dict3)
                )
                    {
                        foreach (var (keyName, kefInfo) in dict3)
                        {
                            newNodeCollection.Add(new DbSchemaModel(TypeInDatabaseEnum.thisReference, this.DatabaseTypeEnumValue)
                            {
                                Parent = this,
                                Name = $"{DatabaseService.KeyNameFromChar(kefInfo.KeyType)}: {keyName}",
                                Info = "(" + string.Join(',', kefInfo.ColumnList.Select(o => o.colName)) + ")"
                                ,
                                ConnectionName = this.ConnectionName
                            });
                        }
                    }
                }
                break;
            default:
                break;
        }
        return newNodeCollection;
    }

    public override string ToString()
    {
        return Name;
    }

}
