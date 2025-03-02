using System.Data.Common;
using System.Text;
using JustyBase.Helpers.Importers;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using Oracle.ManagedDataAccess.Client;

namespace OraclePlugin;

public sealed class Oracle : DatabaseService
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.Oracle;
    public Oracle(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
    {
        DatabaseType = WHO_I_AM_CONST;
        AutoCompletDatabaseMode = CurrentAutoCompletDatabaseMode.SchemaTable;
        preferDatabaseInCodes = false;
    }

    public override DbConnection GetConnection(string? databaseName, bool pooling = false, bool forSchema = false)
    {
        OracleConnectionStringBuilder builder = new()
        {
            UserID = Username,
            Password = Password,
            ConnectionTimeout = CONNECTION_TIMEOUT,
            Pooling = pooling
        };
        databaseName = Database;

        string server = Ip;
        if (server.StartsWith("Tns:"))
        {
            if (OracleConfiguration.TnsAdmin != server[4..])
            {
                OracleConfiguration.TnsAdmin = server[4..];
            }
            if (OracleConfiguration.WalletLocation != OracleConfiguration.TnsAdmin)
            {
                OracleConfiguration.WalletLocation = OracleConfiguration.TnsAdmin;
            }

            builder.DataSource = databaseName;
            //OracleConfiguration.TnsAdmin = server[4..];
            //builder.WalletLocation = server[4..];
            //Connection = new OracleConnection(builder.ConnectionString);
            var conn = new OracleConnection(builder.ConnectionString);
            return conn;
            //return $"User Id={UserName(connectionName)};Password={Password(connectionName)};Data Source={DBname(connectionName)};Connection Timeout={CONNECTION_TIMEOUT};";
        }
        else if (server.StartsWith("TLS:"))
        {
            return new OracleConnection($"User Id={Username};Password={Password};Data Source={server[4..]};Connection Timeout={CONNECTION_TIMEOUT};");
        }
        else
        {
            builder.DataSource = $"{Ip}/{databaseName}";
            return new OracleConnection(builder.ConnectionString);
            //return $"User Id={UserName(connectionName)};Password={Password(connectionName)};Data Source={Server(connectionName)}/{DBname(connectionName)}";
        }
    }

    public override async Task DbSpecificImportPart(IDbImportJob importJob, string randName, Action<string>? progress, bool tableExists = false)
    {
        try
        {
            using var conn = GetConnection(Connection.Database, pooling: false);
            if (conn is OracleConnection oracleConnection)
            {
                conn.Open();
                await OracleImportHelper.OracleImportExecute(oracleConnection, importJob, randName, progress, tableExists);
                conn.Close();
            }
        }
        catch (Exception ex)
        {
            progress?.Invoke($"[ERROR] {ex.Message}");
        }
    }

    protected override string GetSqlTablesAndOtherObjects(string dbName)
    {
        return
        """
            SELECT 
                OBJECT_ID
                , OBJECT_NAME
                , NULL AS DESC_
                , OWNER AS SCHEMA_
                , OBJECT_TYPE
                , OWNER AS SCHEMA_
                , CREATED
            FROM 
                ALL_OBJECTS 
            --WHERE
            --    OBJECT_TYPE IN ('TABLE','VIEW','PROCEDURE','SYNONYM','SEQUENCE','FUNCTION','INDEX')
            ORDER BY 
                OWNER,OBJECT_TYPE, OBJECT_NAME
            """;
    }
    string version = "0.0.0.0.0";

    protected override string GetLimitClause(object rowsCnt)
    {
        return $"FETCH FIRST {rowsCnt} ROWS ONLY";
    }

    protected override List<(string, string)> GetDatabases()
    {
        var databases = new List<(string, string)>();
        using var con = GetConnection(Database, pooling: false, forSchema: true);
        con.Open();
        if (con is OracleConnection oracleConnection)
        {
            version = con.ServerVersion;
            databases.Add((oracleConnection.DatabaseName, "SCHEMA"));
        }
        con.Close();
        return databases;
    }

    protected override string GetSqlOfColumns(string dbName)
    {
        string DEFAULT_ON_NULL;
        if (string.Compare(version, "12") > 0)
        {
            DEFAULT_ON_NULL = "DEFAULT_ON_NULL";
        }
        else
        {
            DEFAULT_ON_NULL = "NULL AS DEFAULT_ON_NULL";
        }

        return
        $"""
            SELECT 
                OB.OBJECT_ID
                , COL.COLUMN_NAME
                , NULL AS DESC_
                , CASE WHEN NULLABLE = 'N' THEN DATA_TYPE ELSE DATA_TYPE || ' NOT NULL' END AS FORMAT_TYPE
                , CASE WHEN NULLABLE = 'N' THEN 1 ELSE 0 END AS ATTNOTNULL
                , {DEFAULT_ON_NULL}
            FROM 
                ALL_TAB_COLUMNS  COL
                JOIN ALL_OBJECTS OB ON COL.OWNER = OB.OWNER 
                    AND COL.TABLE_NAME = OB.OBJECT_NAME
            --WHERE 
            --    OB.OBJECT_TYPE IN ('TABLE','VIEW')
            ORDER BY 
                OB.OBJECT_ID, COLUMN_ID
            """;
    }


    //protected override string? getProcedureSql(string database, string schema, string procedureName)
    //{
    //    return 
    //        $"""
    //            SELECT TEXT FROM dba_source
    //            WHERE TYPE = 'PROCEDURE'
    //            AND OWNER = '{schema}'
    //            AND NAME = '{procedureName}'
    //        """;
    //}

    //TODO
    protected override string? GetProceduresSql(string database, string objectFilterName)
    {
        // CacheAllObjects source is splited line by line.. TODO
        //        SELECT LINE
        //*
        //FROM ALL_SOURCE
        //WHERE TYPE = 'PROCEDURE'
        //ORDER BY OWNER,NAME

        return
            $"""
            SELECT 
            OWNER, TEXT,-1,'fake source', cast(0 as SMALLINT),'fake','fake','fake','fake','fake','fake','fake', NULL AS LANGUAGE
            FROM ALL_SOURCE
            WHERE TYPE = 'PROCEDURE'
            ORDER BY OWNER,NAME
            """;
    }

    //TODO
    protected override string? GetViewsSql(string database, string objectFilterName)
    {
        return
            $"""
            SELECT OWNER, VIEW_NAME, TEXT_VC,'fake source', cast(0 as SMALLINT),'fake','fake','fake','fake','fake','fake','fake','fake' 
            FROM ALL_VIEWS
            ORDER BY OWNER,VIEW_NAME
            """;
    }

    public override async ValueTask GetCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName, string? overrideTableName = null, string? middleCode = null, string? endingCode = null, List<string>? distOverride = null)
    {
        var txt = await Task.Run(() => GetDDLOracle(schema, tableName, "TABLE"));
        sb.Append(txt);
    }
    public override async ValueTask GetCreateViewTextStringBuilder(StringBuilder sb, string database, string schema, string viewName)
    {
        var txt = await Task.Run(() => GetDDLOracle(schema, viewName, "VIEW"));

        sb.Append(
            $"""
            {txt}
            --SELECT * FROM ALL_VIEWS WHERE OWNER = '{schema}' AND VIEW_NAME = '{viewName}'
            """);
    }
    public override async ValueTask GetCreateProcedureTextStringBuilder(StringBuilder sb, string database, string schema, string procName, bool forceFreshCode = false)
    {
        var txt = await Task.Run(() => GetDDLOracle(schema, procName, "PROCEDURE"));

        sb.Append(
            $"""
            {txt}
            --SELECT TEXT,LINE FROM ALL_SOURCE 
            --WHERE 
            --    OWNER = '{schema}' 
            --    AND NAME = '{procName}' 
            --    AND TYPE = 'PROCEDURE'
            --ORDER BY LINE
            """);
    }

    private string GetDDLOracle(string schema, string tablename, string type)
    {
        string SQL = $"select dbms_metadata.get_ddl('{type}', '{tablename}', '{schema}') FROM DUAL";
        string? txt = "problem";
        try
        {
            using DbConnection conn = GetConnection(Database, false, true);
            conn.Open();
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = SQL;
            txt = cmd.ExecuteScalar() as string;
            conn.Close();
        }
        catch (Exception ex)
        {
            txt = ex.Message;
        }

        return txt ?? "";
    }

    protected override string? GetExternalTableSql(string database)
    {
        throw new NotImplementedException();
    }

    protected override string? GetSynonymSql(string database)
    {
        throw new NotImplementedException();
    }
}