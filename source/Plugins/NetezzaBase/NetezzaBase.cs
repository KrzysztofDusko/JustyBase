using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Text;
using JustyBase.Helpers.Importers;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.PluginDatabaseBase.Enums;
using JustyBase.StringExtensions;

namespace JustyBase.Services.Database;

public class NetezzaBase : DatabaseService, INetezza
{
    public NetezzaBase(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
    {
        AutoCompletDatabaseMode = CurrentAutoCompletDatabaseMode.DatabaseSchemaTable |
            CurrentAutoCompletDatabaseMode.SchemaOptional |
            CurrentAutoCompletDatabaseMode.SchemaTable |
            CurrentAutoCompletDatabaseMode.DatabaseAndSchemaOptional |
            CurrentAutoCompletDatabaseMode.NullSchemaCanBeAccepted |
            CurrentAutoCompletDatabaseMode.MakeUpperCase;
    }

    public override DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false)
    {
        throw new NotImplementedException();
    }


    public override void ChangeDatabaseSpecial(DbConnection con, string databaseName)
    {
        try
        {
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
            var cmdX = con.CreateCommand();
            cmdX.CommandText = $"SET CATALOG {databaseName}";
            cmdX.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            if (ex.Message?.StartsWith("Failed to establish a connection to ") == false)
            {
                con.ChangeDatabase(databaseName);
            }
            else
            {
                throw;
            }
        }
    }


    protected override List<(string, string)> GetDatabases()
    {
        var con = Connection;

        if (con.State != ConnectionState.Open)
        {
            con.Open();
        }
        var cmd = CreateCommandFromConnection(con);
        cmd.CommandText =
            """
            SELECT 
                OBJID::INT AS OBJID
                , DEFSCHEMAID::INT AS DEFSCHEMAID
                , DATABASE
                , OWNER
                , DEFSCHEMA
            FROM _V_DATABASE
            ORDER BY DATABASE
            """;
        var rdr = cmd.ExecuteReader();
        List<(string, string)> databases = new();
        while (rdr.Read())
        {
            databases.Add((rdr.GetString(2), rdr.GetString(4)));
        }
        return databases;
    }
    protected override string GetSqlTablesAndOtherObjects(string dbName)
    {
        bool noDescMode = (dbName == "SYSTEM");
        bool ownerMode = false;

        if (!dbName.StartsWith('"'))
        {
            dbName = dbName.ToUpper();
        }

        string ownerOrSchema = "";

        if (dbName == "SYSTEM")
        {
            ownerOrSchema = @"CASE WHEN D1.SCHEMAID = 4 THEN 'DEFINITION_SCHEMA' 
 WHEN D1.SCHEMAID = 5 THEN 'INFORMATION_SCHEMA'
WHEN D1.SCHEMA IS NULL OR D1.SCHEMA = '' THEN 'ADMIN' ELSE D1.SCHEMA END AS SCHEMA_X";
        }
        else if (!ownerMode)
        {
            ownerOrSchema = "CASE WHEN D1.SCHEMA IS NULL OR D1.SCHEMA = '' THEN 'ADMIN' ELSE D1.SCHEMA END AS SCHEMA_X";
        }
        else
        {
            ownerOrSchema = " CASE WHEN D1.OWNER IS NULL OR D1.OWNER = '' THEN 'ADMIN' ELSE D1.OWNER END AS SCHEMA_X";
        }


        string dbWhere = $"AND D1.DBNAME = '{dbName}'";
        if (noDescMode)
        {
            string systemSql =
        $"""
            SELECT 
                OBJID::INT AS OBJID
                , OBJNAME
                , D1.DESCRIPTION
                , {ownerOrSchema}
                , CASE OBJTYPE
                WHEN  'SYSTEM TABLE' THEN 'TABLE'
                WHEN 'SYSTEM VIEW' THEN 'VIEW' END 
                , D1.OWNER
                , D1.CREATEDATE
            FROM SYSTEM.._V_OBJECT_DATA D1
            WHERE OBJTYPE IN ('SYSTEM TABLE','SYSTEM VIEW')
            AND DBNAME = 'SYSTEM'
        """;
            dbWhere += $" UNION ALL {systemSql}";
        }

        string sql =
        $"""
            SELECT 
                    D1.OBJID::INT AS OBJID
                , COALESCE(F.FUNCTIONSIGNATURE, PR.PROCEDURESIGNATURE,D1.OBJNAME) AS OBJNAME_
                , D1.DESCRIPTION
                , {ownerOrSchema}
                , CASE WHEN f.ENV like '%com.ibm.nz.fq.SqlReadLauncher%' THEN 'FLUID' ELSE D1.OBJTYPE END AS OBJTYPE_
                , D1.OWNER
                , D1.CREATEDATE
            FROM 
                {dbName}.._V_OBJECT_DATA D1
                LEFT JOIN {dbName}.._V_PROCEDURE PR ON PR.OBJID = D1.OBJID AND PR.DATABASE = '{dbName}'
                LEFT JOIN {dbName}.._V_FUNCTION F ON F.OBJID = D1.OBJID AND F.DATABASE = '{dbName}'
            WHERE 
                D1.OBJTYPE NOT IN --pasted 16 unique from 16
                ('AGGREGATE','CONSTRAINT','DATABASE','DATATYPE','GROUP','MANAGEMENT INDEX','MANAGEMENT SEQ','MANAGEMENT TABLE',
                'MANAGEMENT VIEW','SCHEDULER RULE','SCHEMA','SYSTEM INDEX','SYSTEM SEQ','SYSTEM TABLE','SYSTEM VIEW','USER')
            AND D1.OBJID NOT IN (4,5)
            {dbWhere}
            ORDER BY SCHEMA_X, OBJTYPE_, OBJNAME_
        """;

        if (dbName == "SYSTEM" && !noDescMode)
        {
            sql = 
            """
                SELECT 
                    OBJID::INT AS OBJID
                    , OBJNAME
                    , D1.DESCRIPTION
                    , OWNER
                    , CASE OBJTYPE
                   WHEN  'SYSTEM TABLE' THEN 'TABLE'
                   WHEN 'SYSTEM VIEW' THEN 'VIEW' END 
                    , D1.OWNER
                    , D1.CREATEDATE
                FROM SYSTEM.._V_OBJECT_DATA D1
                WHERE OBJTYPE IN ('SYSTEM TABLE','SYSTEM VIEW')
                AND DBNAME = 'SYSTEM'
            """;
        }

        return sql;
    }
    protected override string GetSqlOfColumns(string dbName)
    {
        if (!dbName.StartsWith('"'))
        {
            dbName = dbName.ToUpper();
        }

        return
            $"""
            SELECT 
                    X.OBJID::INT AS OBJID
                    , X.ATTNAME
                    , X.DESCRIPTION
                    , CASE WHEN X.ATTNOTNULL THEN X.FORMAT_TYPE || ' NOT NULL'  ELSE X.FORMAT_TYPE END
                    , X.ATTNOTNULL::BOOL AS ATTNOTNULL
                    , X.COLDEFAULT
                FROM
                    {dbName}.._V_RELATION_COLUMN X
                WHERE
                    X.TYPE IN ('TABLE','VIEW','EXTERNAL TABLE', 'SEQUENCE','SYSTEM VIEW','SYSTEM TABLE')
                    AND X.OBJID NOT IN (4,5)
                    AND DATABASE = '{dbName}'
                ORDER BY 
                    X.OBJID, X.ATTNUM
            """;
    }

    protected override string? GetProceduresSql(string database, string objectFilterName)
    {
        string whereOnSpecificObject = "";
        if (!string.IsNullOrEmpty(objectFilterName))
        {
            whereOnSpecificObject = $" AND PROCEDURESIGNATURE = '{objectFilterName}'";
        }

        return
            $"""
                SELECT SCHEMA,PROCEDURESOURCE,OBJID::INT 
                    ,RETURNS, EXECUTEDASOWNER, DESCRIPTION, PROCEDURESIGNATURE, ARGUMENTS, NULL AS LANGUAGE
                    --, '' AS SPECIFICNAME
                FROM {database}.._V_PROCEDURE
                    WHERE DATABASE = '{database}'{whereOnSpecificObject}
                    ORDER BY 1,2,3;
                """;
    }
    //returns ay..
    public string? NetezzazProcWrongReturnFix(string? procReturns)
    {
        if (procReturns == "CHARACTER VARYING")
        {
            procReturns = "CHARACTER VARYING(ANY)";
        }
        else if (procReturns == "NATIONAL CHARACTER VARYING")
        {
            procReturns = "NATIONAL CHARACTER VARYING(ANY)";
        }
        else if (procReturns == "NATIONAL CHARACTER")
        {
            procReturns = "NATIONAL CHARACTER(ANY)";
        }
        else if (procReturns == "CHARACTER")
        {
            procReturns = "CHARACTER(ANY)";
        }
        return procReturns;
    }

    protected override string? GetSynonymSql(string database)
    {
        return $"SELECT SCHEMA,SYNONYM_NAME, REFOBJNAME, REFDATABASE, REFSCHEMA, DESCRIPTION FROM {database}.._V_SYNONYM WHERE DATABASE = '{database}'";
    }

    protected override string? GetViewsSql(string database, string objectFilterName)
    {
        string whereOnSpecificObject = "";
        if (!string.IsNullOrEmpty(objectFilterName))
        {
            whereOnSpecificObject = $" AND VIEWNAME = '{objectFilterName}'";
        }
        return
            $"""
                SELECT SCHEMA,VIEWNAME, DEFINITION FROM {database}.._V_VIEW
                    WHERE DATABASE = '{database}'{whereOnSpecificObject}
                    ORDER BY 1,2,3;
            """;
    }

    protected override string? GetExternalTableSql(string database)
    {
        return
            $"""
            SELECT 
                    E1.SCHEMA
                    , E1.TABLENAME
                    , E2.EXTOBJNAME
                    , E2.OBJID::INT
                    , E1.DELIM
                    , E1.ENCODING
                    , E1.TIMESTYLE
                    , E1.REMOTESOURCE
                    , E1.SKIPROWS
                    , E1.MAXERRORS
                    , E1.ESCAPE
                    , E1.LOGDIR
                    , E1.DECIMALDELIM
                    , E1.QUOTEDVALUE
                    , E1.NULLVALUE
                    , E1.CRINSTRING
                    , E1.TRUNCSTRING
                    , E1.CTRLCHARS
                    , E1.IGNOREZERO
                    , E1.TIMEEXTRAZEROS
                    , E1.Y2BASE
                    , E1.FILLRECORD
                    , E1.COMPRESS
                    , E1.INCLUDEHEADER
                    , E1.LFINSTRING
                    , E1.DATESTYLE
                    , E1.DATEDELIM
                    , E1.TIMEDELIM
                    , E1.BOOLSTYLE
                    , E1.FORMAT
                    , E1.SOCKETBUFSIZE
                    , E1.RECORDDELIM
                    , E1.MAXROWS
                    , E1.REQUIREQUOTES
                    , E1.RECORDLENGTH
                    , E1.DATETIMEDELIM
                    , E1.NULLINDICATOR
                    , E1.REJECTFILE 
                    --, CODESET
                    --, CLOUD_CONNSTRING
                    --, DISTSTATS -- NEW NPS
                    --, ADJUSTDISTZEROINT -- NEW NPS
                FROM 
                    {database}.._V_EXTERNAL E1
                    JOIN {database}.._V_EXTOBJECT E2 ON E1.DATABASE = E2.DATABASE
                        AND E1.SCHEMA = E2.SCHEMA
                        AND E1.TABLENAME = E2.TABLENAME
                WHERE 
                    E1.DATABASE = '{database}';
            """;
    }

    public override string GetDeleted(string table, string database, string schema)
    {
        var cols = GetColumns(database, schema, table, "");
        var colList = String.Join("\r\n    , ", cols.Select(o => QuoteNameIfNeeded(o.Name)));

        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return
            $"""
            SET show_deleted_records = 1;
            SELECT T1.createxid, T1.deletexid, T1.* FROM {tableCl}  T1 WHERE deletexid != 0;
            SET show_deleted_records = 0;
            """;
    }

    public override string GetGrant(string database, string schema, string table)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return @$"GRANT SELECT ON {tableCl} TO SOME_OWNER?;
--https://www.ibm.com/docs/en/netezza?topic=npsscr-grant-2";

    }

    public override string GetOrganize(string database, string schema, string table)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return @$"ALTER TABLE {tableCl} ORGANIZE ON (<COL1>, <COL2>);
--https://www.ibm.com/docs/en/netezza?topic=tables-select-organizing-keys";

    }

    public override string GetGroom(string database, string schema, string table)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return @$"GROOM TABLE {tableCl} RECORDS ALL RECLAIM BACKUPSET NONE;
--GROOM TABLE {tableCl} VERSIONS;
--https://www.ibm.com/docs/en/netezza?topic=databases-groom-tables";

    }

    public override string GetGenerateStats(string database, string schema, string table)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return @$"GENERATE EXPRESS STATISTICS ON {tableCl};
--https://www.ibm.com/docs/en/netezza?topic=reference-generate-express-statistics";
    }
    public override string GetAddComment(string table, string database, string schema)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return @$"COMMENT ON TABLE {tableCl} IS 'some comment';";
    }

    protected Dictionary<string, Dictionary<string, Dictionary<string, ExternaTableCachedInfo>>> _exteralTableDictCache = new();
    public void ClearExternalTableCache()
    {
        _exteralTableDictCache.Clear();
    }

    protected Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> _distributionDictionary = new();
    public Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> DistributionDictionary => _distributionDictionary;

    protected Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> _oraganizeDictionary = new();
    public Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> OrganizeDictionary => _oraganizeDictionary;


    private string GetKeysSql(string databaseName)
    {
        return
            $"""
             SELECT 
                    --X.OBJID::INT AS OBJID,
                    X.SCHEMA
                    , X.RELATION
                    , X.CONSTRAINTNAME
                    , X.CONTYPE
                    , X.ATTNAME
                    --, X.PKOBJID
                    , X.PKDATABASE
                    , X.PKSCHEMA
                    , X.PKRELATION
                    , X.PKATTNAME
                	, X.UPDT_TYPE
                	, X.DEL_TYPE
                FROM 
                    {databaseName}.._V_RELATION_KEYDATA X
                WHERE 
                    X.OBJID NOT IN (4,5)
                    AND X.DATABASE = '{databaseName}'
                ORDER BY
                    X.SCHEMA, X.RELATION, X.CONSEQ
            """;
    }

    protected readonly Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, NetezzaKeyItem>>>> keysDictionary = new();
    public Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, NetezzaKeyItem>>>> KeysDictionary => keysDictionary;

    public void FillKeysInfoForDatabase(string databaseName, DbConnection? dbConnection = null)
    {
        lock (_lock2)
        {
            keysDictionary[databaseName] = new();
        }

        var databaseDic = keysDictionary[databaseName];

        var con = dbConnection ?? Connection;
        ChangeDatabaseSpecial(con, databaseName);
        if (con is not null && con.State != System.Data.ConnectionState.Open)
        {
            con.Open();
        }
        var cmd = CreateCommandFromConnection(con);
        cmd.CommandText = GetKeysSql(databaseName);

        var rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            //object schemaObj = rdr.GetValue(0);
            //string schema = schemaObj is null ? "ADMIN" : schemaObj.ToString();

            string schema = rdr.GetString(0);

            string tabName = rdr.GetString(1);
            string keyName = rdr.GetString(2);
            char keyType = rdr.GetChar(3);
            string attName = rdr.GetString(4);

            string PKDATABASE = rdr.GetString(5);
            string PKSCHEMA = rdr.GetString(6);
            string PKRELATION = rdr.GetString(7);
            string PKATTNAME = rdr.GetString(8);
            string UPDT_TYPE = (rdr.GetValue(9) as string) ?? "NO ACTION";
            string DEL_TYPE = (rdr.GetValue(10) as string) ?? "NO ACTION";


            if (!databaseDic.TryGetValue(schema, out var databaseDictLevel1))
            {
                databaseDictLevel1 = new();
                databaseDic[schema] = databaseDictLevel1;
            }

            if (!databaseDictLevel1.TryGetValue(tabName, out var databaseDictLevel2))
            {
                databaseDictLevel2 = new();
                databaseDictLevel1[tabName] = databaseDictLevel2;
            }

            if (!databaseDictLevel2.TryGetValue(keyName, out var databaseDictLevel3))
            {
                databaseDictLevel3 = new NetezzaKeyItem(keyType, PKDATABASE, PKSCHEMA, PKRELATION, new List<(string colName, string referencedPkColName)>(), UPDT_TYPE, DEL_TYPE);
                databaseDictLevel2[keyName] = databaseDictLevel3;
            }

            databaseDictLevel3.columnList.Add((attName, PKATTNAME));
        }
    }

    public async Task FillKeysInfoForDatabaseAsync(string databaseName, DbConnection? dbConnection = null)
    {
        await Task.Run(() => FillKeysInfoForDatabase(databaseName, dbConnection));
    }

    public void FillDistInfoForDatabase(string databaseName, DbConnection? dbConnection = null)
    {
        lock (_lock2)
        {
            DistributionDictionary[databaseName] = new();
        }
        var databaseDic = DistributionDictionary[databaseName];

        var con = dbConnection ?? Connection;
        ChangeDatabaseSpecial(con, databaseName);
        if (con is not null && con.State != System.Data.ConnectionState.Open)
        {
            con.Open();
        }

        using var cmd = CreateCommandFromConnection(con);
        cmd.CommandText = GetDistributeSql(databaseName);

        var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            string schema = rdr.GetString(0);
            string tabName = rdr.GetString(1);

            if (!databaseDic.TryGetValue(schema, out Dictionary<string, List<string>>? databaseDictLevel1))
            {
                databaseDictLevel1 = new();
                databaseDic[schema] = databaseDictLevel1;
            }

            if (!databaseDictLevel1.TryGetValue(tabName, out List<string>? databaseDictLevel2))
            {
                databaseDictLevel2 = new();
                databaseDictLevel1[tabName] = databaseDictLevel2;
            }

            databaseDictLevel2.Add(rdr.GetString(3));
        }


        lock (_lock2)
        {
            OrganizeDictionary[databaseName] = new();
        }
        databaseDic = OrganizeDictionary[databaseName];

        using var cmd2 = CreateCommandFromConnection(con);
        cmd2.CommandText = GetOrganizeSql(databaseName);

        var rdr2 = cmd2.ExecuteReader();
        while (rdr2.Read())
        {
            string schema = rdr2.GetString(0);
            string tabName = rdr2.GetString(1);

            if (!databaseDic.TryGetValue(schema, out Dictionary<string, List<string>>? databaseDictLevel1))
            {
                databaseDictLevel1 = new();
                databaseDic[schema] = databaseDictLevel1;
            }

            if (!databaseDictLevel1.TryGetValue(tabName, out List<string>? databaseDictLevel2))
            {
                databaseDictLevel2 = new();
                databaseDictLevel1[tabName] = databaseDictLevel2;
            }

            databaseDictLevel2.Add(rdr2.GetString(3));
        }
    }

    public override async ValueTask GetCreateExternalTextStringBuilder(StringBuilder sb, string database, string schema, string tableName)
    {
        var (cleanDatabaseName, cleanSchema, cleanTableName) = GetCleanedNames(database, schema, tableName);

        sb.AppendLine($"CREATE EXTERNAL TABLE {cleanDatabaseName}.{cleanSchema}.{cleanTableName}");

        var columns = GetColumns(database, schema, tableName, "");
        sb.AppendLine(
            $"""
            (
                {string.Join($",{Environment.NewLine}", columns.Select(o => $"    {o.Name} {o.FullTypeName}"))}
                )
            """);
        sb.AppendLine("USING");
        sb.AppendLine("(");
        if (!_exteralTableDictCache.ContainsKey(database))
        {
            await CacheAllObjects(new TypeInDatabaseEnum[] { TypeInDatabaseEnum.ExternalTable }, database);
        }
        if (_exteralTableDictCache.TryGetValue(database, out var d1) && d1.TryGetValue(schema, out var d2) && d2.TryGetValue(tableName, out var d3))
        {
            if (d3.DATAOBJECT is not null)
            {
                sb.AppendLine($"    DATAOBJECT('{d3.DATAOBJECT}')");
            }
            if (d3.DELIMITER is not null)
            {
                sb.AppendLine($"    DELIMITER '{d3.DELIMITER}'");
            }
            if (d3.ENCODING is not null)
            {
                sb.AppendLine($"    ENCODING '{d3.ENCODING}'");
            }
            if (d3.TIMESTYLE is not null)
            {
                sb.AppendLine($"    TIMESTYLE '{d3.TIMESTYLE}'");
            }
            if (d3.REMOTESOURCE is not null)
            {
                sb.AppendLine($"    REMOTESOURCE '{d3.REMOTESOURCE}'");
            }
            if (d3.MAXERRORS is not null)
            {
                sb.AppendLine($"    MAXERRORS {d3.MAXERRORS}");
            }
            if (d3.ESCAPECHAR is not null)
            {
                sb.AppendLine($"    ESCAPECHAR '{d3.ESCAPECHAR}'");
            }
            if (d3.DECIMALDELIM is not null)
            {
                sb.AppendLine($"    DECIMALDELIM '{d3.DECIMALDELIM}'");
            }
            if (d3.LOGDIR is not null)
            {
                sb.AppendLine($"    LOGDIR '{d3.LOGDIR}'");
            }
            if (d3.QUOTEDVALUE is not null)
            {
                sb.AppendLine($"    QUOTEDVALUE '{d3.QUOTEDVALUE}'");
            }
            if (d3.NULLVALUE is not null)
            {
                sb.AppendLine($"    NULLVALUE '{d3.NULLVALUE}'");
            }
            if (d3.CRINSTRING is not null)
            {
                sb.AppendLine($"    CRINSTRING {d3.CRINSTRING}");
            }
            if (d3.TRUNCSTRING is not null)
            {
                sb.AppendLine($"    TRUNCSTRING {d3.TRUNCSTRING}");
            }
            if (d3.CTRLCHARS is not null)
            {
                sb.AppendLine($"    CTRLCHARS {d3.CTRLCHARS}");
            }
            if (d3.IGNOREZERO is not null)
            {
                sb.AppendLine($"    IGNOREZERO {d3.IGNOREZERO}");
            }
            if (d3.TIMEEXTRAZEROS is not null)
            {
                sb.AppendLine($"    TIMEEXTRAZEROS {d3.TIMEEXTRAZEROS}");
            }
            if (d3.Y2BASE is not null)
            {
                sb.AppendLine($"    Y2BASE {d3.Y2BASE}");
            }
            if (d3.FILLRECORD is not null)
            {
                sb.AppendLine($"    FILLRECORD {d3.FILLRECORD}");
            }
            if (d3.COMPRESS is not null)
            {
                sb.AppendLine($"    COMPRESS {d3.COMPRESS}");
            }
            if (d3.INCLUDEHEADER is not null)
            {
                sb.AppendLine($"    INCLUDEHEADER {d3.INCLUDEHEADER}");
            }
            if (d3.LFINSTRING is not null)
            {
                sb.AppendLine($"    LFINSTRING {d3.LFINSTRING}");
            }
            if (d3.DATESTYLE is not null)
            {
                sb.AppendLine($"    DATESTYLE '{d3.DATESTYLE}'");
            }
            if (d3.DATEDELIM is not null)
            {
                sb.AppendLine($"    DATEDELIM '{d3.DATEDELIM}'");
            }
            if (d3.TIMEDELIM is not null)
            {
                sb.AppendLine($"    TIMEDELIM '{d3.TIMEDELIM}'");
            }
            if (d3.BOOLSTYLE is not null)
            {
                sb.AppendLine($"    BOOLSTYLE '{d3.BOOLSTYLE}'");
            }
            if (d3.FORMAT is not null)
            {
                sb.AppendLine($"    FORMAT '{d3.FORMAT}'");
            }
            if (d3.SOCKETBUFSIZE is not null)
            {
                sb.AppendLine($"    SOCKETBUFSIZE {d3.SOCKETBUFSIZE}");
            }
            if (d3.RECORDDELIM is not null)
            {
                sb.AppendLine($"    RECORDDELIM '{d3.RECORDDELIM}'");
            }
            if (d3.MAXROWS is not null)
            {
                sb.AppendLine($"    MAXROWS {d3.MAXROWS}");
            }
            if (d3.REQUIREQUOTES is not null)
            {
                sb.AppendLine($"    REQUIREQUOTES {d3.REQUIREQUOTES}");
            }
            if (d3.RECORDLENGTH is not null)
            {
                sb.AppendLine($"    RECORDLENGTH {d3.RECORDLENGTH}");
            }
            if (d3.DATETIMEDELIM is not null)
            {
                sb.AppendLine($"    DATETIMEDELIM '{d3.DATETIMEDELIM}'");
            }
            if (d3.REJECTFILE is not null)
            {
                sb.AppendLine($"    REJECTFILE '{d3.REJECTFILE}'");
            }
        }
        sb.AppendLine(");");
    }

    public override string GetCheckDistributeText(string database, string schema, string tableName)
    {
        var (cleanDatabaseName, cleanSchema, cleanTableName) = GetCleanedNames(database, schema, tableName);

        string distQ1 =
        $"""
        SET SHOW_DELETED_RECORDS = 1;
            SELECT 
                DATASLICEID, COUNT(1), COUNT(NULLIF(DELETEXID,0)) 
            FROM 
                {cleanDatabaseName}.{cleanSchema}.{cleanTableName} 
            GROUP BY 
                DATASLICEID 
            ORDER BY 
                COUNT(1) DESC;
            SET SHOW_DELETED_RECORDS = 0;
        """;

        string distQ2 =
        $"""
        SELECT
                OBJID::BIGINT AS OBJID
                , SKEW::DOUBLE
                , CREATEDATE::DATETIME AS CREATEDATE
                , ALLOCATED_BYTES::BIGINT AS ALLOCATED_BYTES
                , USED_BYTES::BIGINT as USED_BYTES
                , ALLOCATED_BLOCKS
                , USED_BLOCKS
                , BLOCK_SIZE
                , USED_MIN
                , USED_MAX
                , USED_AVG
            FROM
                {cleanDatabaseName}.{cleanSchema}._V_TABLE_STORAGE_STAT
            WHERE
                UPPER(OBJTYPE) = 'TABLE' 
                AND UPPER(TABLENAME) = '{tableName.ToUpper()}'
            ORDER BY
                TABLENAME, OWNER;
        """;

        return distQ1 + Environment.NewLine + Environment.NewLine + distQ2;
    }

    public override bool IsTypeInDatabaseSupported(TypeInDatabaseEnum tpe)
    {
        return true;
    }
    public override async ValueTask GetCreateProcedureTextStringBuilder(StringBuilder sb, string database, string schema, string procName, bool forceFreshCode = false)
    {
        if (!_procedureDictCache.ContainsKey(database))
        {
            await CacheAllObjects([TypeInDatabaseEnum.Procedure], database);
        }
        else if (forceFreshCode)
        {
            await CacheAllObjects([TypeInDatabaseEnum.Procedure], database, procName);
        }

        if (_procedureDictCache.TryGetValue(database, out var d1) && d1.TryGetValue(schema, out var d2) &&
            d2.TryGetValue(procName, out var d3))
        {
            var (cleanDatabaseName, cleanSchema) = GetCleanedNames(database, schema);
            sb.AppendLine($"CREATE OR REPLACE PROCEDURE {cleanDatabaseName}.{cleanSchema}.{procName}");// {d3.Arguments}
            sb.AppendLine($"RETURNS {d3.Returns}");
            if (d3.ExecuteAsOwner)
            {
                sb.AppendLine("EXECUTE AS OWNER");
            }
            else
            {
                sb.AppendLine("EXECUTE AS CALLER");
            }
            sb.AppendLine("LANGUAGE NZPLSQL AS");
            sb.AppendLine("BEGIN_PROC");
            sb.AppendLine(d3.ProcedureSource);
            sb.AppendLine("END_PROC;");
            if (d3.Desc is not null)
            {
                string cmt = d3.Desc;
                cmt = CleanComment(cmt);
                sb.AppendLine($"COMMENT ON PROCEDURE {procName} IS '{cmt}';");
            }
        }
    }

    public override string GetCreateProcedurePatternText()
    {
        return
            """
            CREATE OR REPLACE PROCEDURE SAMPLE_PROC(TEXT) 
            RETURNS TEXT
            EXECUTE AS CALLER
            LANGUAGE NZPLSQL AS
            BEGIN_PROC
                DECLARE PARAM_ALIAS ALIAS FOR $1;
                DECLARE SID INTEGER;
                DECLARE RESULT TEXT;
            BEGIN
                SID := CURRENT_SID;
                RESULT := 'HELLO ' || PARAM_ALIAS || 'SID IS ' || SID;
                RETURN RESULT;


                EXCEPTION
                WHEN OTHERS THEN
                    ROLLBACK;
                    RAISE EXCEPTION  'Procedure failed: %', sqlerrm;
                    --RAISE NOTICE 'Caught error, continuing %', sqlerrm;
            END;
            END_PROC;
            """;
    }

    public string GetCreateFluidSample(string database, string schema, string tableName)
    {
        int i1 = tableName.IndexOf('(');
        tableName = tableName[..i1];
        var f = GetQuotedTwoOrTreePartName(database, schema, tableName);
        return $"SELECT * FROM TABLE WITH FINAL ({f}('', '', 'SELECT *  FROM SOME_TABLE'))";
    }


    protected string GetDistributeSql(string databaseName)
    {
        return
            $"""
            SELECT 
                    --OBJID,
                    SCHEMA
                    , TABLENAME
                    , DISTATTNUM
                    , ATTNAME
                FROM 
                    {databaseName}.._V_TABLE_DIST_MAP
                WHERE 
                    DATABASE = '{databaseName}'
                ORDER BY 
                    SCHEMA, TABLENAME, DISTSEQNO
            """;
    }

    protected string GetOrganizeSql(string databaseName)
    {
        return
            $"""
            SELECT 
                    SCHEMA
                    , TABLENAME
                    , ATTNUM
                    , ATTNAME
                FROM 
                    {databaseName}.._V_TABLE_ORGANIZE_COLUMN
                ORDER BY 
                    SCHEMA, TABLENAME, ORGSEQNO;
            """;
    }

    public override async ValueTask GetCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName, string? overrideTableName = null, string ?middleCode = null, string? endingCode = null, List<string>? distOverride = null)
    {
        var (cleanDatabaseName, cleanSchema, cleanTableName) = GetCleanedNames(database, schema, tableName);

        sb.AppendLine($"CREATE TABLE {cleanDatabaseName}.{cleanSchema}.{overrideTableName ?? cleanTableName} \n(");

        var columns = GetColumns(database, schema, tableName, "");
        sb.Append("    ");
        List<string> columnsString = new List<string>();
        foreach (var column in columns)
        {
            string cleanColumnName = QuoteNameIfNeeded(column.Name);

            //string txt = $"{cleanColumnName} {column.FullTypeName}{(column.ColumnNotNull ? " NOT NULL" : "")}";
            // NOT NULL IS PART OF FULL COLUMN TYPE
            string txt = $"{cleanColumnName} {column.FullTypeName}";
            if (!string.IsNullOrWhiteSpace(column.COLDEFAULT))
            {
                txt += $" DEFAULT {column.COLDEFAULT}";
            }
            columnsString.Add(txt);
        }
        sb.Append(string.Join(",\n    ", columnsString));

        if (!DistributionDictionary.ContainsKey(database))
        {
            await Task.Run(() => FillDistInfoForDatabase(database));
        }

        if (distOverride is null)
        {
            if (DistributionDictionary[database].TryGetValue(schema, out var dic1) && dic1.TryGetValue(tableName, out var distList))
            {
                var cleanDistList = string.Join(',', distList.Select(o => QuoteNameIfNeeded(o)));
                sb.AppendLine($"\n)\nDISTRIBUTE ON ({string.Join(", ", cleanDistList)})");
            }
            else
            {
                sb.AppendLine("\n)\nDISTRIBUTE ON RANDOM");
            }
        }
        else
        {
            if (distOverride.Count > 0)
            {
                sb.AppendLine('(' + String.Join(',', distOverride) + ")");
            }
            else
            {
                sb.AppendLine($"RANDOM");
            }
        }

        if (OrganizeDictionary[database].TryGetValue(schema, out var org1) && org1.TryGetValue(tableName, out var orgList))
        {
            var cleanOrganizeListList = string.Join(',', orgList.Select(o => QuoteNameIfNeeded(o)));
            sb.AppendLine($"ORGANIZE ON ({string.Join(", ", cleanOrganizeListList)})");
        }
        sb.AppendLine(";");
        sb.AppendLine();

        if (middleCode != null)
        {
            sb.AppendLine(middleCode);
        }

        if (!keysDictionary.ContainsKey(database))
        {
            await FillKeysInfoForDatabaseAsync(database);
        }

        if (keysDictionary.TryGetValue(database, out var dict1) && dict1.TryGetValue(schema, out var dict2) && dict2.TryGetValue(tableName, out var dict3))
        {
            foreach (var (keyName, kefInfo) in dict3)
            {
                string cleanKeyName = QuoteNameIfNeeded(keyName);

                var colList1 = kefInfo.columnList.Select(o => QuoteNameIfNeeded(o.colName));
                if (kefInfo.KeyType == 'f')
                {
                    var colList2 = kefInfo.columnList.Select(o => QuoteNameIfNeeded(o.referencedPkColName));
                    sb.AppendLine($"ALTER TABLE {cleanDatabaseName}.{cleanSchema}.{cleanTableName} ADD CONSTRAINT {cleanKeyName} {KeyNameFromChar(kefInfo.KeyType)} ({string.Join(", ", colList1)}) REFERENCES {kefInfo.PKDATABASE}.{kefInfo.PKSCHEMA}.{kefInfo.PKRELATION}({string.Join(", ", colList2)}) ON DELETE {kefInfo.DEL_TYPE} ON UPDATE {kefInfo.UPDT_TYPE};");
                }
                else if (kefInfo.KeyType == 'p')
                {
                    sb.AppendLine($"ALTER TABLE {cleanDatabaseName}.{cleanSchema}.{cleanTableName} ADD CONSTRAINT {cleanKeyName} {KeyNameFromChar(kefInfo.KeyType)} ({string.Join(", ", colList1)});");
                }
                else if (kefInfo.KeyType == 'u')
                {
                    sb.AppendLine($"ALTER TABLE {cleanDatabaseName}.{cleanSchema}.{cleanTableName} ADD CONSTRAINT {cleanKeyName} {KeyNameFromChar(kefInfo.KeyType)} ({string.Join(", ", colList1)});");
                }
            }
        }
        if (_databaseSchemaTable.TryGetValue(database, out var schamaItems)
            && schamaItems.TryGetValue(schema, out var items)
            && items.TryGetValue(tableName, out var tableItem))
        {
            var cmt = tableItem.Desc;
            if (cmt is not null)
            {
                if (cmt.Contains('\''))
                {
                    cmt = cmt.Replace("'", "''");
                }
                sb.AppendLine();
                sb.AppendLine($"COMMENT ON TABLE {cleanDatabaseName}.{cleanSchema}.{cleanTableName} IS '{cmt}';");
            }

            foreach (var column in columns)
            {
                if (column.Desc is not null)
                {
                    string colCmt = column.Desc;
                    if (colCmt.Contains('\''))
                    {
                        colCmt = colCmt.Replace("'", "''");
                    }
                    string cleanColumnName = QuoteNameIfNeeded(column.Name);

                    sb.AppendLine($"COMMENT ON COLUMN {cleanDatabaseName}.{cleanSchema}.{cleanTableName}.{cleanColumnName} IS '{colCmt}';");
                }
            }
        }

        if (endingCode != null)
        {
            sb.AppendLine(endingCode);
        }
    }

    public override async ValueTask GetReCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName)
    {
        string tempName = StringExtension.RandomSuffix("TMP_", 10);
        string tempName2 = StringExtension.RandomSuffix("TMP_", 10);

        var (cleanDatabaseName, cleanSchema, cleanTableName) = GetCleanedNames(database, schema, tableName);

        StringBuilder middleCodeSB = new();
        middleCodeSB.AppendLine($"INSERT INTO {cleanDatabaseName}.{cleanSchema}.{tempName} SELECT * FROM {cleanDatabaseName}.{cleanSchema}.{cleanTableName};");
        middleCodeSB.AppendLine($"ALTER TABLE {cleanDatabaseName}.{cleanSchema}.{tempName} SET PRIVILEGES TO {cleanDatabaseName}.{cleanSchema}.{cleanTableName};");
        middleCodeSB.AppendLine($"ALTER TABLE {cleanDatabaseName}.{cleanSchema}.{cleanTableName} RENAME TO {cleanDatabaseName}.{cleanSchema}.{tempName2};");
        middleCodeSB.AppendLine($"ALTER TABLE {cleanDatabaseName}.{cleanSchema}.{tempName} RENAME TO {cleanDatabaseName}.{cleanSchema}.{cleanTableName};");

        string owner = QuoteNameIfNeeded(_databaseSchemaTable[database][schema][tableName].Owner);
        middleCodeSB.AppendLine($"ALTER TABLE {cleanDatabaseName}.{cleanSchema}.{cleanTableName} OWNER TO {owner};");
        middleCodeSB.AppendLine($"DROP TABLE {cleanDatabaseName}.{cleanSchema}.{tempName2};");

        await GetCreateTableTextStringBuilder(sb, database, schema, tableName, tempName, middleCodeSB.ToString(), $"GENERATE EXPRESS STATISTICS ON {cleanDatabaseName}.{cleanSchema}.{cleanTableName};{Environment.NewLine}", distOverride: null);
    }

    public override async ValueTask GetCreateViewTextStringBuilder(StringBuilder sb, string database, string schema, string tableName)
    {
        var (cleanDatabaseName, cleanSchema, cleanTableName) = GetCleanedNames(database, schema, tableName);

        sb.AppendLine($"CREATE OR REPLACE VIEW {cleanDatabaseName}.{cleanSchema}.{cleanTableName} AS ");

        if (!_viewDictCache.ContainsKey(database))
        {
            await CacheAllObjects([TypeInDatabaseEnum.View], database);
        }
        if (_viewDictCache.TryGetValue(database, out var d1)
            && d1.TryGetValue(schema, out var d2)
            && d2.TryGetValue(tableName, out var d3)
            )
        {
            sb.AppendLine(d3.ViewSource);
        }
        if (_databaseSchemaTable.TryGetValue(database, out var schamaItems) && schamaItems.TryGetValue(schema, out var items)
            && items.TryGetValue(tableName, out var tableItem))
        {
            var cmt = tableItem.Desc;
            if (cmt is not null)
            {
                if (cmt.Contains('\''))
                {
                    cmt = cmt.Replace("'", "''");
                }
                sb.AppendLine();
                sb.AppendLine($"COMMENT ON VIEW {cleanDatabaseName}.{cleanSchema}.{cleanTableName} IS '{cmt}';");
            }
        }
    }

    protected virtual string DriverName =>  "dotnet";
    public override async Task DbSpecificImportPart(IDbImportJob importJob, string randName, Action<string>? progress, bool tableExists = false)
    {
        try
        {
            using var conn = GetConnection(Connection.Database, pooling: false);
            if (conn is not null)
            {
                conn.Open();
                await NetezzaImportHelper.NetezzaImportExecute(conn, this.TempDataDirectory, importJob, randName, progress, DriverName);
                conn.Close();
            }
        }
        catch (Exception ex)
        {
            progress?.Invoke($"[ERROR] {ex.Message}");
            randName = ex.Message;
        }
    }

    private Lock _lockForExternales = new Lock();
    public void ReadExternalTable(string database, DbDataReader rdr)
    {
        while (rdr.Read())
        {
            string schema = rdr.GetValue(0) as string;
            string extTableName = rdr.GetString(1);
            string sourceFileName = rdr.GetValue(2) as string;
            int id = rdr.GetInt32(3);
            string DELIM = rdr.GetValue(4) as string;

            string ENCODING = rdr.GetValue(5) as string;
            string TIMESTYLE = rdr.GetValue(6) as string;
            string REMOTESOURCE = rdr.GetValue(7) as string;
            Int64? SKIPROWS = rdr.GetValue(8) as Int64?;
            Int64? MAXERRORS = rdr.GetValue(9) as Int64?;
            string ESCAPECHAR = rdr.GetValue(10) as string;//string
            string LOGDIR = rdr.GetString(11);//string
            string DECIMALDELIM = rdr.GetValue(12) as string;//string
            string QUOTEDVALUE = rdr.GetValue(13) as string;//string
            string NULLVALUE = rdr.GetValue(14) as string;//string
            bool? CRINSTRING = rdr.GetValue(15) as bool?;//bool
            bool? TRUNCSTRING = rdr.GetValue(16) as bool?;//bool
            bool? CTRLCHARS = rdr.GetValue(17) as bool?;//bool
            bool? IGNOREZERO = rdr.GetValue(18) as bool?;//bool
            bool? TIMEEXTRAZEROS = rdr.GetValue(19) as bool?;//bool
            Int16? Y2BASE = rdr.GetValue(20) as Int16?;//int16
            bool? FILLRECORD = rdr.GetValue(21) as bool?;//bool
            string COMPRESS = rdr.GetValue(22) as string;//string
            bool? INCLUDEHEADER = rdr.GetValue(23) as bool?;//bool
            bool? LFINSTRING = rdr.GetValue(24) as bool?;//bool
            string DATESTYLE = rdr.GetValue(25) as string;//string
            string DATEDELIM = rdr.GetValue(26) as string;//string
            string TIMEDELIM = rdr.GetValue(27) as string;//string
            string BOOLSTYLE = rdr.GetValue(28) as string;//string
            string FORMAT = rdr.GetValue(29) as string;//string
            Int32? SOCKETBUFSIZE = rdr.GetValue(30) as Int32?;//int32
            string RECORDDELIM = rdr.GetString(31).Replace("\r", "\\r").Replace("\n", "\\n");//string
            Int64? MAXROWS = rdr.GetValue(32) as Int64?;//int64
            bool? REQUIREQUOTES = rdr.GetValue(33) as bool?;//bool
            string RECORDLENGTH = rdr.GetValue(34) as string;//string
            string DATETIMEDELIM = rdr.GetValue(35) as string;//string
            string REJECTFILE = rdr.GetValue(36) as string;//string

            lock (_lockForExternales)
            {
                ref var databaseItem = ref CollectionsMarshal.GetValueRefOrAddDefault(_exteralTableDictCache, database, out var _);
                databaseItem ??= new();
                ref var schemaItem = ref CollectionsMarshal.GetValueRefOrAddDefault(databaseItem!, schema, out var _);

                schemaItem ??= new();

                schemaItem[extTableName] = new ExternaTableCachedInfo()
                {
                    DATAOBJECT = sourceFileName,
                    DELIMITER = DELIM,
                    ENCODING = ENCODING,
                    TIMESTYLE = TIMESTYLE,
                    REMOTESOURCE = REMOTESOURCE,

                    SKIPROWS = SKIPROWS,
                    MAXERRORS = MAXERRORS,
                    ESCAPECHAR = ESCAPECHAR,
                    LOGDIR = LOGDIR,
                    DECIMALDELIM = DECIMALDELIM,
                    QUOTEDVALUE = QUOTEDVALUE,
                    NULLVALUE = NULLVALUE,
                    CRINSTRING = CRINSTRING,
                    TRUNCSTRING = TRUNCSTRING,
                    CTRLCHARS = CTRLCHARS,
                    IGNOREZERO = IGNOREZERO,
                    TIMEEXTRAZEROS = TIMEEXTRAZEROS,
                    Y2BASE = Y2BASE,
                    FILLRECORD = FILLRECORD,
                    COMPRESS = COMPRESS,
                    INCLUDEHEADER = INCLUDEHEADER,
                    LFINSTRING = LFINSTRING,
                    DATESTYLE = DATESTYLE,
                    DATEDELIM = DATEDELIM,
                    TIMEDELIM = TIMEDELIM,
                    BOOLSTYLE = BOOLSTYLE,
                    FORMAT = FORMAT,
                    SOCKETBUFSIZE = SOCKETBUFSIZE,
                    RECORDDELIM = RECORDDELIM,
                    MAXROWS = MAXROWS,
                    REQUIREQUOTES = REQUIREQUOTES,
                    RECORDLENGTH = RECORDLENGTH,
                    DATETIMEDELIM = DATETIMEDELIM,
                    REJECTFILE = REJECTFILE
                };
            }
        }
    }

    public string GetExternalDataObject(string database, string schema, string itemNameOrSignature)
    {
        if (_exteralTableDictCache.TryGetValue(database, out var t1) &&
            schema is not null && t1.TryGetValue(schema, out var t2) &&
            itemNameOrSignature is not null && t2.TryGetValue(itemNameOrSignature, out var finalItem))
        {
            return finalItem.DATAOBJECT;
        }
        return "";
    }


}
