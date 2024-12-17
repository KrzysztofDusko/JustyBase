using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using NetezzaBase;
using System.Data.Common;
using System.Data.Odbc;

namespace NetezzaOdbcPlugin;

//TODO
public sealed class NetezzaOdbc : NetezzaCommonClass, INetezza
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.NetezzaSQLOdbc;
    public const bool IsThatPluginFree = true;
    public NetezzaOdbc(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
    {
        DatabaseType = WHO_I_AM_CONST;
    }
    public static NetezzaOdbc FromOdbc(string connectionString, int timeout)
    {
        OdbcConnectionStringBuilder builder = new OdbcConnectionStringBuilder(connectionString);
        return new NetezzaOdbc(builder["username"] as string, builder["password"] as string, builder["port"] as string, 
            builder["servername"] as string, builder["database"] as string, timeout);
    }

    protected override string DriverName => "odbc";

    private readonly Lock _lock = new Lock();

    public override DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false)
    {
        OdbcConnection? conn = null;
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = Database;
            }
            
            OdbcConnectionStringBuilder builder = new OdbcConnectionStringBuilder();
            builder.Driver = "NetezzaSQL";
            builder["username"] = Username;
            builder["password"] = Password;
            builder["port"] = Port;
            builder["servername"] = Ip;
            builder["database"] = databaseName;
            builder["Connection Pooling"] = pooling;//?

            conn = new OdbcConnection(builder.ConnectionString);
            conn.InfoMessage += Conn_InfoMessage;
        }
        if (forSchema)
        {
            conn.ConnectionTimeout = CONNECTION_TIMEOUT + 1;
        }
        else
        {
            conn.ConnectionTimeout = CONNECTION_TIMEOUT;
        }
        return conn;
    }

    private void Conn_InfoMessage(object sender, OdbcInfoMessageEventArgs e)
    {
        DbMessageAction?.Invoke(e.Message);
    }
}
