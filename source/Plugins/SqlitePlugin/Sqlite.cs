using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;

namespace SqlitePlugin;
public sealed class Sqlite : DatabaseService
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.Sqlite;
    public Sqlite(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
    {
        DatabaseType = WHO_I_AM_CONST;
        AutoCompletDatabaseMode = CurrentAutoCompletDatabaseMode.SchemaTable;
        preferDatabaseInCodes = false;
    }

    public override DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false)
    {
        databaseName ??= Database;
        Connection = new SQLiteConnection(@$"URI=file:{Ip}\{databaseName}; Journal Mode=Off");
        //Connection = new SqliteConnection(@$"Data Source={Ip}\{databaseName}");
        return Connection;
    }

    protected override string GetSqlTablesAndOtherObjects(string dbName)
    {
        return
        """
        SELECT 
            "rootpage"
            , "name"
            , NULL AS DESC_
            , "main" AS SCHEMA_
            , UPPER("type")
            , 'OWNER TO DO' AS OWNER
            , NULL
        FROM 
            sqlite_schema
        WHERE 
            name NOT LIKE 'sqlite_%'
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
        SELECT 
            "main" AS SCHEMA_
            , "name"
            , "sql"
           ,"rootpage"
            , UPPER("type")
        FROM 
            sqlite_schema
        WHERE 
            name NOT LIKE 'sqlite_%'
            and "type" = 'view'
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

        databases.Add((Database, "main"));
        return databases;
    }

    protected override string GetSqlOfColumns(string dbName)
    {
        return
        $"""
        SELECT 
                m."rootpage"
                , p."name"
                , null as DESC_
                , CASE WHEN NOT p."notnull" THEN p."type" ELSE p."type" || ' NOT NULL' END as FORMAT_TYPE
                , p."notnull"
                , p."dflt_value"

            FROM sqlite_master m
            left outer join pragma_table_info((m.name)) p
                 on m.name <> p.name
            WHERE 
                 "cid" is not null
            order by m.name, p."name", p."cid"
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
        await Task.Delay(1);
    }
}
