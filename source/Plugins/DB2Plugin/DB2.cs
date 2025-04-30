using IBM.Data.Db2;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Models;
using JustyBase.PluginDatabaseBase.Database;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace DB2Plugin;

internal sealed class DB2 : DatabaseService
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.DB2;
    public DB2(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
    {
        DatabaseType = WHO_I_AM_CONST;
        AutoCompletDatabaseMode = CurrentAutoCompletDatabaseMode.SchemaTable;
        preferDatabaseInCodes = false;
    }

    public override DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false)
    {
        databaseName ??= Database;

        DB2ConnectionStringBuilder builder = new DB2ConnectionStringBuilder();
        builder.UserID = Username;
        builder.Password = Password;

        builder.Server = Ip;
        builder.Database = databaseName;
        builder.Connect_Timeout = CONNECTION_TIMEOUT;
        builder.Pooling = pooling;

        //Connection = new DB2Connection(builder.ConnectionString);
        var conn = new DB2Connection(builder.ConnectionString);
        conn.InfoMessage += DB2Connection_InfoMessage;

        return conn;
    }

    private void DB2Connection_InfoMessage(object sender, DB2InfoMessageEventArgs e)
    {
        List<string> arr = [];
        for (int i = 0; i < e.Errors.Count; i++)
        {
            arr.Add(e.Errors[i].Message);
        }
        DbMessageAction?.Invoke(string.Join(Environment.NewLine, arr));
    }

    protected override string GetSqlTablesAndOtherObjects(string dbName)
    {
        //SELECT TABNAME, TABSCHEMA, TABLEORG ,TBSPACE, COMPRESSION, keycolumns, keyindexid, keyunique, checkcount, PARTITION_MODE, REMARKS,PROPERTY FROM SYSCAT.TABLES
        //SELECT * FROM SYSIBM.SQLTABLES;
        return
        """
            
            SELECT 
                   CASE WHEN TYPE = 'PROCEDURE' THEN -1 ELSE ROW_NUMBER() OVER (ORDER BY SCHEMA, TYPE, NAME)::INT END AS NR
                   , NAME
                   , REMARKS
                   ,SCHEMA
                   , TYPE
                   , TRIM(OWNER) AS OWNER
                   , CREATE_TIME
             FROM 
            (
                SELECT 
                   TRIM(T.TABNAME) AS NAME
                   , T.REMARKS
                   , TRIM(T.TABSCHEMA) AS SCHEMA
                   , CASE 
                        WHEN T.TYPE = 'T' THEN 'TABLE'
                        WHEN T.TYPE = 'U' THEN 'TYPED TABLE'
                        WHEN T.TYPE = 'H' THEN 'HIERARCHY TABLE'
                        WHEN T.TYPE = 'L' THEN 'DETACHED TABLE'
                        WHEN T.TYPE = 'S' THEN 'MATERIALIZED QUERY TABLE'
                        WHEN T.TYPE = 'V' THEN 'VIEW'
                        WHEN T.TYPE = 'W' THEN 'TYPED VIEW'
                        WHEN T.TYPE = 'N' THEN 'NICKNAME'
                        WHEN T.TYPE = 'A' THEN 'ALIAS'
                        WHEN T.TYPE = 'I' THEN 'INDEX'
                        
                        ELSE 'OTHER TYPE'
                   END AS TYPE
                   , OWNER
                   , CREATE_TIME
                FROM 
                    SYSCAT.TABLES T

            UNION ALL 

            select 
                   TRIM(R.ROUTINENAME) || ' - ' ||R.PARM_COUNT::VARCHAR(5) AS NAME
                   , R.REMARKS
                   , TRIM(R.ROUTINESCHEMA) AS SCHEMA
                      , CASE 
                        WHEN R.ROUTINETYPE = 'F' THEN 'FUNCTION'
                        WHEN R.ROUTINETYPE = 'M' THEN 'METHOD'      
                        WHEN R.ROUTINETYPE = 'P' THEN 'PROCEDURE'      
                        ELSE 'OTHER TYPE'
                   END AS TYPE
                   , OWNER
                   , CREATE_TIME
            from 
            SYSCAT.ROUTINES R

            UNION ALL

            SELECT 
               TRIM(S.SEQNAME) AS NAME
               , S.REMARKS
               , TRIM(S.SEQNAME) AS SCHEMA
                                  , CASE 
                                    WHEN S.SEQTYPE = 'A' THEN 'ALIAS'
                                    WHEN S.SEQTYPE = 'I' THEN 'IDENTITY SEQUENCE'
                                    WHEN S.SEQTYPE = 'S' THEN 'SEQUENCE'                        
                                    ELSE 'OTHER TYPE'
                               END AS TYPE
                , OWNER
                , CREATE_TIME
            FROM SYSCAT.SEQUENCES S
            WHERE S.SEQTYPE IN ('A','I')
            ) X 
            ORDER BY SCHEMA, TYPE, NAME
            WITH UR
            """;
    }

    private readonly Dictionary<int, (string PROVIDER_TYPE_NAME, string CREATE_PARAMS, string SQL_TYPE_NAME)> dataTypes = new();

    protected override List<(string, string)> GetDatabases()
    {
        var databases = new List<(string, string)>();
        using (var con = GetConnection(Database))
        {
            con.Open();
            if (dataTypes.Count == 0)
            {
                var dt = con.GetSchema("DataTypes");

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var currentRow = dt.Rows[i];
                    var providerIdObj = currentRow["PROVIDER_TYPE"];
                    string typeName = currentRow["PROVIDER_TYPE_NAME"]?.ToString();
                    string createParams = currentRow["CREATE_PARAMS"]?.ToString();
                    string sqlTypeName = currentRow["SQL_TYPE_NAME"]?.ToString();
                    if (providerIdObj is int providerId && typeName is string && sqlTypeName is not null && sqlTypeName is not null)
                    {
                        dataTypes[providerId] = (typeName, createParams, sqlTypeName);
                    }
                }
            }

            databases.Add((con.Database, "SCHEMA"));
        }
        return databases;
    }

    protected override string GetSqlOfColumns(string dbName)
    {
        return
            """
                SELECT 
                    X.NR
                    , C.COLNAME
                    , C.REMARKS
                , CASE WHEN UPPER(TYPENAME) IN('BINARY','VARBINARY','BLOB', 'CLOB','DBCLOB','CHARACTER', 'VARCHAR','GRAPHIC','VARGRAPHIC') then TYPENAME || '(' || LENGTH ||')'
                       WHEN UPPER(TYPENAME) IN('DECIMAL') then TYPENAME || '(' || LENGTH || ',' || SCALE || ')'
                         ELSE TYPENAME
                         END 
                    || CASE WHEN C.NULLS = 'Y' THEN '' ELSE ' NOT NULL' END
                    || CASE WHEN C.DEFAULT IS NOT NULL THEN ' DEFAULT ' || DEFAULT ELSE '' END
                    AS TYPENAME
                    , CASE WHEN C.NULLS = 'N' THEN 1 ELSE 0 END::INT
                    , C.DEFAULT
                FROM 
                    SYSCAT.COLUMNS C
                    JOIN
                    (
                            SELECT 
                                   CASE WHEN  TYPE = 'PROCEDURE' THEN -1 ELSE ROW_NUMBER() OVER (ORDER BY SCHEMA, TYPE, NAME)::INT END AS NR
                                   , OBJDB
                                   , NAME
                                   , REMARKS
                                   ,SCHEMA
                                   , TYPE
                             FROM 
                            (
                                SELECT 
                                   NULL AS OBJDB
                                   , TRIM(T.TABNAME) AS NAME
                                   , T.REMARKS
                                   , TRIM(T.TABSCHEMA) AS SCHEMA
                                   , CASE 
                                        WHEN T.TYPE = 'T' THEN 'TABLE'
                                        WHEN T.TYPE = 'U' THEN 'TYPED TABLE'
                                        WHEN T.TYPE = 'H' THEN 'HIERARCHY TABLE'
                                        WHEN T.TYPE = 'L' THEN 'DETACHED TABLE'
                                        WHEN T.TYPE = 'S' THEN 'MATERIALIZED QUERY TABLE'
                                        WHEN T.TYPE = 'V' THEN 'VIEW'
                                        WHEN T.TYPE = 'W' THEN 'TYPED VIEW'
                                        WHEN T.TYPE = 'N' THEN 'NICKNAME'
                                        WHEN T.TYPE = 'A' THEN 'ALIAS'
                                        WHEN T.TYPE = 'I' THEN 'INDEX'
                        
                                        ELSE 'OTHER TYPE'
                                   END AS TYPE
                                FROM 
                                    SYSCAT.TABLES T
                                UNION ALL 


                            select 
                                   NULL AS OBJDB
                                   , TRIM(R.SPECIFICNAME) AS NAME
                                   , R.REMARKS
                                   , TRIM(R.ROUTINESCHEMA) AS SCHEMA
                                      , CASE 
                                        WHEN R.ROUTINETYPE = 'F' THEN 'FUNCTION'
                                        WHEN R.ROUTINETYPE = 'M' THEN 'METHOD'      
                                        WHEN R.ROUTINETYPE = 'P' THEN 'PROCEDURE'      
                                        ELSE 'OTHER TYPE'
                                   END AS TYPE

                            from 
                            SYSCAT.ROUTINES R

                            UNION ALL
                            SELECT 
                               NULL AS OBJDB
                               , TRIM(S.SEQNAME) AS NAME
                               , S.REMARKS
                               , TRIM(S.SEQNAME) AS SCHEMA
                                                  , CASE 
                                                    WHEN S.SEQTYPE = 'A' THEN 'ALIAS'
                                                    WHEN S.SEQTYPE = 'I' THEN 'IDENTITY SEQUENCE'
                                                    WHEN S.SEQTYPE = 'S' THEN 'SEQUENCE'                        
                                                    ELSE 'OTHER TYPE'
                                               END AS TYPE
                            FROM SYSCAT.SEQUENCES S
                            WHERE S.SEQTYPE IN ('A','I')
                            ) X 
                            ORDER BY SCHEMA, TYPE, NAME


                    ) X ON X.SCHEMA = C.TABSCHEMA AND X.NAME = C.TABNAME AND X.NR !=-1
                WHERE 
                    C.RANDDISTKEY = 'N' -- HIDDEN, GENERATED
                ORDER BY 
                    C.TABSCHEMA, C.TABNAME, C.COLNO
                WITH UR;
                """;
    }

    protected override string? GetProceduresSql(string database, string objectFilterName)
    {
        return
            $"""
                SELECT TRIM(PROCSCHEMA), TEXT,-1
                    , '' AS RETURNS
                    , FALSE AS EXECUTEDASOWNER
                    , REMARKS
                    , TRIM(PROCNAME) || ' - ' ||PARM_COUNT::VARCHAR(5) AS PROCEDURESIGNATURE
                    , '' AS ARGUMENTS
                    , NULL AS LANGUAGE
                    --, TRIM(SPECIFICNAME)
                FROM SYSCAT.PROCEDURES
                    ORDER BY PROCSCHEMA,PROCNAME;
                """;
    }

    protected override string? GetViewsSql(string database, string objectFilterName)
    {
        return
            $"""
                SELECT TRIM(VIEWSCHEMA),TRIM(VIEWNAME), TEXT FROM SYSCAT.VIEWS
                    ORDER BY VIEWSCHEMA,VIEWNAME;
                """;
    }

    public override bool IsTypeInDatabaseSupported(TypeInDatabaseEnum typeInDatabase)
    {
        return typeInDatabase != TypeInDatabaseEnum.ExternalTable;
    }

    protected override string? GetSynonymSql(string database)
    {
        return @$"SELECT TRIM(A.TABSCHEMA) AS SCHEMA, TRIM(A.TABNAME) AS NAME, A.REMOTE_TABLE, A.SERVERNAME,NVL(A.REMOTE_SCHEMA,'ADMIN') AS REMOTE_SCHEMA, A.REMARKS FROM SYSCAT.NICKNAMES A;";
    }

    public override async ValueTask<List<PorcedureCachedInfo>> GetProceduresSignaturesFromName(string database, string schema, string procName)
    {
        List<PorcedureCachedInfo> ll = [];
        if (!_procedureDictCache.ContainsKey(database))
        {
            await CacheAllObjects([TypeInDatabaseEnum.Procedure], database);
        }

        if (_procedureDictCache.TryGetValue(database, out var d1) && d1.TryGetValue(schema, out var d2) &&
            d2.TryGetValue(procName, out var d3))
        {
            ll.Add(d3);
        }

        return ll;
    }

    public override async ValueTask GetCreateViewTextStringBuilder(StringBuilder sb, string database, string schema, string tableName)
    {
        if (!_viewDictCache.ContainsKey(database))
        {
            await CacheAllObjects(new TypeInDatabaseEnum[] { TypeInDatabaseEnum.View }, database);
        }
        if (_viewDictCache.TryGetValue(database, out var d1)
            && d1.TryGetValue(schema, out var d2)
            && d2.TryGetValue(tableName, out var d3)
            )
        {
            sb.Append(d3.ViewSource);
            return;
        }
        sb.Append("no view text avaiable");
    }

    public override async ValueTask GetCreateSynonymTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string synonymName)
    {
        if (!_synonymTableDictCache.ContainsKey(database))
        {
            await CacheAllObjects([TypeInDatabaseEnum.Synonym], database);
        }

        if (_synonymTableDictCache.TryGetValue(database, out var d1) && d1.TryGetValue(schema, out var d2) &&
            d2.TryGetValue(synonymName, out var d3))
        {
            var f = GetQuotedTwoOrTreePartName(database, schema, synonymName);
            var g = GetQuotedTwoOrTreePartName(d3.RefObjNamePart1, d3.RefObjNamePart2, d3.RefObjNamePart3, force: true);
            stringBuilder.Append($"CREATE NICKNAME {f} FOR {g}");
            return;
        }
        stringBuilder.Append($"PROBLEM ! {database}.{schema}.{synonymName}");
    }

    public override async ValueTask GetCreateProcedureTextStringBuilder(StringBuilder sb, string database, string schema, string procName, bool forceFreshCode = false)
    {
        //var (cleanSchema, cleanTableName) = GetCleanedNames(schema, procName);

        List<PorcedureCachedInfo> list = await GetProceduresSignaturesFromName(database, schema, procName);
        if (list.Count == 0)
        {
            sb.Append(" NO SOURCE FOUND");
            return;
        }

        sb.Append(list[0].ProcedureSource);
    }


    public override async ValueTask GetCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName, string? overrideTableName = null, string? middleCode = null, string? endingCode = null, List<string>? distOverride = null)
    {
        string SQL = $"SELECT TABLEORG ,TBSPACE, COMPRESSION, keycolumns, keyindexid, keyunique, checkcount, PARTITION_MODE, REMARKS,PROPERTY FROM SYSCAT.TABLES WHERE TABNAME = '{tableName}' AND TABSCHEMA = '{schema}';";
        //0         1       2           3           4           5           6           7               8
        string? organize = null;
        string? tbSpace = null;
        string? compression = null;
        short keycolumns = 0;
        string? remarks = null;
        string pk = "????";
        string? pkComment = null;
        List<string> pkCols = [];
        string pkEnforced = "";
        string pkTrusted = "";
        string distributeInfo = "-- DISTIBUTE BY ... ";
        string partitionInfo = "";
        string PARTITION_MODE = "";//7
        string PROPERTY = ""; //9

        Dictionary<string, List<string>> fkCols = [];
        Dictionary<string, string?> fkComments = [];
        Dictionary<string, string> fkEnforced = [];
        Dictionary<string, string> fkTrusted = [];

        Dictionary<string, string> constraints = [];
        Dictionary<string, string> checks = [];
        Dictionary<string, string> checksEnforced = [];
        Dictionary<string, string> checksTrusted = [];

        List<string> indexes = [];
        List<string> triggers = [];

        var (clearSchema, clearTableName) = GetCleanedNames(schema, tableName);

        await Task.Run(() =>
        {
            using (var conn = GetConnection(database))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    var rdr = cmd.ExecuteReader();
                    rdr.Read();
                    organize = rdr.GetString(0) == "R" ? "ROW" : "COLUMN";
                    var tmp = rdr.GetValue(1);
                    if (tmp != DBNull.Value && tmp is string str)
                    {
                        tbSpace = str;
                    }
                    compression = rdr.GetString(2) == "N" ? "NO" : "YES";
                    keycolumns = rdr.GetInt16(3);
                    if (rdr.GetValue(8) != DBNull.Value)
                    {
                        remarks = rdr.GetString(8);
                    }
                    PARTITION_MODE = rdr.GetString(7).Trim();
                    PROPERTY = rdr.GetString(9).Trim();
                    if (string.IsNullOrWhiteSpace(PARTITION_MODE))
                    {
                        distributeInfo = "";
                    }
                    else if (PARTITION_MODE == "H" && PROPERTY == "Y")
                    {
                        distributeInfo = $"DISTRIBUTE BY RANDOM";
                    }
                    else if (PARTITION_MODE == "H")
                    {
                        List<string> listOfHash = [];

                        string distSql = $@"SELECT colname from syscat.columns 
                            where TABSCHEMA = '{schema}' and TABNAME = '{tableName}' and partkeyseq !=0
                            order by partkeyseq with ur";

                        using (var cmd2 = conn.CreateCommand())
                        {
                            cmd2.CommandText = distSql;
                            using var rdrDist = cmd2.ExecuteReader();
                            while (rdrDist.Read())
                            {
                                listOfHash.Add(rdrDist.GetString(0));
                            }
                        }

                        distributeInfo = $"DISTRIBUTE BY HASH({string.Join(',', listOfHash)})";
                    }
                    rdr.Close();
                }

                if (keycolumns >= 1)
                {
                    SQL = GetConstraints1Sql(schema, tableName);

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SQL;
                        var rdr = cmd.ExecuteReader();
                        while (rdr.Read())
                        {
                            pk = rdr.GetString(0);
                            pkComment = rdr.GetValue(2) == DBNull.Value ? null : rdr.GetString(2);
                            pkCols.Add(rdr.GetString(1));

                            pkEnforced = rdr.GetString(3);
                            pkTrusted = rdr.GetString(4);

                            pkEnforced = pkEnforced switch
                            {
                                "Y" => " ENFORCED",
                                "N" => " NOT ENFORCED",
                                _ => "",
                            };
                            pkTrusted = pkTrusted switch
                            {
                                "Y" => " TRUSTED",
                                "N" => " NOT TRUSTED",
                                _ => "",
                            };
                        }

                        rdr.Close();
                    }
                }

                SQL = $@"SELECT c.constname
                             , kcu.colname
                             , c.remarks
                             , c.ENFORCED
                             , c.TRUSTED
                          FROM syscat.tabconst c
                               , syscat.keycoluse kcu
                         WHERE c.tabschema = '{schema}'
                               and c.TABNAME = '{tableName}'
                               AND c.type = 'F'
                           AND kcu.constname = c.constname
                           AND kcu.tabschema = c.tabschema
                           AND kcu.tabname   = c.tabname
                         ORDER BY c.constname
                                , kcu.colseq
                        WITH UR";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        string constname = rdr.GetString(0);
                        string colname = rdr.GetString(1);
                        string? remark = rdr.GetValue(2) == DBNull.Value ? null : rdr.GetString(2);

                        if (!fkCols.TryGetValue(constname, out List<string>? value1))
                        {
                            value1 = [];
                            fkCols[constname] = value1;
                        }

                        value1.Add(colname);
                        fkComments[constname] = remark;

                        string fkEnforcedVal = rdr.GetString(3);
                        string fkTrustedVal = rdr.GetString(4);

                        fkEnforced[constname] = fkEnforcedVal switch
                        {
                            "Y" => " ENFORCED",
                            "N" => " NOT ENFORCED",
                            _ => "",
                        };
                        fkTrusted[constname] = fkTrustedVal switch
                        {
                            "Y" => " TRUSTED",
                            "N" => " NOT TRUSTED",
                            _ => "",
                        };
                    }
                    rdr.Close();
                }


                SQL = $@"SELECT
                        r.CONSTNAME,REFTABSCHEMA,REFTABNAME, PK_COLNAMES, DELETERULE, UPDATERULE
                    FROM
                        syscat.references r
                    WHERE
                        R.TABSCHEMA = '{schema}'
                         AND r.tabname = '{tableName}'";

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        string constname = rdr.GetString(0);
                        string REFTABSCHEMA = rdr.GetString(1).Trim();
                        //QuoteNameIfNeeded(ref REFTABSCHEMA);

                        string REFTABNAME = rdr.GetString(2);
                        string PK_COLNAMES = rdr.GetString(3);
                        string DELETERULEpom = rdr.GetString(4);
                        string UPDATERULE = rdr.GetString(5);
                        string deleterule = DELETERULEpom switch
                        {
                            "R" => "ON DELETE RESTRICT",
                            "C" => "ON DELETE CASCADE",
                            "N" => "ON DELETE SET NULL",
                            _ => $"ON DELETE {UPDATERULE} ???",
                        };
                        constraints[constname] = $"REFERENCES {QuoteNameIfNeeded(REFTABSCHEMA)}.{QuoteNameIfNeeded(REFTABNAME)}({Regex.Replace(PK_COLNAMES.Trim(), @"\s{3,}", ",")}) {deleterule}";
                    }
                    rdr.Close();
                }

                SQL = GetChecksSql(schema, tableName);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        string constname = rdr.GetString(0);
                        checks[constname] = rdr.GetString(1);

                        string fkEnforcedVal = rdr.GetString(2);
                        string fkTrustedVal = rdr.GetString(3);

                        fkEnforcedVal = fkEnforcedVal switch
                        {
                            "Y" => " ENFORCED",
                            "N" => " NOT ENFORCED",
                            _ => "",
                        };
                        fkTrustedVal = fkTrustedVal switch
                        {
                            "Y" => " TRUSTED",
                            "N" => " NOT TRUSTED",
                            _ => "",
                        };
                        checksEnforced[constname] = fkEnforcedVal;
                        checksTrusted[constname] = fkTrustedVal;
                    }
                }
                //  ase uniquerule
                //    when 'P' then 'Primary key'
                //    when 'U' then 'Unique'
                //    when 'D' then 'Nonunique'
                //end as type,

                SQL = $@"SELECT INDSCHEMA, INDNAME, COLNAMES,
                     uniquerule,
                    case indextype 
                        when 'BLOK' then 'Block index'
                        when 'CLUS' then 'Clustering index'
                        when 'DIM' then 'Dimension block index'
                        when 'REG' then 'Regular index'
                        when 'XPTH' then 'XML path index'
                        when 'XRGN' then 'XML region index'
                        when 'XVIL' then 'Index over XML column (logical)'
                        when 'XVIP' then 'Index over XML column (physical)'
                    end as index_type
                    , COMPRESSION
                    FROM syscat.indexes  WHERE indschema not like 'SYS%' AND TABSCHEMA = '{schema}' AND TABNAME = '{tableName}' WITH UR;";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        string indshema = rdr.GetString(0).Trim();
                        string indName = rdr.GetString(1);
                        string compressIndexText = "";

                        //QuoteNameIfNeeded(ref indshema);


                        var indexColsList = rdr.GetString(2).Split('+');
                        for (int i = 0; i < indexColsList.Length; i++)
                        {
                            indexColsList[i] = QuoteNameIfNeeded(indexColsList[i]);
                        }
                        ArraySegment<string> indexArraySegment;
                        if (string.IsNullOrWhiteSpace(indexColsList[0]))
                        {
                            indexArraySegment = new ArraySegment<string>(indexColsList, 1, indexColsList.Length - 1);
                        }
                        else
                        {
                            indexArraySegment = new ArraySegment<string>(indexColsList, 0, indexColsList.Length);
                        }

                        string uniquerule = rdr.GetString(3);
                        if (uniquerule == "U")
                        {
                            uniquerule = " UNIQUE";
                        }
                        else
                        {
                            uniquerule = "";
                        }
                        var tmp = rdr.GetValue(5);
                        if (tmp != DBNull.Value && tmp is string str && str == "Y")
                        {
                            compressIndexText = " COMPRESS YES";
                        }

                        indexes.Add($"CREATE{uniquerule} INDEX {QuoteNameIfNeeded(indshema)}.{QuoteNameIfNeeded(indName)} ON {clearSchema}.{clearTableName}({string.Join(',', (IEnumerable<string>)indexArraySegment)}){compressIndexText};");
                    }
                }

                SQL = GetTriggersSql(schema, tableName);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        triggers.Add(rdr.GetString(6));
                    }
                }


                SQL = "SELECT DATAPARTITIONEXPRESSION, NULLSFIRST FROM SYSCAT.DATAPARTITIONEXPRESSION " +
                    $"WHERE TABSCHEMA = '{schema}' AND TABNAME = '{tableName}' order by DATAPARTITIONKEYSEQ;";
                List<string> ls = [];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SQL;
                    var rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        var temp = rdr.GetValue(1);
                        string nullInfo = "";
                        if (temp != DBNull.Value && temp is string str && str == "N")
                        {
                            nullInfo = " NULLS LAST";
                        }
                        ls.Add(rdr.GetString(0) + nullInfo);
                    }
                }
                if (ls.Count > 0)
                {
                    partitionInfo = $"PARTITION BY RANGE({string.Join(',', ls)}){Environment.NewLine}";

                    SQL = $@"SELECT P.DATAPARTITIONNAME,P.LOWVALUE,P.HIGHVALUE,P.LOWINCLUSIVE,P.HIGHINCLUSIVE, S.TBSPACE
                        FROM SYSCAT.DATAPARTITIONS P 
                        LEFT JOIN syscat.tablespaces S ON S.TBSPACEID = P.TBSPACEID
                        WHERE P.TABSCHEMA = '{schema}' AND P.TABNAME = '{tableName}' ORDER BY SEQNO 
                        WITH UR;";

                    bool multiKey = false;
                    if (ls.Count > 1)
                    {
                        multiKey = true;
                    }

                    ls.Clear();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SQL;
                        var rdr = cmd.ExecuteReader();
                        //int i = 0;
                        while (rdr.Read())
                        {
                            string DATAPARTITIONNAME = rdr.GetString(0);
                            string LOWVALUE = multiKey ? $"({rdr.GetValue(1)})" : $"{rdr.GetValue(1)}";
                            string HIGHVALUE = multiKey ? $"({rdr.GetValue(2)})" : $"{rdr.GetValue(2)}";
                            string LOWINCLUSIVE = rdr.GetString(3) == "Y" ? "INCLUSIVE" : "EXCLUSIVE";
                            string HIGHINCLUSIVE = rdr.GetString(4) == "Y" ? "INCLUSIVE" : "EXCLUSIVE";
                            string TBSPACE = rdr.GetString(5);
                            //i++;
                            ls.Add($"PARTITION {DATAPARTITIONNAME} STARTING FROM {LOWVALUE} {LOWINCLUSIVE} ENDING AT {HIGHVALUE} {HIGHINCLUSIVE} IN {TBSPACE}");
                        }
                        partitionInfo += $"({string.Join($",{Environment.NewLine}", ls)})";
                    }
                }
                conn.Close();
            }
        });

        DatabaseColumn[] columnsOfTable = GetColumns(database, schema, tableName, "").ToArray();


        string[] columnsWithTypes = new string[columnsOfTable.Length];
        for (int i = 0; i < columnsWithTypes.Length; i++)
        {
            columnsWithTypes[i] = $"{QuoteNameIfNeeded(columnsOfTable[i].Name)} {columnsOfTable[i].FullTypeName}";
        }

        sb.AppendLine($"CREATE TABLE {clearSchema}.{clearTableName}");
        sb.AppendLine("(");
        sb.Append("    ");
        sb.AppendLine(string.Join($",{Environment.NewLine}    ", columnsWithTypes));
        sb.AppendLine(")");
        if (tbSpace != null)
        {
            sb.AppendLine($"ORGANIZE BY {organize} IN {tbSpace}"); // SELECT XXX = TABLEORG , YYY = TBSPACE, ZZZ = COMPRESSION FROM SYSCAT.TABLES T WHERE T.TABNAME = 'EMPLOYEE'
        }
        else
        {
            sb.AppendLine($"ORGANIZE BY {organize}");
        }
        if (!string.IsNullOrWhiteSpace(distributeInfo))
        {
            sb.AppendLine(distributeInfo);
        }
        sb.AppendLine($"COMPRESS {compression}{Environment.NewLine}{partitionInfo};");

        //PK
        if (keycolumns >= 1)
        {
            string clearPk = QuoteNameIfNeeded(pk);
            sb.AppendLine($"ALTER TABLE {clearSchema}.{clearTableName} ADD CONSTRAINT {clearPk} PRIMARY KEY({string.Join(",", pkCols)}){pkEnforced}{pkTrusted};");
            if (pkComment is not null)
            {
                sb.AppendLine($"COMMENT ON CONSTRAINT {clearSchema}.{clearTableName}.{clearPk}  IS '{pkComment.Replace("'", "''")}';");
            }
        }
        if (remarks != null)
        {
            sb.AppendLine($"COMMENT ON TABLE {clearSchema}.{clearTableName} IS '{remarks.Replace("'", "'")}';");
        }
        // COMMENT ON CONSTRAINT TEST.EMPLOYEE.RED IS 'DDDD';


        foreach (var item in fkCols)
        {
            sb.AppendLine($"ALTER TABLE {clearSchema}.{clearTableName} ADD CONSTRAINT {item.Key} FOREIGN KEY({string.Join(",", item.Value)}) {constraints[item.Key]}{fkEnforced[item.Key]}{fkTrusted[item.Key]};");

            if (fkComments[item.Key] != null)
            {
                sb.AppendLine($"COMMENT ON CONSTRAINT {clearSchema}.{clearTableName}.{item.Key}  IS '{fkComments[item.Key].Replace("'", "''")}';");
            }
        }

        foreach (var item in checks.Keys)
        {
            sb.AppendLine($"ALTER TABLE {clearSchema}.{clearTableName} ADD CONSTRAINT {item} CHECK({checks[item]}){checksEnforced[item]}{checksTrusted[item]};");
        }

        for (int i = 0; i < columnsOfTable.Length; i++)
        {
            if (columnsOfTable[i].Desc is not null)
            {
                sb.AppendLine($"COMMENT ON COLUMN {clearSchema}.{clearTableName}.{columnsOfTable[i].Name} IS '{columnsOfTable[i].Desc.Replace("'", "'")}';");
            }
        }

        foreach (var item in indexes)
        {
            sb.AppendLine(item);
        }

        if (triggers.Count > 0)
        {
            sb.AppendLine("--REGION TRIGGERS");
        }
        foreach (var item in triggers)
        {
            sb.AppendLine(item);
        }
        if (triggers.Count > 0)
        {
            sb.AppendLine("--ENDREGION");
        }

        //COMMENT ON COLUMN TEST.EMPLOYEE.EDLEVEL     IS 'highest grade level passed in school'
        // NOT NULL + 
        //Unique
        //Primary key + 
        //Foreign Key
        //Check
        //Informational
    }


    private static string GetConstraints1Sql(string schema, string tablename)
    {
        return $@"SELECT c.CONSTNAME, kcu.colname, c.remarks, c.ENFORCED, c.TRUSTED
                              FROM syscat.tabconst c
                                   , syscat.keycoluse kcu
                               WHERE c.tabschema = '{schema}'
                                     and c.TABNAME = '{tablename}'
                                    AND c.type = 'P'
                               AND kcu.constname = c.constname
                               AND kcu.tabschema = c.tabschema
                               AND kcu.tabname   = c.tabname
                             ORDER BY c.constname
                                    , kcu.colseq
                            WITH UR
                            ;";
    }
    private static string GetTriggersSql(string schema, string tablename)
    {
        return $@"select 
                                    trigname as trigger_name,
                                    tabschema , 
                                    tabname , 
                                    case trigtime 
                                         when 'B' then 'before'
                                         when 'A' then 'after'
                                         when 'I' then 'instead of' 
                                    end as activation,
                                    rtrim(case when eventupdate ='Y' then  'update ' else '' end 
                                          concat 
                                          case when eventdelete ='Y' then  'delete ' else '' end
                                          concat
                                          case when eventinsert ='Y' then  'insert ' else '' end)
                                    as event,   
                                    case when ENABLED = 'N' then 'disabled'
                                    else 'active' end as status,
                                    text as definition
                                from syscat.triggers t
                                where tabschema = '{schema}'
                                      AND  TABNAME = '{tablename}'
                                order by trigname";
    }

    private static string GetChecksSql(string schema, string tablename)
    {
        return $@"select con.CONSTNAME, con.text, tcu.ENFORCED, tcu.TRUSTED
                        from 
                            syscat.checks con
                            join syscat.tabconst tcu on tcu.CONSTNAME = con.CONSTNAME and tcu.TABSCHEMA= '{schema}' and con.tabname = '{tablename}'
                        where
                            con.TABSCHEMA = '{schema}'
                            and con.tabname = '{tablename}'
                    ";
    }

    public override async Task DbSpecificImportPart(IDbImportJob importJob, string randName, Action<string>? progress, bool tableExists = false)
    {
        await Task.Run(() =>
        {
            using var conn = GetConnection(null) as DB2Connection;
            if (conn is null)
            {
                return;
            }

            conn.Open();
            if (!tableExists)
            {
                string[] headers = importJob.ReturnHeadersWithDataTypes(DatabaseTypeEnum.DB2);
                string SQL = $"CREATE TABLE {randName} ({string.Join(',', headers)}){Environment.NewLine}ORGANIZE BY ROW{Environment.NewLine}COMPRESS YES{Environment.NewLine};";
                using DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = SQL;
                cmd.ExecuteNonQuery();
            }

            using DB2BulkCopy cpy = new DB2BulkCopy(conn, DB2BulkCopyOptions.TableLock);
            cpy.BulkCopyTimeout = DEFAULT_COMMAND_TIMEOUT;
            cpy.DestinationTableName = randName;
            cpy.NotifyAfter = 10_000;
            cpy.DB2RowsCopied += (o, e) => progress?.Invoke($"Copied {e.RowsCopied:N0}");
            cpy.WriteToServer(importJob.AsReader);

            if (cpy.Errors.Count > 0)
            {
                if (progress is not null)
                {
                    progress?.Invoke($"{cpy.Errors.Count} Errors");
                    foreach (DB2Error item in cpy.Errors)
                    {
                        progress?.Invoke($"ERROR! Row: {item.RowNumber} Message:{item.Message}");
                    }
                    cpy.Close();
                    conn.Close();
                }
                else
                {
                    throw new Exception($"{cpy.Errors.Count} Errors");
                }
            }
            else
            {
                cpy.Close();
                conn.Close();
            }
        });
    }

    public override string GetEmpty(string table, string database, string schema)
    {
        var tableCl = GetQuotedTwoOrTreePartName(database, schema, table);

        return @$"TRUNCATE TABLE {tableCl} IMMEDIATE;";
    }

    protected override string? GetExternalTableSql(string database)
    {
        throw new NotImplementedException();
    }
}