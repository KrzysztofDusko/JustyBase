using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Enums;

namespace JustyBase.PluginDatabaseBase.Database;

public static class DatabaseServiceHelpers
{
    private static readonly string[] _typeInDatabaseToNameInSchema;
    private static readonly Dictionary<string, TypeInDatabaseEnum> _schemaNameToTypeInDatabaseEnum;
    static DatabaseServiceHelpers()
    {
        var enumValues = Enum.GetValues<TypeInDatabaseEnum>();
        int enaumElements = enumValues.Count();
        _typeInDatabaseToNameInSchema = new string[enaumElements];
        foreach (TypeInDatabaseEnum item in Enum.GetValues<TypeInDatabaseEnum>())
        {
            _typeInDatabaseToNameInSchema[(int)item] = item.ToString();
        }
        _typeInDatabaseToNameInSchema[(int)TypeInDatabaseEnum.Table] = "Table";
        _typeInDatabaseToNameInSchema[(int)TypeInDatabaseEnum.View] = "View";
        _typeInDatabaseToNameInSchema[(int)TypeInDatabaseEnum.Procedure] = "Procedure";
        _typeInDatabaseToNameInSchema[(int)TypeInDatabaseEnum.ExternalTable] = "External table";
        _typeInDatabaseToNameInSchema[(int)TypeInDatabaseEnum.Synonym] = "Synonym";
        _typeInDatabaseToNameInSchema[(int)TypeInDatabaseEnum.Function] = "Function";
        _typeInDatabaseToNameInSchema[(int)TypeInDatabaseEnum.Fluid] = "Fluid";
        _schemaNameToTypeInDatabaseEnum = new Dictionary<string, TypeInDatabaseEnum>(enaumElements);
        for (int i = 0; i < enaumElements; i++)
        {
            _schemaNameToTypeInDatabaseEnum[_typeInDatabaseToNameInSchema[i]] = (TypeInDatabaseEnum)i;
        }
    }

    public static string ToStringEx(this TypeInDatabaseEnum typeInDatabase)
    {
        return _typeInDatabaseToNameInSchema[(int)typeInDatabase];
    }
    public static TypeInDatabaseEnum FromStringEx(string name)
    {
        if (_schemaNameToTypeInDatabaseEnum.TryGetValue(name, out var res))
        {
            return res;
        }
        return TypeInDatabaseEnum.otherNoneEntry;
    }

    /// <summary>
    /// name from Sql result to TypeInDatabaseEnum
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static TypeInDatabaseEnum GetTypeInDatabaseEnumFromDbName(this string typeName)
    {
        return typeName switch
        {
            "TABLE" => TypeInDatabaseEnum.Table,
            "BASE TABLE" => TypeInDatabaseEnum.Table,
            "TYPED TABLE" => TypeInDatabaseEnum.Table,
            "HIERARCHY TABLE" => TypeInDatabaseEnum.Table,
            "DETACHED TABLE" => TypeInDatabaseEnum.Table,
            "MATERIALIZED QUERY TABLE" => TypeInDatabaseEnum.Table,
            "ALIAS" => TypeInDatabaseEnum.db2alias,
            "VIEW" => TypeInDatabaseEnum.View,
            "TYPED VIEW" => TypeInDatabaseEnum.View,
            "PROCEDURE" => TypeInDatabaseEnum.Procedure,
            "FUNCTION" => TypeInDatabaseEnum.Function,
            "SEQUENCE" => TypeInDatabaseEnum.Sequence,
            "IDENTITY SEQUENCE" => TypeInDatabaseEnum.Sequence,
            "SYNONYM" => TypeInDatabaseEnum.Synonym,
            "NICKNAME" => TypeInDatabaseEnum.Synonym,
            "EXTERNAL TABLE" => TypeInDatabaseEnum.ExternalTable,
            "AGGREGATE" => TypeInDatabaseEnum.thisAggregate,
            "FLUID" => TypeInDatabaseEnum.Fluid,
            _ => TypeInDatabaseEnum.otherNoneGroup
        };
    }


    private static readonly Dictionary<string, DatabaseTypeEnum> _textToDatabaseTypeEnumDict = new()
    {
        {"NetezzaSQL", DatabaseTypeEnum.NetezzaSQL},
        {"NetezzaSQLOdbc", DatabaseTypeEnum.NetezzaSQLOdbc},
        {"DB2", DatabaseTypeEnum.DB2},
        {"MsSqlTrusted", DatabaseTypeEnum.MsSqlTrusted},
        {"Postgres", DatabaseTypeEnum.PostgreSql},
        {"Oracle", DatabaseTypeEnum.Oracle},
        {"SQLite", DatabaseTypeEnum.Sqlite},
        {"DuckDB", DatabaseTypeEnum.DuckDB},
        {"MySQL", DatabaseTypeEnum.MySql},
    };
    public static List<string> GetSupportedDriversNames()
    {
        return _textToDatabaseTypeEnumDict.Keys.ToList();
    }
    public static DatabaseTypeEnum StringToDatabaseTypeEnum(string driver)
    {
        if (driver is not null && _textToDatabaseTypeEnumDict.TryGetValue(driver, out var tpe))
        {
            return tpe;
        }
        else
        {
            return DatabaseTypeEnum.NotSupportedDatabase;
        }
    }

    // connectionName -> database service
    private static readonly Dictionary<string, IDatabaseService> _cachedDbServices = [];
    public static void RemoveCachedConnection(string connectionName)
    {
        if (connectionName is not null && _cachedDbServices.TryGetValue(connectionName, out var res))
        {
            res.ClearCachedData();
            _cachedDbServices.Remove(connectionName);
        }
    }

    private readonly static Dictionary<DatabaseTypeEnum, Func<
    string,//username
    string,//password
    string,//port
    string,//ip
    string,//db
    int, // connectionTimeout
    IDatabaseService // result -> Oracle/Db2/etc.
    >> SpecificDbImpelmetations = new Dictionary<DatabaseTypeEnum, Func<string, string, string, string, string, int, IDatabaseService>>();

    private static readonly Lock _lockAddDatabaseImplementation = new Lock();
    public static void AddDatabaseImplementation(DatabaseTypeEnum databaseTypeEnum, Func<
        string,//username
        string,//password
        string,//port
        string,//ip
        string,//db
        int, // connectionTimeout
        IDatabaseService // result -> Oracle/Db2/etc.
        > ctorOfDbService)
    {
        lock (_lockAddDatabaseImplementation)
        {
            SpecificDbImpelmetations[databaseTypeEnum] = ctorOfDbService;
        }
    }

    /// <summary>
    /// connectionName = name of connection
    /// forceRefresh = delete and load again
    /// delayCache = true -> 99% of operation wil be done in separated thread Task.Run not awaited
    /// </summary>
    /// <param name="connectionName"></param>
    /// <param name="forceRefresh"></param>
    /// <param name="delayCache"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static IDatabaseService? GetDatabaseService(IDatabaseInfo databaseInfo, string connectionName, bool forceRefresh = false, bool delayCache = false, int connectionTimeout = 0,
        Action<string>? messageAction = null, IDatabaseService? ownDatabaseService = null)
    {
        ArgumentNullException.ThrowIfNull(connectionName);

        if (forceRefresh)
        {
            RemoveCachedConnection(connectionName);
        }
        if (_cachedDbServices.TryGetValue(connectionName, out var cachedService1))
        {
            return cachedService1;
        }
        else
        {
            IDatabaseService? databaseService;
            if (ownDatabaseService is null && databaseInfo?.LoginDataDic is not null && databaseInfo.LoginDataDic.TryGetValue(connectionName, out var loginDataModel))
            {
                string userName = loginDataModel.UserName;
                string password = loginDataModel.Password;
                string ip = loginDataModel.Server;
                string db = loginDataModel.Database;
                string driver = loginDataModel.Driver;
                loginDataModel.ConnectionName = connectionName;

                DatabaseTypeEnum typedDriver = StringToDatabaseTypeEnum(driver);
                databaseInfo.LoadPluginsIfNeeded(null).Wait();
                databaseService = CreateDbInstanceService(typedDriver, userName, password, ip, db, connectionTimeout, databaseInfo.GetDataDir());
            }
            else 
            {
                databaseService = ownDatabaseService;
            }

            if (databaseService is null)
            {
                throw new NullReferenceException("databaseService should not be null");
            }

            if (databaseInfo?.GlobalLoggerObject is not null)
            {
                databaseService.Logger = databaseInfo.GlobalLoggerObject;
            }
            else 
            {
                databaseService.Logger = ISimpleLogger.EmptyLogger;
            }

            _cachedDbServices[connectionName] = databaseService;
            databaseService.ConnectedLevel = DatabaseConnectedLevel.Connected;
            databaseService.Name = connectionName;
            if (!delayCache)
            {
                try
                {
                    databaseService.CacheMainDictionary();
                }
                catch (Exception ex)
                {
                    databaseService.Logger.TrackError(ex, isCrash: false);
                    _cachedDbServices.Remove(connectionName);
                    messageAction?.Invoke($"ERROR {ex.Message}");
                    return null;
                }
            }
            else
            {
                Task.Run(() =>
                {
                    try
                    {
                        databaseService.CacheMainDictionary();
                    }
                    catch (Exception ex)
                    {
                        databaseService.Logger.TrackError(ex, isCrash: false);
                        _cachedDbServices.Remove(connectionName);
                        messageAction?.Invoke($"ERROR {ex.Message}");
                        return;
                    }
                });
            }
        }

        return _cachedDbServices[connectionName];
    }

    private static IDatabaseService CreateDbInstanceService(DatabaseTypeEnum typedDriver, string userName, string password, string ip, string db, int connectionTimeout,
    string tempDirectory)
    {
        if (SpecificDbImpelmetations.TryGetValue(typedDriver, out var creator) && creator is not null)
        {
            IDatabaseService databaseService = creator.Invoke(userName, password, "", ip, db, connectionTimeout);
            databaseService.TempDataDirectory = tempDirectory;
            return databaseService;
        }
        else
        {
            throw new NotSupportedException("database is not supported");
        }
    }

    public static bool IsDatabaseConnected(string connectionName)
    {
        return GetDatabaseConnectedLevel(connectionName) >= DatabaseConnectedLevel.Connected;
    }
    public static DatabaseConnectedLevel GetDatabaseConnectedLevel(string connectionName)
    {
        if (!_cachedDbServices.TryGetValue(connectionName, out IDatabaseService? value))
        {
            return DatabaseConnectedLevel.NotConnected;
        }
        return value.ConnectedLevel;
    }

}