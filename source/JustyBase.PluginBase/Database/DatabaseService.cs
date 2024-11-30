using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Enums;
using JustyBase.StringExtensions;
using JustyBase.PluginDatabaseBase.Models;
using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace JustyBase.PluginDatabaseBase.Database;

public abstract class DatabaseService : IDatabaseService, IDatabaseWithSpecificImportService
{
    public string Name { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Port { get; set; }
    public string Ip { get; set; }
    public string Database { get; set; }
    public string TempDataDirectory { get; set; }
    public ISimpleLogger Logger { get; set; }
    public CurrentAutoCompletDatabaseMode AutoCompletDatabaseMode { get; init; }
    public DatabaseTypeEnum DatabaseType { get; init; } = DatabaseTypeEnum.NotSupportedDatabase;

    private DbConnection? _connection;
    public DbConnection Connection
    {
        get
        {
            _connection ??= GetConnection(null, pooling: false, forSchema: true);
            return _connection;
        }
        protected set
        {
            if (_connection is not null)
            {
                Connection.Disposed -= Connection_Disposed;
            }
            _connection = value;
            Connection.Disposed += Connection_Disposed;
        }
    }
    private void Connection_Disposed(object? sender, EventArgs e)
    {
        Debug.Assert(false);
        _connection = null;//???
    }

    public int CONNECTION_TIMEOUT = 10;

    public int DEFAULT_COMMAND_TIMEOUT = 3_600;

    protected bool preferDatabaseInCodes = true;
    protected virtual string GetLimitClause(object rowsCnt)
    {
        return $"LIMIT {rowsCnt}";
    }

    public DatabaseConnectedLevel ConnectedLevel { get; set; } = DatabaseConnectedLevel.NotConnected;

    protected DatabaseService(string username, string password, string port, string ip, string db, int connectionTimeout)
    {
        Username = username;
        Password = password;
        Port = port;
        Ip = ip;
        Database = db;
        if (connectionTimeout > 0)
        {
            CONNECTION_TIMEOUT = connectionTimeout;
        }
    }

    public Action<string> DbMessageAction { get; set; }
    //private static readonly StringPool stringPoolForSchemaGeneral = new StringPool();
    //protected static StringPool StringPoolForSchemaGeneral => stringPoolForSchemaGeneral;
    public static string CleanSqlWord(string word, CurrentAutoCompletDatabaseMode autoCompletMode)
    {
        if (word is not null && (autoCompletMode & CurrentAutoCompletDatabaseMode.MakeUpperCase) != CurrentAutoCompletDatabaseMode.NotSet)
        {
            if (!word.StartsWith('"'))
            {
                word = word.ToUpper();
            }
            else if (word.StartsWith('"') && word.EndsWith('"'))
            {
                word = word[1..^1];
            }
        }
        return word;
    }

    public bool PrefrerUpperCase = true;
    public string QuoteNameIfNeeded(string word)
    {
        if (!word.IsGoodName(PrefrerUpperCase))
        {
            word = $"\"{word.Replace("\"", "\"\"")}\"";
        }
        return word;
    }


    protected readonly Dictionary<string, Dictionary<string, Dictionary<string, DatabaseObject>>> _databaseSchemaTable = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> _databaseDefSchema = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    //DatabaseSchemaTableColumn[database][schema][objectName][column]

    private readonly Dictionary<string, Dictionary<int, ColumnInterval>> DatabaseTableIdColumnIntervalSpan = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, DatabaseColumn[]> DatabaseColumnsList = new Dictionary<string, DatabaseColumn[]>(StringComparer.OrdinalIgnoreCase);

    public void ClearCachedData()
    {
        _databaseSchemaTable.Clear();
        _databaseDefSchema.Clear();
        DatabaseTableIdColumnIntervalSpan.Clear();
        DatabaseColumnsList.Clear();
    }

    public IEnumerable<string> GetDatabases(string filter)
    {
        foreach (var item in _databaseSchemaTable.Keys)
        {
            if (string.IsNullOrWhiteSpace(filter) || item.StartsWith(filter, StringComparison.OrdinalIgnoreCase) || item.Contains("_" + filter, StringComparison.OrdinalIgnoreCase))
            {
                yield return item;
            }
        }
    }
    public IEnumerable<string> GetSchemas(string database, string filter)
    {
        if (database is null && _databaseSchemaTable.Keys.Count > 0)
        {
            database = _databaseSchemaTable.Keys.ToArray()[0];
        }
        if (database is null)
        {
            yield break;
        }
        if (_databaseSchemaTable.TryGetValue(database, out var pairs))
        {
            foreach (var item in pairs.Keys)
            {
                if (string.IsNullOrWhiteSpace(filter) || item.StartsWith(filter, StringComparison.OrdinalIgnoreCase) || item.Contains("_" + filter, StringComparison.OrdinalIgnoreCase))
                {
                    yield return item;
                }
            }
        }
    }

    public IEnumerable<(DatabaseObject dbObject, string schema)> FindDbObject(string database, string schema, string name, bool cleanNames)
    {
        if (cleanNames)
        {
            database = CleanSqlWord(database, AutoCompletDatabaseMode);
            schema = CleanSqlWord(schema, AutoCompletDatabaseMode);
            name = CleanSqlWord(name, AutoCompletDatabaseMode);
        }

        if (database is null && _databaseSchemaTable.Keys.Count > 0)
        {
            database = _databaseSchemaTable.Keys.ToArray()[0];
        }
        if (database is not null && _databaseSchemaTable.TryGetValue(database, out var pairs))
        {
            foreach (var item in pairs)
            {
                if ((string.IsNullOrEmpty(schema) || item.Key.Equals(schema, StringComparison.OrdinalIgnoreCase))
                    && name is not null && item.Value.TryGetValue(name, out var res))
                {
                    yield return (res, item.Key);
                }
            }
        }
    }

    private List<string> GetAvaiableSchemas(string database, string? schema)
    {
        List<string> schemas = [];
        if (string.IsNullOrWhiteSpace(schema))
        {
            if (database is not null && database != "SYSTEM")
            {
                if (_databaseDefSchema.TryGetValue(database, out var schemaTmp) && schemaTmp is not null)
                {
                    schema = schemaTmp;
                    schemas.Add(schema);
                }
                else
                {
                    schema = "ADMIN";
                }
            }
            else if (_databaseSchemaTable.TryGetValue("SYSTEM", out var systemRes))
            {
                schemas.AddRange(systemRes.Keys);
            }
        }
        else if (schema is not null)
        {
            schemas.Add(schema);
        }
        return schemas;
    }
    public IEnumerable<DatabaseObject> GetDbObjects(string database, string schema, string filter, TypeInDatabaseEnum typeInDatabase)
    {
        if (database is null && _databaseSchemaTable.Keys.Count > 0)
        {
            database = _databaseSchemaTable.Keys.ToArray()[0];
        }
        if (database is null)
        {
            yield break;
        }
        if (_databaseSchemaTable.TryGetValue(database, out var pairs))
        {
            List<string> schemas = GetAvaiableSchemas(database, schema);
            foreach (var schemaX in schemas)
            {
                if (schemaX is not null && pairs.TryGetValue(schemaX, out var strings))
                {
                    foreach (var (_, item) in strings)
                    {
                        var itemName = item.Name;
                        if (string.IsNullOrWhiteSpace(filter) || itemName.Contains("_" + filter, StringComparison.OrdinalIgnoreCase)
                            || itemName.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                        {
                            if (typeInDatabase == TypeInDatabaseEnum.allObjects || item.TypeInDatabase == typeInDatabase)
                            {
                                yield return item;
                            }
                        }
                    }
                }
            }
        }
    }

    public IEnumerable<DatabaseColumn> GetColumns(string database, string schema, string table, string filter)
    {
        if (table is null)
        {
            yield break;
        }
        if (database is null && _databaseSchemaTable.Keys.Count > 0)
        {
            database = _databaseSchemaTable.Keys.ToArray()[0];
        }
        if (database is null)
        {
            yield break;
        }

        List<string> schemas = GetAvaiableSchemas(database, schema);

        if (schemas.Count == 0)
        {
            yield break;
        }

        if (_databaseSchemaTable.TryGetValue(database, out var schemaTableDict))
        {
            foreach (var schemaX in schemas)
            {
                if (schemaX is not null && schemaTableDict.TryGetValue(schemaX, out var tableDictionary))
                {
                    if (tableDictionary.TryGetValue(table, out var accualObject) &&
                        DatabaseTableIdColumnIntervalSpan.TryGetValue(database, out var columnDictionaryOfCurrentDatabase) &&
                        columnDictionaryOfCurrentDatabase.TryGetValue(accualObject.Id, out var columnInterval))
                    {
                        int firstColumnIndex = columnInterval.FirstIndex;
                        int lastColumnIndex = columnInterval.LastIndex;

                        if (DatabaseColumnsList.TryGetValue(database, out var columnsArray))
                        {
                            for (int i = firstColumnIndex; i < lastColumnIndex && i < columnsArray.Length; i++)
                            {
                                var item = columnsArray[i];
                                if (string.IsNullOrWhiteSpace(filter) || item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                                {
                                    yield return columnsArray[i];
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public IEnumerable<(DatabaseColumn, DatabaseObject)> GetColumnsFromAllTablesAndSchemas(string database, string schema)
    {
        foreach (var table in GetDbObjects(database, schema, "", TypeInDatabaseEnum.Table))
        {
            foreach (var item in GetColumns(database, schema, table.Name, ""))
            {
                yield return (item, table);
            }
        }
        foreach (var table in GetDbObjects(database, schema, "", TypeInDatabaseEnum.View))
        {
            foreach (var item in GetColumns(database, schema, table.Name, ""))
            {
                yield return (item, table);
            }
        }
    }

    protected Dictionary<string, Dictionary<string, Dictionary<string, PorcedureCachedInfo>>> _procedureDictCache = new();

    protected Dictionary<string, Dictionary<string, Dictionary<string, ViewCachedInfo>>> _viewDictCache = new();

    protected Dictionary<string, Dictionary<string, Dictionary<string, SynonymCachedInfo>>> _synonymTableDictCache = new();

    protected abstract string? GetProceduresSql(string database, string objectFilterName);

    protected virtual string? GetViewsSql(string database, string objectFilterName)
    {
        return "select table_schema,table_name, view_definition from information_schema.views";
    }

    protected abstract string? GetExternalTableSql(string database);

    protected abstract string? GetSynonymSql(string database);

    protected string? GetObjectCode(TypeInDatabaseEnum typeInDatabase, string database, string objectFilterName = "")
    {
        return typeInDatabase switch
        {
            TypeInDatabaseEnum.Procedure => GetProceduresSql(database, objectFilterName),
            TypeInDatabaseEnum.View => GetViewsSql(database, objectFilterName),
            TypeInDatabaseEnum.ExternalTable => GetExternalTableSql(database),
            TypeInDatabaseEnum.Synonym => GetSynonymSql(database),
            _ => null,
        };
    }

    protected (string, string, string) GetCleanedNames(string database, string schema, string tableName)
    {
        string cleanDatabaseName = database;
        if (cleanDatabaseName is not null)
        {
            cleanDatabaseName = QuoteNameIfNeeded(cleanDatabaseName);
        }
        string cleanSchema = schema;
        if (cleanSchema is not null)
        {
            cleanSchema = QuoteNameIfNeeded(cleanSchema);
        }
        string cleanTableName = tableName;
        if (cleanTableName is not null)
        {
            cleanTableName = QuoteNameIfNeeded(cleanTableName);
        }
        return (cleanDatabaseName, cleanSchema, cleanTableName);
    }

    protected (string, string) GetCleanedNames(string schema, string tableName)
    {
        string cleanSchema = schema;
        if (cleanSchema is not null)
        {
            cleanSchema = QuoteNameIfNeeded(cleanSchema);
        }
        string cleanTableName = tableName;
        if (cleanTableName is not null)
        {
            cleanTableName = QuoteNameIfNeeded(cleanTableName);
        }
        return (cleanSchema, cleanTableName);
    }
    protected static string? CleanComment(string comment)
    {
        if (comment is null)
        {
            return null;
        }
        else
        {
            return comment.Replace("'", "''");
        }
    }
    public virtual bool IsTypeInDatabaseSupported(TypeInDatabaseEnum typeInDatabase)
    {
        return typeInDatabase != TypeInDatabaseEnum.ExternalTable && typeInDatabase != TypeInDatabaseEnum.Synonym;
    }

    public virtual void ChangeDatabaseSpecial(DbConnection con, string databaseName)
    {
        con.ChangeDatabase(databaseName);
    }
    public string ChangeDatabaseIfNeeded(DbConnection con, string selectedDatabaseName)
    {
        if (this.DatabaseType == DatabaseTypeEnum.NetezzaSQL || this.DatabaseType == DatabaseTypeEnum.PostgreSql)
        {
            if (string.IsNullOrWhiteSpace(selectedDatabaseName))
            {
                selectedDatabaseName = con.Database;
            }
            ChangeDatabaseSpecial(con, selectedDatabaseName);
            return selectedDatabaseName;
        }
        return "";
    }
    private void ClearCache(TypeInDatabaseEnum[] typeInDatabaseArr)
    {
        if (typeInDatabaseArr.Contains(TypeInDatabaseEnum.Procedure))
        {
            _procedureDictCache.Clear();
        }
        if (typeInDatabaseArr.Contains(TypeInDatabaseEnum.View))
        {
            _viewDictCache.Clear();
        }
        if (typeInDatabaseArr.Contains(TypeInDatabaseEnum.ExternalTable) && this is INetezza netezza)
        {
            netezza.ClearExternalTableCache();
        }
        if (typeInDatabaseArr.Contains(TypeInDatabaseEnum.Synonym))
        {
            _synonymTableDictCache.Clear();
        }
    }

    public async Task CacheAllObjects(TypeInDatabaseEnum[] typeInDatabaseArr, string databaseName = "", string procedureName = "")
    {
        await Task.Run(() =>
        {
            lock (_lock1)
            {
                ClearCache(typeInDatabaseArr);

                //foreach (var database in GetDatabases(databaseName))
                Parallel.ForEach(GetDatabases(databaseName), new ParallelOptions() { MaxDegreeOfParallelism = 4 }, database =>
                {
                    try
                    {
                        using var con = GetConnection(database, pooling: false);
                        con.Open();

                        foreach (var typeInDatabase in typeInDatabaseArr)
                        {
                            if (!IsTypeInDatabaseSupported(typeInDatabase))
                            {
                                continue;
                            }
                            try
                            {
                                var cmd = CreateCommandFromConnection(con);
                                cmd.CommandText = GetObjectCode(typeInDatabase, database, procedureName);
                                var rdr = cmd.ExecuteReader();

                                if (typeInDatabase == TypeInDatabaseEnum.Procedure)
                                {
                                    while (rdr.Read())
                                    {
                                        string? schema = rdr.GetString(0);
                                        string? source = rdr.GetValue(1) as string ?? "";
                                        int id = rdr.GetInt32(2);

                                        string? RETURNS = rdr.GetValue(3) as string;
                                        if (this is INetezza netezza1)
                                        {
                                            RETURNS = netezza1.NetezzazProcWrongReturnFix(RETURNS);
                                        }

                                        object executeAsOwnerObj = rdr.GetValue(4);
                                        bool EXECUTEDASOWNER = false;
                                        if (executeAsOwnerObj is bool boolVal)
                                        {
                                            EXECUTEDASOWNER = boolVal;
                                        }
                                        else if (executeAsOwnerObj is short int16Num)
                                        {
                                            EXECUTEDASOWNER = int16Num == 1 ? true : false;
                                        }

                                        string stringDesc = rdr.GetValue(5) as string;
                                        string PROCEDURESIGNATURE = rdr.GetValue(6) as string;
                                        string ARGUMENTS = rdr.GetValue(7) as string;
                                        string LANGUAGE = rdr.GetValue(8) as string;

                                        Monitor.Enter(_procedureDictCache);

                                        ref var databaseItem = ref CollectionsMarshal.GetValueRefOrAddDefault(_procedureDictCache, database, out var _);
                                        databaseItem ??= new();
                                        ref var schemaItem = ref CollectionsMarshal.GetValueRefOrAddDefault(databaseItem, schema, out var _);
                                        schemaItem ??= new();

                                        schemaItem[PROCEDURESIGNATURE] = new PorcedureCachedInfo()
                                        {
                                            Id = id,
                                            ProcedureSource = source,
                                            Returns = RETURNS,
                                            ExecuteAsOwner = EXECUTEDASOWNER,
                                            Desc = stringDesc,
                                            ProcedureSignature = PROCEDURESIGNATURE,
                                            Arguments = ARGUMENTS,
                                            ProcLanguage = LANGUAGE
                                            //SPECIFICNAME = SPECIFICNAME
                                        };
                                        Monitor.Exit(_procedureDictCache);
                                    }
                                }
                                else if (typeInDatabase == TypeInDatabaseEnum.View)
                                {
                                    while (rdr.Read())
                                    {
                                        string? schema = rdr.GetValue(0) as string;
                                        string? viewName = rdr.GetString(1);
                                        string? source = rdr.GetString(2);

                                        Monitor.Enter(_viewDictCache);

                                        ref var databaseItem = ref CollectionsMarshal.GetValueRefOrAddDefault(_viewDictCache, database, out var _);
                                        databaseItem ??= new();
                                        ref var schemaItem = ref CollectionsMarshal.GetValueRefOrAddDefault(databaseItem, schema, out var _);
                                        schemaItem ??= new();

                                        schemaItem[viewName] = new ViewCachedInfo(source);
                                        Monitor.Exit(_viewDictCache);
                                    }
                                }
                                else if (typeInDatabase == TypeInDatabaseEnum.ExternalTable && this is INetezza netezza)
                                {
                                    netezza.ReadExternalTable(database, rdr);
                                }
                                else if (typeInDatabase == TypeInDatabaseEnum.Synonym)
                                {
                                    while (rdr.Read())
                                    {
                                        string? schema = rdr.GetValue(0) as string;
                                        string? name = rdr.GetString(1);
                                        string refObjName = rdr.GetString(2); // name
                                        string refObjNamePart1 = rdr.GetValue(3) as string ?? "PROBLEM"; //server or database
                                        string refObjNamePart2 = rdr.GetValue(4) as string ?? "PROBLEM"; // schema

                                        Monitor.Enter(_synonymTableDictCache);

                                        ref var databaseItem = ref CollectionsMarshal.GetValueRefOrAddDefault(_synonymTableDictCache, database, out var _);
                                        databaseItem ??= new();
                                        ref var schemaItem = ref CollectionsMarshal.GetValueRefOrAddDefault(databaseItem, schema, out var _);
                                        schemaItem ??= new();

                                        var syn = new SynonymCachedInfo(refObjNamePart1, refObjNamePart2, refObjName);

                                        schemaItem[name] = syn;
                                        Monitor.Exit(_synonymTableDictCache);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.TrackError(ex, isCrash: false);
                            }
                        }

                        con.Close();
                    }
                    catch (Exception ex)
                    {
                        Logger.TrackError(ex, isCrash: false);
                    }
                });
            }
        });
    }

    private string? GetProcedureSource(string database, string schema, string procedureName, int procedureId)
    {
        string? res = null;
        if (database is not null && _procedureDictCache.TryGetValue(database, out var schemas)
            && schema is not null && schemas.TryGetValue(schema, out var procedures)
            && procedureName is not null && procedures.TryGetValue(procedureName, out var procedure))
        {
            if (procedure.Id == procedureId || procedure.Id == -1)
            {
                res = procedure.ProcedureSource;
            }
        }
        return res;
    }

    public bool IsItemSourceContains(TypeInDatabaseEnum typeInDatabase, string database, string schema, string itemNameOrSignature, int procedureId, StringComparison comp, string searchWord, Regex rx)
    {
        string? res = "";
        if (database is not null)
        {
            switch (typeInDatabase)
            {
                case TypeInDatabaseEnum.Procedure:
                    res = GetProcedureSource(database, schema, itemNameOrSignature, procedureId);
                    break;
                case TypeInDatabaseEnum.View:
                    res = GetViewSource(database, schema, itemNameOrSignature, procedureId);
                    break;
                case TypeInDatabaseEnum.ExternalTable:
                    if (this is INetezza netezza)
                    {
                        res = netezza.GetExternalDataObject(database, schema, itemNameOrSignature);
                    }
                    break;
                case TypeInDatabaseEnum.Synonym:
                    if (_synonymTableDictCache.TryGetValue(database, out var tmp1) && schema is not null && tmp1.TryGetValue(schema, out var tmp2)
                        && itemNameOrSignature is not null && tmp2.TryGetValue(itemNameOrSignature, out var finalX))
                    {
                        res = finalX.RefObjNamePart3;
                    }
                    break;
                default:
                    break;
            }
        }
        if (string.IsNullOrEmpty(res))
        {
            return false;
        }

        if (rx is not null)
        {
            return rx.IsMatch(res);
        }
        else
        {
            return res.Contains(searchWord, comp);
        }
    }

    public virtual async ValueTask<List<PorcedureCachedInfo>> GetProceduresSignaturesFromName(string database, string schema, string procName)
    {
        await Task.CompletedTask;
        return [new PorcedureCachedInfo()];
    }
    private string? GetViewSource(string database, string schema, string procedureName, int procedureId)
    {
        string? res = null;
        if (database is not null && _viewDictCache.TryGetValue(database, out var schemas)
            && schema is not null && schemas.TryGetValue(schema, out var procedures)
            && procedureName is not null && procedures.TryGetValue(procedureName, out var view)
            )
        {
            res = view.ViewSource;
        }
        return res;
    }

    public virtual string GetTableDropCode(string fullName)
    {
        return $"DROP TABLE {fullName};";
    }
    public virtual string GetTableRenameCode(string fullName)
    {
        return $"ALTER TABLE {fullName} RENAME TO ABC;";
    }
    public virtual string GetShortSelectCode(string fullName)
    {
        return $"SELECT T1.* FROM {fullName} AS T1 {GetLimitClause(100)};";
    }
    public virtual string GetCreateFromCode(string fullName)
    {
        return $"CREATE TABLE ABC AS (SELECT T1.* FROM {fullName} AS T1) DISTRIBUTE ON RANDOM;";
    }

    private bool _initialized = false;
    private static readonly Lock _lock1 = new Lock();
    protected static readonly Lock _lock2 = new Lock();

    public void CacheMainDictionary()
    {
        if (_initialized)
        {
            return;
        }

        lock (_lock1)
        {
            _initialized = true;
            List<(string, string)> databasesList = GetDatabases();
            foreach (var (database, defSchema) in databasesList)
            {
                _databaseSchemaTable[database] = new(StringComparer.OrdinalIgnoreCase);
                _databaseDefSchema[database] = defSchema;
            }

            //foreach (var o in databasesList)
            Parallel.ForEach(databasesList, new ParallelOptions { MaxDegreeOfParallelism = 4 }, o =>
            {
                try
                {
                    var (database, _) = o;
                    if (database == "template0" && DatabaseType == DatabaseTypeEnum.PostgreSql) // cannot to connect this
                    {
                        return;
                    }

                    var con = GetConnection(database, pooling: false,forSchema: true);
                    try
                    {
                        con.Open();
                        LoadDatabaseObject(database, con);
                        ConnectedLevel = DatabaseConnectedLevel.ConnectedDatabaseObjects;
                        if (DatabaseType == DatabaseTypeEnum.PostgreSql)
                        {
                            con.Close();
                            con.Dispose();
                            con = GetConnection(database, false, true);
                            con.Open();
                        }
                        LoadColumns(database, con);
                        if (this is INetezza netezza)
                        {
                            netezza.FillDistInfoForDatabase(database, con);
                            netezza.FillKeysInfoForDatabase(database, con);
                        }
                        ConnectedLevel = DatabaseConnectedLevel.ConnectedColumns;
                        con.Close();
                    }
                    finally
                    {
                        //GC.SuppressFinalize(con);//TEST
                        con.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    if (!ex.Message.StartsWith("55000")) // ???
                    {
                        Logger.TrackError(ex, isCrash: false);
                    }
                }
            });
        }
    }

    // HACK ?
    //public int WaitDbToSynced()
    //{
    //    int a = 0;
    //    lock (_lock1)
    //    {
    //        a = 0;
    //    }
    //    return a;
    //}
    protected abstract string GetSqlTablesAndOtherObjects(string dbName);
    protected abstract string GetSqlOfColumns(string dbName);

    private void LoadDatabaseObject(string database, DbConnection con)
    {
        var cmd = CreateCommandFromConnection(con);
        cmd.CommandText = GetSqlTablesAndOtherObjects(database);
        var rdr = cmd.ExecuteReader();
        var acualDb = _databaseSchemaTable[database];
        while (rdr.Read())
        {
            int objId = rdr.GetInt32(0);
            string objNme = rdr.GetString(1);
            string? desc = rdr.GetValue(2) as string;
            string schema = rdr.GetString(3);
            string databaseObjectType = rdr.GetString(4);
            string owner = rdr.GetString(5);
            DateTime? crtTime = rdr.GetValue(6) as DateTime?;
            TypeInDatabaseEnum dbType = databaseObjectType.GetTypeInDatabaseEnumFromDbName();

            _ = acualDb.TryAdd(schema, new Dictionary<string, DatabaseObject>()); // no StringComparer.OrdinalIgnoreCase by purpouse
            acualDb[schema][objNme] = new DatabaseObject(objId, objNme, desc, dbType, databaseObjectType, owner, crtTime);
        }
        if (DatabaseType == DatabaseTypeEnum.PostgreSql)
        {
            cmd.Dispose();
        }
    }

    public DbCommand CreateCommandFromConnection(DbConnection con)
    {
        var cmd = con.CreateCommand();
        ConfigureStringPoolForSchema(cmd);
        return cmd;
    }

    protected virtual void ConfigureStringPoolForSchema(DbCommand cmd)
    {
        //current only for netezza dotnet driver
    }

    private void LoadColumns(string database, DbConnection con)
    {
        var currentDic = new Dictionary<int, ColumnInterval>();

        var cmd = CreateCommandFromConnection(con);

        cmd.CommandText = GetSqlOfColumns(database);
        var rdr = cmd.ExecuteReader();

        List<DatabaseColumn> tempCols = new List<DatabaseColumn>();
        int num = 0;
        int prevObjId = -1;
        int tmpA = 0;

        while (rdr.Read())
        {
            string columnName = rdr.GetString(1);
            int obejctId = rdr.GetInt32(0);

            string? desc = rdr.GetValue(2) as string;
            string columnTypeFullName = rdr.GetString(3);

            var notNull = rdr.GetValue(4);
            bool columnNotNull = false;
            if (notNull is bool boolNotNull) // false/true
            {
                columnNotNull = boolNotNull;
            }
            else if (notNull is int intNotNull) // 0/1
            {
                columnNotNull = intNotNull > 0;
            }

            if (prevObjId != -1 && prevObjId != obejctId)
            {
                currentDic[prevObjId] = new ColumnInterval() { FirstIndex = tmpA, LastIndex = num };
                tmpA = num;
            }
            prevObjId = obejctId;
            string? colDefValue = rdr.GetValue(5) as string;

            tempCols.Add(new DatabaseColumn(columnName, desc, columnTypeFullName, columnNotNull, colDefValue));
            num++;
        }
        currentDic[prevObjId] = new ColumnInterval() { FirstIndex = tmpA, LastIndex = num };
        lock (_lock2)
        {
            DatabaseTableIdColumnIntervalSpan[database] = currentDic;
            DatabaseColumnsList[database] = tempCols.ToArray();
        }
        if (DatabaseType == DatabaseTypeEnum.PostgreSql)
        {
            cmd.Dispose();
        }
    }
    protected abstract List<(string databaseName, string defaultSchema)> GetDatabases();
    protected string GetQuotedTwoOrTreePartName(string? database, string schema, string table, bool force = false)
    {
        if (!preferDatabaseInCodes && !force)
        {
            database = null;
        }

        if (table is not null)
        {
            table = QuoteNameIfNeeded(table);
        }
        if (database is not null)
        {
            database = QuoteNameIfNeeded(database);
        }
        if (schema is not null)
        {
            schema = QuoteNameIfNeeded(schema);
        }

        string tableCl = "";

        if (database is not null && schema is not null)
        {
            tableCl = $"{database}.{schema}.{table}";
        }
        else if (schema is not null)
        {
            tableCl = $"{schema}.{table}";
        }
        else
        {
            tableCl = $"{table}";
        }

        return tableCl;
    }
    public string GetTop100Select(string database, string schema, string table, bool snippetMode, bool addWhereToTextCols = false)
    {
        var cols = GetColumns(database, schema, table, "");
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        if (snippetMode)
        {
            var colList = string.Join("\r\n    , ",
                cols.Select(o =>
                {
                    return "${ALIAS}." + QuoteNameIfNeeded(o.Name);
                })
            );

            return $$"""
            SELECT 
                {{colList}}
            FROM {{tableCl}} ${ALIAS=T1}
            {{GetLimitClause(100)}}${Caret};
            """;
        }
        else
        {
            string aliasText = PrefrerUpperCase ? "T1" : "t1";
            var colList = string.Join("\r\n    , ", cols.Select(o => $"{aliasText}.{QuoteNameIfNeeded(o.Name)}"));
            string declareAddition = "";
            string whereAddition = "";

            if (addWhereToTextCols)
            {
                declareAddition = "declare &SEARCHED = UPPER('%${TEXT TO SEARCH}%');\r\n";
                var colsToWhere = cols.Where(o => o.FullTypeName.Contains("CHARACTER", StringComparison.OrdinalIgnoreCase)).Select(o =>
                {
                    return $"UPPER({aliasText}.{QuoteNameIfNeeded(o.Name)}) LIKE &SEARCHED";
                });
                if (colsToWhere.Count() == 0)
                {
                    whereAddition =
                        """

                    where 1=2 -- no text columns
                    """;
                }
                else
                {
                    whereAddition =
                        $"""
                    
                WHERE 
                    --REGION WHERE CODE
                    {string.Join("\r\n  OR ", colsToWhere)}
                    --ENDREGION
                """;
                }
            }

            return $$"""
            {{declareAddition}}SELECT 
                --REGION COLS
                {{colList}}
                --ENDREGION
            FROM {{tableCl}} {{aliasText}}{{whereAddition}}
            {{GetLimitClause(100)}};
            """;
        }
    }

    public const string TABS_WITH_ROWS = "--##RETURN_ONLY_TABS_WITH_ROWS";
    public const string TIMEOUT_OVERRIDE = "--##TIMEOUT_OVERRIDE:";
    public const string CONTINUE_ON_ERROR = "--##CONTINUE_ON_ERROR";
    public string GetTop100SelectTextFromTables(string database, string schema, IEnumerable<DatabaseObject> tables)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(TABS_WITH_ROWS);
        sb.AppendLine($"{TIMEOUT_OVERRIDE}:20");
        sb.AppendLine(CONTINUE_ON_ERROR);
        sb.AppendLine(";declare &SEARCHED = UPPER('%${TEXT TO SEARCH}%');");
        sb.AppendLine("declare &LIMIT_CNT = 100;");
        string aliasText = PrefrerUpperCase ? "T1" : "t1";
        foreach (var item in tables)
        {
            var table = item.Name;
            var cols = GetColumns(database, schema, table, "");
            var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

            var colList = string.Join("\r\n    , ", cols.Select(o => $"{aliasText}.{QuoteNameIfNeeded(o.Name)}"));

            string whereAddition = "";
            var colsToWhere = cols.Where(o => o.FullTypeName.Contains("CHARACTER", StringComparison.OrdinalIgnoreCase)
            || o.FullTypeName.StartsWith("CHAR", StringComparison.OrdinalIgnoreCase)
            || o.FullTypeName.StartsWith("NCHAR", StringComparison.OrdinalIgnoreCase)
            || o.FullTypeName.StartsWith("VARCHAR", StringComparison.OrdinalIgnoreCase)
            || o.FullTypeName.StartsWith("VARCHAR2", StringComparison.OrdinalIgnoreCase)
            || o.FullTypeName.StartsWith("NVARCHAR", StringComparison.OrdinalIgnoreCase)
            || o.FullTypeName.StartsWith("TEXT", StringComparison.OrdinalIgnoreCase)
            || o.FullTypeName.StartsWith("NTEXT", StringComparison.OrdinalIgnoreCase)
            ).Select(o =>
            {
                return $"UPPER({aliasText}.{QuoteNameIfNeeded(o.Name)}) LIKE &SEARCHED";
            });
            if (!colsToWhere.Any())
            {
                continue;
            }
            else
            {
                whereAddition =
                    $"""
                    
                WHERE 
                    --REGION WHERE CODE
                    {string.Join("\r\n  OR ", colsToWhere)}
                    --ENDREGION
                """;
            }
            sb.AppendLine($$"""
            --REGION RESULT_NAME:{{tableCl}}
            SELECT 
                --REGION COLS
                {{colList}}
                --ENDREGION
            FROM {{tableCl}} {{aliasText}}{{whereAddition}}
            {{GetLimitClause("&LIMIT_CNT")}}
            --ENDREGION
            ;
            """);
        }

        return sb.ToString();
    }

    public string GetTop100SelectNumberFromTables(string database, string schema, IEnumerable<DatabaseObject> tables)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(TABS_WITH_ROWS);
        sb.AppendLine($"{TIMEOUT_OVERRIDE}:20");
        sb.AppendLine(CONTINUE_ON_ERROR);
        sb.AppendLine(";declare &SEARCHED = ${123};");
        sb.AppendLine("declare &LIMIT_CNT = 100;");
        string aliasText = PrefrerUpperCase ? "T1" : "t1";
        foreach (var item in tables)
        {
            var table = item.Name;
            var cols = GetColumns(database, schema, table, "");
            var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

            var colList = string.Join("\r\n    , ", cols.Select(o => $"{aliasText}.{QuoteNameIfNeeded(o.Name)}"));

            string whereAddition = "";
            var colsToWhere = cols.Where(o => o.FullTypeName.StartsWith("INT", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("BIGINT", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("SMALLINT", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("TINYINT", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("BYTEINT", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("NUMERIC", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("DECIMAL", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("NUMBER", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("FLOAT", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("DOUBLE", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("REAL", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("DECFLOAT", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("MONEY", StringComparison.OrdinalIgnoreCase)
             || o.FullTypeName.StartsWith("SMALLMONEY", StringComparison.OrdinalIgnoreCase)
            ).Select(o =>
            {
                return $"{aliasText}.{QuoteNameIfNeeded(o.Name)} = &SEARCHED";
            });
            if (!colsToWhere.Any())
            {
                continue;
            }
            else
            {
                whereAddition =
                    $"""
                    
                WHERE 
                    --REGION WHERE CODE
                    {string.Join("\r\n  OR ", colsToWhere)}
                    --ENDREGION
                """;
            }
            sb.AppendLine($$"""
            --REGION RESULT_NAME:{{tableCl}}
            SELECT 
                --REGION COLS
                {{colList}}
                --ENDREGION
            FROM {{tableCl}} {{aliasText}}{{whereAddition}}
            {{GetLimitClause("&LIMIT_CNT")}}
            --ENDREGION
            ;
            """);
        }

        return sb.ToString();
    }

    public virtual string GetDuplicates(string table, string database, string schema)
    {
        var cols = GetColumns(database, schema, table, "");
        var colListString = cols.Select(o => QuoteNameIfNeeded(o.Name));
        var colList = string.Join("\r\n    , ", colListString.Append("COUNT(1)"));
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);
        return $"""
            SELECT 
                {colList} 
            FROM {tableCl} 
            GROUP BY
                {string.Join("\r\n    , ", colListString)}
            HAVING
                COUNT(1) > 1
            {GetLimitClause(100)};
            """;
    }

    public virtual string GetDeleted(string table, string database, string schema)
    {
        return "not supported";
    }
    public virtual string GetGrant(string database, string schema, string table)
    {
        return "not supported";
    }
    public virtual string GetOrganize(string database, string schema, string table)
    {
        return "not supported";
    }
    public virtual string GetGroom(string database, string schema, string table)
    {
        return "not supported";
    }
    public virtual string GetDrop(string table, string database, string schema)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return @$"DROP TABLE {tableCl};";
    }
    public virtual string GetEmpty(string table, string database, string schema)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return @$"TRUNCATE TABLE {tableCl};";
    }
    public virtual string GetExport(string table, string database, string schema)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);
        string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var invalids = Path.GetInvalidFileNameChars();
        var sanitizedName = string.Join("_", tableCl.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');

        return @$"@expCsv: SELECT * FROM {tableCl} -> {path}\{sanitizedName}.csv;";
    }
    public virtual string GetImport(string table, string database, string schema)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);
        return @$"IMPORTING CODE TO DO..  {tableCl};";
    }

    public virtual string GetGenerateStats(string database, string schema, string table)
    {
        return "not supported";
    }

    public virtual string GetAddComment(string table, string database, string schema)
    {
        return "not supported";
    }

    public abstract ValueTask GetCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName, string overrideTableName = null, string middleCode = null, string endingCode = null, List<string> distOverride = null);

    public async ValueTask<string> GetCreateTableText(string database, string schema, string tableName, string overrideTableName = null, string middleCode = null, string endingCode = null, List<string> distOverride = null)
    {
        StringBuilder stringBuilder = new StringBuilder();
        await GetCreateTableTextStringBuilder(stringBuilder, database, schema, tableName, overrideTableName, middleCode, endingCode, distOverride);
        return stringBuilder.ToString();
    }

    public virtual async ValueTask GetReCreateTableTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string tableName)
    {
        await Task.CompletedTask;
    }

    public async ValueTask<string> GetReCreateTableText(string database, string schema, string tableName)
    {
        StringBuilder stringBuilder = new();
        await GetReCreateTableTextStringBuilder(stringBuilder, database, schema, tableName);
        return stringBuilder.ToString();
    }

    public async ValueTask<string> GetCreateExternalText(string database, string schema, string tableName)
    {
        StringBuilder stringBuilder = new();
        await GetCreateExternalTextStringBuilder(stringBuilder, database, schema, tableName);
        return stringBuilder.ToString();
    }

    public virtual async ValueTask GetCreateExternalTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string tableName)
    {
        await Task.CompletedTask;
    }

    public async ValueTask<string> GetCreateSynonymText(string database, string schema, string synonymName)
    {
        StringBuilder stringBuilder = new();
        await GetCreateSynonymTextStringBuilder(stringBuilder, database, schema, synonymName);
        return stringBuilder.ToString();
    }

    public virtual async ValueTask GetCreateSynonymTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string synonymName)
    {
        if (!_synonymTableDictCache.ContainsKey(database))
        {
            await CacheAllObjects([TypeInDatabaseEnum.Synonym], database);
        }

        if (_synonymTableDictCache.TryGetValue(database, out var d1) && d1.TryGetValue(schema, out var d2) && d2.TryGetValue(synonymName, out var d3))
        {
            var f = GetQuotedTwoOrTreePartName(database, schema, synonymName);
            var g = GetQuotedTwoOrTreePartName(d3.RefObjNamePart1, d3.RefObjNamePart2, d3.RefObjNamePart3, force: true);
            stringBuilder.Append($"CREATE SYNONYM {f} FOR {g};");
            return;
        }
        stringBuilder.Append($"PROBLEM ! {database}.{schema}.{synonymName}");
    }


    public string GetCreateSynonymPatternText()
    {
        return "CREATE SYNONYM <synonym> FOR <name>";
    }

    public string GetCreateSequencePatternText()
    {
        return
            """         
            CREATE SEQUENCE CUSTOMER_112 AS BIGINT 
               START WITH 1 
               INCREMENT BY 1 
               MINVALUE 0
               NO MAXVALUE
               NO CYCLE;
            """;
    }
    public virtual string GetCreateProcedurePatternText()
    {
        return
            """
            -- TO DO SAMPLE PROCEDURE...
            """;
    }

    public virtual string GetCheckDistributeText(string database, string schema, string tableName)
    {
        return "not implemented yet";
    }

    public virtual string GetKeyCodeText(string database, string schema, string tableName)
    {
        var f = GetQuotedTwoOrTreePartName(database, schema, tableName);
        var d = QuoteNameIfNeeded($"PK_{tableName}");
        return $"ALTER TABLE {f} ADD CONSTRAINT {d} PRIMARY KEY (<COL1>,<COL2>);";
    }

    public virtual string GetKeyUiqueCodeText(string database, string schema, string tableName)
    {
        var f = GetQuotedTwoOrTreePartName(database, schema, tableName);
        var d = QuoteNameIfNeeded($"UK_{tableName}");
        return $"ALTER TABLE {f} ADD CONSTRAINT {d} UNIQUE (<COL1>,<COL2>);";
    }

    public async ValueTask<string> GetCreateViewText(string database, string schema, string tableName)
    {
        var stringBuilder = new StringBuilder();
        await GetCreateViewTextStringBuilder(stringBuilder, database, schema, tableName);
        return stringBuilder.ToString();
    }

    public virtual async ValueTask GetCreateViewTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string tableName)
    {
        await Task.CompletedTask;
    }

    public async ValueTask<string> GetCreateProcedureText(string database, string schema, string procedureName, bool forceFreshCode = false)
    {
        var stringBuilder = new StringBuilder();
        await GetCreateProcedureTextStringBuilder(stringBuilder, database, schema, procedureName, forceFreshCode);
        return stringBuilder.ToString();
    }
    public virtual async ValueTask GetCreateProcedureTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string tableName, bool forceFreshCode = false)
    {
        await Task.CompletedTask;
    }

    public virtual string GetCreateProcedureCall(string database, string schema, string tableName)
    {
        var f = GetQuotedTwoOrTreePartName(database, schema, tableName);
        return $"CALL PROCEDURE {f}";
    }

    public virtual Task DbSpecificImportPart(IDbImportJob importJob, string randName, Action<string>? progress, bool tableExists = false)
    {
        throw new NotImplementedException();
    }

    public virtual (int position, int length) HanleExceptions(ReadOnlySpan<char> sqlText, Exception exception)
    {
        return (-1, -1);
    }

    public virtual IDatabaseRowReader GetDatabaseRowReader(DbDataReader reader)
    {
        return new DatabaseRowReaderGeneral(reader);
    }
    public static string KeyNameFromChar(char c)
    {
        return c switch
        {
            'f' => "FOREIGN KEY",
            'p' => "PRIMARY KEY",
            'u' => "UNIQUE",
            _ => "TO DO"
        };
    }
    public abstract DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false);

}
