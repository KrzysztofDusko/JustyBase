using DuckDB.NET.Data;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using System.Data.Common;
using System.Text;

namespace DuckDBPlugin;
public sealed class DuckDB : DatabaseService
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.DuckDB;
    public DuckDB(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
    {
        DatabaseType = WHO_I_AM_CONST;
        AutoCompletDatabaseMode = CurrentAutoCompletDatabaseMode.SchemaTable
            | CurrentAutoCompletDatabaseMode.SchemaOptional
            | CurrentAutoCompletDatabaseMode.DatabaseAndSchemaOptional
            | CurrentAutoCompletDatabaseMode.DatabaseSchemaTable;
        preferDatabaseInCodes = false;
    }

    public override DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false)
    {
        databaseName ??= Database;
        if (string.IsNullOrEmpty(Ip))
        {
            Connection = new DuckDBConnection(@$"Data Source={databaseName}");
        }
        else
        {
            Connection = new DuckDBConnection(@$"Data Source={Ip}\{databaseName}.db");
        }

        return Connection;
    }

    protected override string GetSqlTablesAndOtherObjects(string dbName)
    {
        return
            """
            SELECT 
                coalesce(T.table_oid, V.view_oid)
                , I.table_name
                , I.TABLE_COMMENT
                , I.table_schema
                , I.table_type
                , '' AS OWNER
                , NULL AS CREATEDATATIME
            FROM information_schema.tables I
            LEFT JOIN duckdb_tables() T ON T.table_name = I.table_name
                AND T.schema_name = I.table_schema
                AND T.database_name = I.table_catalog
            LEFT JOIN duckdb_views() V ON V.view_name = I.table_name
                AND v.schema_name = I.table_schema
                AND v.database_name = I.table_catalog
            """;
    }

    protected override string? GetProceduresSql(string database, string objectFilterName)
    {
        return
        """
        SELECT 
            "main" AS SCHEMA_
            , "sql"
           ,"rootpage"
            , UPPER("type")
            , NULL AS LANGUAGE
        FROM 
            sqlite_schema
        WHERE 
            name NOT LIKE 'sqlite_%'
            and "type" not in ('view','table','index')
        """;
    }

    protected override string? GetViewsSql(string database, string objectFilterName)
    {
        return
        """
        select schema_name, view_name, sql from duckdb_views() where not internal
        """;
    }

    protected override List<(string, string)> GetDatabases()
    {
        var databases = new List<(string, string)>();
        var con = Connection;//GetConnection(Database, pooling:false, forSchema:true) as NZdotNETConnection;
        if (con.State != System.Data.ConnectionState.Open)
        {
            con.Open();
        }
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT database_name,database_oid, FROM DUCKDB_DATABASES()  WHERE NOT INTERNAL";
        var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            databases.Add((rdr.GetString(0), "main"));
        }

        return databases;
    }

    protected override string GetSqlOfColumns(string dbName)
    {
        return
            $"""
                SELECT 
                    table_oid AS OBJECT_ID
                    , column_name
                    , NULL AS DESCRIPTION
                    , data_type || 
                        COALESCE('(' ||character_maximum_length || ')', 
                        '(' || numeric_precision || ',' || numeric_scale || ')',
                        '') 
                        || CASE WHEN is_nullable THEN ' NOT NULL' ELSE '' END  
                    , is_nullable AS ATTNOTNULL
                    , column_default
                FROM duckdb_columns()
                ORDER BY table_oid, column_index
                """;
    }

    protected override string? GetExternalTableSql(string database)
    {
        throw new NotImplementedException();
    }
    protected override string? GetSynonymSql(string database)
    {
        throw new NotImplementedException();
    }
    public override async ValueTask GetCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName, string? overrideTableName = null, string? middleCode = null, string? endingCode = null, List<string>? distOverride = null)
    {
        await Task.Run(() =>
        {
            using var conn = GetConnection(database);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
            $"""
                SELECT sql FROM duckdb_tables()
                where database_name = 'duck'
                and schema_name = '{schema}'
                and table_name = '{tableName}'
            """;
            string? txt = cmd.ExecuteScalar() as string;
            sb.Append(txt);
        });
    }
}
