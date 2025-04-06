using CommunityToolkit.HighPerformance.Buffers;
using JustyBase.PluginDatabaseBase.Database;
using System.Data.Common;
using System.Text.RegularExpressions;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Contracts;
using NetezzaBase;
using JustyBase.NetezzaDriver;

namespace NetezzaDotnetPlugin;

public sealed class Netezza : NetezzaCommonClass, INetezza, INetezzaDotnet
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.NetezzaSQL;
    public Netezza(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
    {
        DatabaseType = WHO_I_AM_CONST;
    }

    protected override string DriverName => "dotnet";

    private static readonly StringPool _stringPoolForSchema = new StringPool();
    public static StringPool StringPoolForSchema => _stringPoolForSchema;

    /// <summary>
    /// saves RAM for big netezza schemas
    /// </summary>
    /// <param name="cmd"></param>
    protected override void ConfigureStringPoolForSchema(DbCommand cmd)
    {

    }


    private readonly Lock _lock = new Lock();
    public override DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false)
    {
        NzConnection? conn = null;
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = Database;
            }

            conn = new NzConnection(Username,Password,Ip, databaseName, int.Parse(Port));
            conn.NoticeReceived += Conn_NoticeReceived; ;
        }

        return conn;
    }

    private void Conn_NoticeReceived(object sender, NzConnection.NzNoticeEventArgs e)
    {
        DbMessageAction?.Invoke(e.Message);
    }

    private static readonly Regex _exceptAtAchar1 = new Regex(@"\^ found ""(?<found>.*)"" \(at char (?<charNum>[0-9]+)\) expecting");
    private static readonly Regex _wrongSet = new Regex(@"^ERROR: 'SET (?<found>.*)'");
    private static readonly Regex _atributeNotFoundRegex = new Regex(@"ERROR: Attribute '(?<found>.*)' not found");
    private static readonly Regex _exceptionIncorrectType = new Regex(@"^ERROR: DROP (TABLE|VIEW): object ""(?<found>.*)"", incorrect type\.$");
    private static readonly Regex _transformColumnType = new Regex(@"^ERROR: transformColumnType: error reading type '(?<found>.*)'$");
    private static readonly Regex _groomError = new Regex(@"^ERROR: GROOM VERSIONS must be run on (?<found>.*) before any other GROOM operation$");
    private static readonly Regex _repeatedError = new Regex(@"^ERROR: Attribute '(?<found>.*)' is repeated. Must have an appropriate alias\.$");
    private static readonly Regex _alreadyExistsError = new Regex(@"^ERROR: CREATE TABLE: object ""(?<found>.*)"" already exists\.$");
    private static readonly Regex _notExistsError = new Regex(@"^ERROR: relation does not exist (?<db>[^.]*)\.?(?<schema>[^.]*)\.?(?<found>.*)$");
    private static readonly Regex _functionError = new Regex(@"^ERROR: Function '(?<found>.*)\(.*\)' does not exist");
    private static readonly Regex _wrongOption = new Regex(@"^ERROR: Option '(?<found>.*)' is not recognized$");
    private static readonly Regex _manySameAliases = new Regex(@"^ERROR: Table name ""(?<found>.*)"" specified more than once$");
    private static readonly Regex _couldNotacquire = new Regex(@"^ERROR: DROP DATABASE: could not acquire lock for ""(?<found>.*)""$");
    private static readonly Regex _ambiguousError = new Regex(@"^ERROR: Column reference ""(?<found>.*)"" is ambiguous$");
    private static readonly Regex _groupError1 = new Regex(@"^ERROR: Attribute (?<found>.*) must be GROUPed or used in an aggregate function$");
    private static readonly Regex _groupError2 = new Regex(@"^ERROR: Attribute (?<table>[^\.]*)\.(?<found>.*) must be GROUPed or used in an aggregate function$");

    private List<Regex> _simpleRegexes;

    public override (int position, int length) HanleExceptions(ReadOnlySpan<char> sql, Exception exception)
    {
        _simpleRegexes ??=
        [
            _wrongSet, _atributeNotFoundRegex,_exceptionIncorrectType, _transformColumnType,
            _groomError, _repeatedError, _alreadyExistsError, _notExistsError, _functionError,
            _wrongOption, _manySameAliases, _couldNotacquire, _ambiguousError
        ];

        string? msg = exception.Message?.ToString();

        if (msg is not null)
        {
            foreach (var reg in _simpleRegexes)
            {
                if (reg.IsMatch(msg))
                {
                    var m = reg.Match(msg);
                    string val = m.Groups["found"].Value;
                    var nm = sql.IndexOf(val, StringComparison.OrdinalIgnoreCase);
                    if (nm >= 0)
                    {
                        return (nm, val.Length);
                    }
                    else
                    {
                        return (-1, -1);
                    }
                }
            }

            if (_exceptAtAchar1.IsMatch(msg))
            {
                var m = _exceptAtAchar1.Match(msg);
                string num = m.Groups["charNum"].Value;
                int number = int.Parse(num) - 1;

                int leadingWhiteNum = 0;
                while (sql[leadingWhiteNum] == '\r' || sql[leadingWhiteNum] == '\n' || sql[leadingWhiteNum] == ' ')
                {
                    leadingWhiteNum++;
                }

                string val = m.Groups["found"].Value;
                var nm = sql[(leadingWhiteNum + number)..].IndexOf(val, StringComparison.OrdinalIgnoreCase);
                if (nm >= 0)
                {
                    return (leadingWhiteNum + number + nm, val.Length);
                }
                else
                {
                    return (-1, -1);
                }
            }
            else if (_groupError1.IsMatch(msg))
            {
                var m = _groupError1.Match(msg);
                if (sql.Contains(m.Groups["found"].Value, StringComparison.OrdinalIgnoreCase))
                {
                    string val = m.Groups["found"].Value;
                    var nm = sql.IndexOf(val, StringComparison.OrdinalIgnoreCase);
                    if (nm >= 0)
                    {
                        return (nm, val.Length);
                    }
                    else
                    {
                        return (-1, -1);
                    }
                }
                else if (_groupError2.IsMatch(msg))
                {
                    m = _groupError2.Match(msg);
                    string val = m.Groups["found"].Value;
                    var nm = sql.IndexOf(val, StringComparison.OrdinalIgnoreCase);
                    if (nm >= 0)
                    {
                        return (nm, val.Length);
                    }
                    else
                    {
                        return (-1, -1);
                    }
                }
            }
        }

        return (-1, -1);
    }

    public async Task DropConnectionEmergencyModeAsync(DbConnection dbConnection)
    {
        if (dbConnection is NzConnection nzCon)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var conX = GetConnection(null, pooling: false);
                    int id = (int) nzCon.Pid !;
                    conX.Open();
                    var cmd = conX.CreateCommand();
                    cmd.CommandText = $"SELECT ID FROM _V_SESSION WHERE PID = {id}";
                    var res = cmd.ExecuteScalar();
                    if (res is int intRes)
                    {
                        cmd.CommandText = $"DROP SESSION {intRes}";
                        cmd.ExecuteNonQuery();
                    }
                    conX.Close();
                }
                catch (Exception) { }
            });
        }
    }
    public void OptimizeCommandBuffer(DbCommand cmd, bool noStringReturn = true)
    {
        //if (cmd is NZdotNETCommand nzConnamd)
        //{
        //    nzConnamd.UseBuffer = true;
        //    nzConnamd.NoStringReturn = noStringReturn;
        //}
    }

    public override IDatabaseRowReader GetDatabaseRowReader(DbDataReader reader)
    {
        if (reader is NzDataReader)
        {
            return new DatabaseRowReaderNetezzaDotnet(reader);
        }
        return new DatabaseRowReaderGeneral(reader);
    }
}
