using Avalonia.Media.Imaging;
using JustyBase.Editor.CompletionProviders;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using System;
using System.Globalization;

namespace JustyBase.Converters;

public sealed class DatabaseIconConverter : IValueConverter
{
    private readonly Bitmap _tableIcon16;
    private readonly Bitmap _columnOneIcon16;
    private readonly Bitmap _externalGroupIcon16;
    private readonly Bitmap _viewIcon16;
    private readonly Bitmap _procIcon16;
    private readonly Bitmap _synonymIcon16;
    private readonly Bitmap _schemaIcon16;
    private readonly Bitmap _databaseIcon16;
    private readonly Bitmap _defaultIcon;
    private readonly Bitmap _netezzaIcon16;
    private readonly Bitmap _oracleIcon16;
    private readonly Bitmap _db2Icon16;
    private readonly Bitmap _sqliteIcon16;
    private readonly Bitmap _duckDbIcon;
    private readonly Bitmap _mySqlIcon;
    private readonly Bitmap _msSqlIcon16;
    private readonly Bitmap _postgreIcon16;
    private readonly Bitmap _tableGroupIcon16;
    private readonly Bitmap _viewGroupIcon16;
    private readonly Bitmap _functuionsGroupIcon16;
    private readonly Bitmap _synonymGroupIcon16;
    private readonly Bitmap _procGroupIcon16;
    private readonly Bitmap _seqGroupIcon16;
    private readonly Bitmap _aggGroupIcon16;
    private readonly Bitmap _columnsIcon16;
    private readonly Bitmap _fluidGroupIcon16;
    private readonly Bitmap _distributedIcon16;
    private readonly Bitmap _refIcon16;

    public DatabaseIconConverter()
    {
        if (_databaseIcon16 is null)
        {
            _databaseIcon16 = App.Current.Resources["GeneralDbBitmap"] as Bitmap;
            _netezzaIcon16 = _databaseIcon16;
            _oracleIcon16 = _databaseIcon16;
            _db2Icon16 = _databaseIcon16;
            _sqliteIcon16 = _databaseIcon16;
            _duckDbIcon = _databaseIcon16;
            _mySqlIcon = _databaseIcon16;
            _msSqlIcon16 = _databaseIcon16;
            _postgreIcon16 = _databaseIcon16;

            _tableIcon16 = App.Current.Resources["TableBitmap"] as Bitmap;
            _viewIcon16 = App.Current.Resources["ViewBitmap"] as Bitmap;
            _tableGroupIcon16 = App.Current.Resources["TableGroupBitmap"] as Bitmap;
            _viewGroupIcon16 = App.Current.Resources["ViewGroupBitmap"] as Bitmap;
            _externalGroupIcon16 = App.Current.Resources["ExternalGroupBitmap"] as Bitmap;
            _functuionsGroupIcon16 = App.Current.Resources["FunctionsGroupBitmap"] as Bitmap;
            _synonymGroupIcon16 = App.Current.Resources["SynonymGroupBitmap"] as Bitmap;
            _synonymIcon16 = App.Current.Resources["SynonymBitmap"] as Bitmap;
            _procGroupIcon16 = App.Current.Resources["ProceduresGroupBitmap"] as Bitmap;
            _procIcon16 = App.Current.Resources["ProcedureBitmap"] as Bitmap;
            _seqGroupIcon16 = App.Current.Resources["SequencesGroupBitmap"] as Bitmap;
            _aggGroupIcon16 = App.Current.Resources["AggregatesGroupBitmap"] as Bitmap;
            _columnsIcon16 = App.Current.Resources["ColumnsBitmap"] as Bitmap;
            _columnOneIcon16 = App.Current.Resources["ColumnBitmap"] as Bitmap;
            _schemaIcon16 = App.Current.Resources["SchemaBitmap"] as Bitmap;
            _fluidGroupIcon16 = App.Current.Resources["FluidGroupBitmap"] as Bitmap;
            _distributedIcon16 = App.Current.Resources["DistributedOnBitmap"] as Bitmap;
            _refIcon16 = App.Current.Resources["ReferencesBitmap"] as Bitmap;
            _defaultIcon = App.Current.Resources["FolderIconBitmap"] as Bitmap;


            // FIX THIS !!
            GlyphExtensions.TableBitmap = _tableIcon16;
            GlyphExtensions.TableBitmap = _tableIcon16;
            GlyphExtensions.ColumnBitmap = _columnOneIcon16;
            GlyphExtensions.ViewBitmap = _viewIcon16;
            GlyphExtensions.DatabaseBitmap = _databaseIcon16;
            GlyphExtensions.ProcedureBitmap = _procIcon16;
            GlyphExtensions.SynonymBitmap = _synonymIcon16;
            GlyphExtensions.SchemaBitmap = _schemaIcon16;
            GlyphExtensions.ExternalBitmap = _externalGroupIcon16;
        }
    }

    private Bitmap GetBitmapFromEnum(DatabaseTypeEnum databaseTypeEnum)
    {
        return databaseTypeEnum switch
        {
            DatabaseTypeEnum.NetezzaSQL => _netezzaIcon16,
            DatabaseTypeEnum.NetezzaSQLOdbc => _netezzaIcon16,
            DatabaseTypeEnum.DB2 => _db2Icon16,
            DatabaseTypeEnum.Sqlite => _sqliteIcon16,
            DatabaseTypeEnum.DuckDB => _duckDbIcon,
            DatabaseTypeEnum.MySql => _mySqlIcon,
            DatabaseTypeEnum.MsSqlTrusted => _msSqlIcon16,
            DatabaseTypeEnum.PostgreSql => _postgreIcon16,
            DatabaseTypeEnum.Oracle => _oracleIcon16,
            DatabaseTypeEnum.NotSupportedDatabase => _defaultIcon,
            _ => _defaultIcon
        };
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        switch (value)
        {
            case Models.Tools.DbSchemaModel node:
                {
                    switch (node.ActualTypeInDatabase)
                    {
                        case TypeInDatabaseEnum.Connection:
                            {
                                DatabaseTypeEnum connectionType = node.DatabaseTypeEnumValue;
                                return GetBitmapFromEnum(connectionType);
                            }
                        case TypeInDatabaseEnum.dbase:
                            return _databaseIcon16;
                        case TypeInDatabaseEnum.Schema:
                            return _schemaIcon16;
                        case TypeInDatabaseEnum.Table:
                            return _tableIcon16;
                        case TypeInDatabaseEnum.View:
                            return _viewIcon16;
                        case TypeInDatabaseEnum.baseTables:
                            return _tableGroupIcon16;
                        case TypeInDatabaseEnum.baseViews:
                            return _viewGroupIcon16;
                        case TypeInDatabaseEnum.baseFluides:
                        case TypeInDatabaseEnum.Fluid:
                            return _fluidGroupIcon16;
                        case TypeInDatabaseEnum.baseExternals:
                        case TypeInDatabaseEnum.ExternalTable:
                            return _externalGroupIcon16;
                        case TypeInDatabaseEnum.baseSynonyms:
                            return _synonymGroupIcon16;
                        case TypeInDatabaseEnum.Synonym:
                            return _synonymIcon16;
                        case TypeInDatabaseEnum.baseFunctions:
                            return _functuionsGroupIcon16;
                        case TypeInDatabaseEnum.Function:
                            return _functuionsGroupIcon16;
                        case TypeInDatabaseEnum.baseProcedures:
                            return _procGroupIcon16;
                        case TypeInDatabaseEnum.Procedure:
                            return _procIcon16;
                        case TypeInDatabaseEnum.baseSequence:
                            return _seqGroupIcon16;
                        case TypeInDatabaseEnum.Sequence:
                            return _seqGroupIcon16;
                        case TypeInDatabaseEnum.baseAggregates:
                            return _aggGroupIcon16;
                        case TypeInDatabaseEnum.columnInTables:
                            return _columnsIcon16;
                        case TypeInDatabaseEnum.distributionColumns:
                        case TypeInDatabaseEnum.thisDistributionCollumn:
                            return _distributedIcon16;
                        case TypeInDatabaseEnum.references:
                        case TypeInDatabaseEnum.thisReference:
                            return _refIcon16;
                        case TypeInDatabaseEnum.columnInThisTable:
                        case TypeInDatabaseEnum.columnInThisView:
                        case TypeInDatabaseEnum.columnInThisExternal:
                            return _columnOneIcon16;
                    }
                    break;
                }
            case DatabaseTypeEnum typeEnum:
                return GetBitmapFromEnum(typeEnum);
            case string stringName:
                {
                    var enumType = DatabaseServiceHelpers.StringToDatabaseTypeEnum(stringName);
                    return GetBitmapFromEnum(enumType);
                }
        }
        return _defaultIcon;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}