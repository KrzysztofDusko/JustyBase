using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Text;


namespace MySqlPlugin;

public sealed class MySql : DatabaseService
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.MySql;

    public MySql(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
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

        var cb = new MySqlConnectionStringBuilder
        {
            Database = databaseName
        };
        int index = Ip.IndexOf(':');
        if (index != -1)
        {
            cb.Server = Ip[..index];
            cb.Port = uint.Parse(Ip[(index + 1)..]);
        }
        else
        {
            cb.Server = Ip;
            cb.Port = uint.Parse(Port);
        }

        cb.UserID = Username;
        cb.Password = Password;
        Connection = new MySqlConnection(cb.ConnectionString);

        return Connection;
    }

    public override ValueTask GetCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName, string? overrideTableName = null, string? middleCode = null, string? endingCode = null, List<string>? distOverride = null)
    {
        throw new NotImplementedException();
    }

    protected override List<(string, string)> GetDatabases()
    {
        var databases = new List<(string, string)>();
        using (var con = GetConnection(Database) as MySqlConnection)
        {
            con.Open();
            using var cmd = con.CreateCommand();
            databases.Add((con.Database, "def"));
        }
        return databases;
    }
    //https://dev.mysql.com/doc/refman/8.0/en/information-schema-general-table-reference.html
    protected override string? GetExternalTableSql(string database)
    {
        throw new NotImplementedException();
    }

    protected override string? GetProceduresSql(string database, string objectFilterName)
    {
        throw new NotImplementedException();
    }

    protected override string GetSqlOfColumns(string dbName)
    {
        throw new NotImplementedException();
    }

    protected override string GetSqlTablesAndOtherObjects(string dbName)
    {
        return
            """
            SELECT 
                -1
                , I.table_name
                , I.TABLE_COMMENT
                , I.table_schema
                , I.table_type
                , '' AS OWNER
                , CREATE_TIME AS CREATEDATATIME
            FROM information_schema.tables I
            """;
    }

    protected override string? GetSynonymSql(string database)
    {
        throw new NotImplementedException();
    }
}
