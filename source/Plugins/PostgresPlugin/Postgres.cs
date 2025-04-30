using System.Data.Common;
using System.Text;
using Npgsql;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.PluginCommon.Enums;

namespace PostgresPlugin;

internal sealed class Postgres : DatabaseService
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.PostgreSql;
    public Postgres(string username, string password, string port, string ip, string db, int connectionTimeout) : base(username, password, port, ip, db, connectionTimeout)
    {
        DatabaseType = WHO_I_AM_CONST;
        AutoCompletDatabaseMode = CurrentAutoCompletDatabaseMode.DatabaseSchemaTable | CurrentAutoCompletDatabaseMode.SchemaTable;
        PrefrerUpperCase = false;
    }

    public override DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false)
    {
        databaseName ??= Database;

        NpgsqlConnectionStringBuilder builder = new()
        {
            Username = Username,
            Password = Password,

            Host = Ip,
            Database = databaseName,
            Timeout = CONNECTION_TIMEOUT,
            Pooling = pooling
        };
        var conn = new NpgsqlConnection(builder.ConnectionString);

        conn.Notice += Conn_Notice;
        return conn;
    }

    private void Conn_Notice(object sender, NpgsqlNoticeEventArgs e)
    {
        DbMessageAction?.Invoke(e.Notice.MessageText);
    }

    protected override string GetSqlTablesAndOtherObjects(string dbName)
    {
        return
            """
            SELECT 
                C.OID::int AS OBJECT_ID
                , C.RELNAME AS OBJECT_NAME
                , OBJ_DESCRIPTION(C.OID) AS DESCRIPTION
                , N.NSPNAME AS TABLE_SCHEMA
                 , CASE C.RELKIND
                     WHEN 'v' THEN 'VIEW'
                     WHEN 'p' THEN 'BASE TABLE'
                     WHEN 'r' THEN 'TABLE'
                     ELSE 'unknown table type'
                   END AS TABLE_TYPE
                 , OWN.ROLNAME AS OWNER
                 , NULL AS CREATEDATATIME
            FROM   PG_CATALOG.PG_CLASS C
            JOIN PG_CATALOG.PG_NAMESPACE N ON N.OID = C.RELNAMESPACE
            JOIN PG_CATALOG.PG_AUTHID OWN ON OWN.OID = C.RELOWNER 
            WHERE
            NOT C.RELISPARTITION
            AND C.RELKIND IN ('v', 'p', 'r')
            AND C.RELNAME IS NOT NULL

            UNION ALL

            select 
                -1
                , proc.routine_name || '(' || string_agg(args.parameter_mode || ' ' || args.parameter_name || ' ' || args.data_type, ',' order by args.ordinal_position) || ')' AS NAME_
                , NULL AS DESCRIPTION
                , proc.specific_schema as procedure_schema
                , proc.routine_type 
                , 'OWNER TO DO'
                ,  NULL AS CREATEDATATIME
            from information_schema.routines proc
            left join information_schema.parameters args
                      on proc.specific_schema = args.specific_schema
                      and proc.specific_name = args.specific_name
            where proc.routine_schema not in ('pg_catalog', 'information_schema')
                  and proc.routine_type in('PROCEDURE','FUNCTION')
            group by 
                proc.specific_schema, proc.specific_name, proc.routine_name, proc.routine_definition,proc.routine_type
            having
                proc.routine_name || '(' || string_agg(args.parameter_mode || ' ' || args.parameter_name || ' ' || args.data_type, ',' order by args.ordinal_position) || ')' IS NOT NULL

            UNION ALL

            select -1, s.sequence_name,null,s.sequence_schema,'SEQUENCE','OWNER TO DO',NULL AS CREATEDATATIME from information_schema.sequences s
            WHERE s.sequence_name IS NOT NULL
            """;
    }

    protected override string GetSqlOfColumns(string dbName)
    {
        return
            $"""
                SELECT 
                    C.OID::int AS REL_OBJECT_ID
                    , COL."column_name"
                    , NULL AS DESCRIPTION
                    , COL.data_type || 
                        COALESCE('(' ||COL.character_maximum_length || ')', 
                        '(' || COL."numeric_precision" || ',' || COL."numeric_scale" || ')',
                        '') 
                        || CASE WHEN COL.is_nullable = 'YES' THEN ' NOT NULL' ELSE '' END  
                    , CASE WHEN COL.is_nullable = 'YES' THEN true ELSE false END  AS ATTNOTNULL
                    , COL."column_default"
                FROM PG_CATALOG.PG_ATTRIBUTE A 
                JOIN PG_CATALOG.PG_TYPE TYP ON TYP.OID = A.ATTTYPID 
                JOIN PG_CATALOG.PG_CLASS C ON C.OID = A."attrelid"
                JOIN PG_CATALOG.PG_NAMESPACE N ON N.OID = C.RELNAMESPACE 
                JOIN INFORMATION_SCHEMA.COLUMNS COL ON COL."column_name" = A."attname" AND N.NSPNAME = COL."table_schema"  AND C.RELNAME = COL."table_name"
                ORDER BY C.OID, COL."ordinal_position"
                """;
    }

    protected override List<(string, string)> GetDatabases()
    {
        var databases = new List<(string, string)>();
        using (var con = GetConnection(Database))
        {
            con.Open();
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = "SELECT DATNAME FROM PG_CATALOG.PG_DATABASE;";
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    databases.Add((rdr.GetString(0), "SCHEMA"));
                }
            }
        }
        return databases;
    }

    protected override string? GetExternalTableSql(string database)
    {
        throw new NotImplementedException();
    }

    protected override string? GetProceduresSql(string database, string objectFilterName)
    {
        return
            """
            select 
                proc.specific_schema as procedure_schema
                , proc.routine_definition
                , -1 AS ID
                , NULL AS RETURNS
                , NULL AS EXECUTEDASOWNER
                , NULL AS DESCRIPTION
                , proc.routine_name || '(' || string_agg(args.parameter_mode || ' ' ||  args.parameter_name || ' ' || args.data_type, ',' order by args.ordinal_position) || ')' AS PROCEDURE_SIGNATURE
                , '(' || string_agg(args.parameter_mode || ' ' ||  args.parameter_name || ' ' || args.data_type, ',' order by args.ordinal_position)|| ')' AS ARGUMENTS
                , proc.external_language
                --, proc.specific_name AS SPECIFICNAME
                   --args.parameter_name,
                   --args.parameter_mode,
                   --args.data_type

            from information_schema.routines proc
            left join information_schema.parameters args
                      on proc.specific_schema = args.specific_schema
                      and proc.specific_name = args.specific_name
            where proc.routine_schema not in ('pg_catalog', 'information_schema')
                  and proc.routine_type = 'PROCEDURE'
            group by 
                proc.specific_schema, proc.specific_name, proc.routine_name, proc.routine_definition, proc.external_language
            """;
    }

    public override async ValueTask GetCreateProcedureTextStringBuilder(StringBuilder sb, string database, string schema, string procName, bool forceFreshCode = false)
    {
        if (!_procedureDictCache.ContainsKey(database))
        {
            await CacheAllObjects([TypeInDatabaseEnum.Procedure], database);
        }
        if (_procedureDictCache.TryGetValue(database, out var d1) && d1.TryGetValue(schema, out var d2) &&
            d2.TryGetValue(procName, out var d3))
        {
            var (cleanDatabaseName, cleanSchema) = GetCleanedNames(database, schema);
            sb.AppendLine($"CREATE PROCEDURE {cleanDatabaseName}.{cleanSchema}.{procName}");// {d3.Arguments}
            sb.AppendLine($"LANGUAGE {d3.ProcLanguage}");
            sb.AppendLine("AS $$");
            sb.AppendLine(d3.ProcedureSource);
            sb.AppendLine("$$;");
            if (d3.Desc is not null)
            {
                string cmt = d3.Desc;
                if (cmt.Contains('\''))
                {
                    cmt = cmt.Replace("'", "''");
                }
                sb.AppendLine($"COMMENT ON PROCEDURE {procName} IS '{cmt}';");
            }
        }
    }

    protected override string? GetSynonymSql(string database)
    {
        throw new NotImplementedException();
    }

    public override async ValueTask GetCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName, string? overrideTableName = null, string? middleCode = null, string? endingCode = null, List<string>? distOverride = null)
    {
        await Task.Run(() =>
        {
            sb.Append($"CREATE TABLE {schema}.{tableName}{Environment.NewLine}({Environment.NewLine}    ");
            string sql = @$"SELECT 
                            column_name
                            , data_type
                            , character_maximum_length
                            , is_nullable
                            , column_default
                            , collation_name
                            , numeric_precision
                            , numeric_scale
                            , numeric_precision_radix
                        FROM information_schema.columns 
                        WHERE 
                            table_name = '{tableName}'
                            and table_schema = '{schema}'
                        ORDER BY 
                            ordinal_position";
            string sqlConstraints = $@"    SELECT 
                            C1.constraint_name
                            , C1.constraint_type
                            , C1.enforced
                            , string_agg(C2.column_name, ',')
                            , C3.constraint_schema
                            , C3.match_option
                            , C3.update_rule
                            , C3.delete_rule
                            , C2.table_name
                            , C2.table_schema
                            , X1.colsForFk
                            , C4.check_clause
                        FROM 
                            information_schema.table_constraints C1
                            JOIN information_schema.constraint_column_usage C2 ON C2.constraint_name = C1.constraint_name 
                            LEFT JOIN information_schema.referential_constraints C3 ON C3.constraint_name = C1.constraint_name
                            LEFT JOIN 
                            (
                            SELECT 
                                a.constraint_name
                                , string_agg(column_name, ',') as colsForFk
                            FROM 
                                information_schema.key_column_usage a
                            GROUP BY 
                                1
                            ) 
                            X1 ON X1.constraint_name = C1.constraint_name 
                            LEFT JOIN information_schema.check_constraints C4 ON
                                C4.constraint_name = C1.constraint_name 
                        WHERE 
                             C1.table_schema = '{schema}' and C1.table_name = '{tableName}'
                        GROUP BY
                            1,2,3,5,6,7,8,9,10,11,12
                        ORDER BY 
                            C1.constraint_type DESC";

            string sqlTriggers = $@"select
                'CREATE TRIGGER ' || trigger_name || ' ' || action_timing || '
                ' ||
                string_agg(event_manipulation, ' OR ') ||'
                 ON ' ||event_object_schema||'.'||event_object_table ||
                ' FOR EACH ROW ' || action_statement

                FROM information_schema.triggers
                WHERE 
                 event_object_schema = '{schema}' and event_object_table = '{tableName}'
                 group by trigger_name, action_timing, event_object_schema , event_object_table, action_statement";

            sql = sql + ";" + sqlConstraints + ";" + sqlTriggers;
            List<string> triggers = [];
            List<string> arr = [];

            using (var conn = GetConnection(database))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        string dataType = rdr.GetString(1);
                        var len = rdr.GetValue(2);
                        var prec = rdr.GetValue(6);
                        var scale = rdr.GetValue(7);
                        string lenD = "";
                        string collation = "";
                        if (len != DBNull.Value)
                        {
                            lenD = $"({len})";
                            var collation_name = rdr.GetValue(5);
                            if (collation_name != DBNull.Value)
                            {
                                collation = " to do from information_schema.collations";
                            }
                            else
                            {
                                collation = " COLLATE pg_catalog.\"default\"";
                            }
                        }
                        else if (prec != DBNull.Value)
                        {
                            lenD = $"({prec},{scale})";
                        }

                        string isNull = rdr.GetString(3);
                        if (isNull == "NO")
                        {
                            isNull = " NOT NULL";
                        }
                        else
                        {
                            isNull = "";
                        }

                        var colDef = rdr.GetValue(4);
                        string colDefString = "";
                        if (colDef != DBNull.Value)
                        {
                            colDefString = $" DEFAULT {colDef}";
                        }
                        arr.Add(rdr.GetString(0) + " " + dataType + lenD + collation + isNull + colDefString);
                    }

                    rdr.NextResult();
                    List<string> cnsArr = [];

                    while (rdr.Read())
                    {
                        string keyType = rdr.GetString(1);
                        if (keyType == "FOREIGN KEY")
                        {
                            string matchOpt = rdr.GetString(5);
                            matchOpt = matchOpt == "NONE" ? "SIMPLE" : matchOpt;
                            cnsArr.Add("CONSTRAINT " + rdr.GetString(0) + " " + keyType + $"({rdr.GetString(10)}) REFERENCES {rdr.GetString(9)}.{rdr.GetString(8)}({rdr.GetString(3)})" +
                                $" MATCH {matchOpt}" + Environment.NewLine +
                                $"        ON UPDATE {rdr.GetString(6)}{Environment.NewLine}        ON DELETE {rdr.GetString(7)}");
                            //add = " FK to do REFERENCES, march, on update on delete information_schema.referential_constraints ";
                        }
                        else if (keyType == "PRIMARY KEY")
                        {
                            cnsArr.Add("CONSTRAINT " + rdr.GetString(0) + " " + keyType + $"({rdr.GetString(10)})");
                        }
                        else
                        {
                            cnsArr.Add("CONSTRAINT " + rdr.GetString(0) + " " + keyType + $" {rdr.GetString(11)}");
                        }
                    }
                    arr.AddRange(cnsArr);
                    rdr.NextResult();
                    while (rdr.Read())
                    {
                        triggers.Add(rdr.GetString(0) + ";");
                    }

                }
            }

            string partitonSqlTxt = PartitonSql1(schema, tableName);

            List<string> partitions = [];
            using (var conn = GetConnection(database))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = partitonSqlTxt;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        partitions.Add(rdr.GetString(0));
                    }
                }
            }

            sql = $@"
select 
    par.relnamespace::regnamespace::text as schema, 
    par.relname as table_name, 
    partnatts as num_columns,
    column_index,
    col.column_name,
    pt.partition_strategy
from   
    (select
         partrelid,
         partnatts,
         case partstrat 
              when 'l' then 'list' 
              when 'r' then 'range' end as partition_strategy,
         unnest(partattrs) column_index
     from
         pg_partitioned_table) pt 
join   
    pg_class par 
on     
    par.oid = pt.partrelid
join
    information_schema.columns col
on  
    col.table_schema = par.relnamespace::regnamespace::text
    and col.table_name = par.relname
    and ordinal_position = pt.column_index
WHERE 
    par.relnamespace::regnamespace::text = '{schema}'
    AND par.relname = '{tableName}'
ORDER BY 
    column_index ASC;";

            string partitonInfo = "";
            List<string> partColumns = [];
            using (var conn = GetConnection(database))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        if (string.IsNullOrWhiteSpace(partitonInfo))
                        {
                            partitonInfo = " PARTITION BY " + rdr.GetString(5) + " ";
                        }

                        partColumns.Add(rdr.GetString(4));
                    }
                }
            }

            if (partitonInfo != "")
            {
                partitonInfo += "(" + string.Join(",", partColumns) + ")";
            }
            sb.AppendLine(string.Join("," + Environment.NewLine + "    ", arr));
            sb.Append(')');
            sb.Append($"{partitonInfo};");
            sb.Append(Environment.NewLine + GetIndexes(tableName));
            sb.Append(Environment.NewLine);
            sb.Append(string.Join(Environment.NewLine, triggers));
            sb.Append(Environment.NewLine);
            sb.Append(string.Join(Environment.NewLine, partitions));
            sb.Append(Environment.NewLine);
        });
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
    }

    Dictionary<string, List<string>> indexes;
    const string indexesSql = @"SELECT
                        tablename
                        , indexname
                        , indexdef
                        , i.tablespace
                    FROM
                        pg_indexes i
                        LEFT JOIN information_schema.table_constraints PK on
                            PK.constraint_type = 'PRIMARY KEY'
                            AND i.indexname = pk.constraint_name
                    WHERE 
                        PK.constraint_type is null    
                    ORDER BY 1,2";
    private string GetIndexes(string tablename)
    {
        if (indexes == null)
        {
            indexes = [];
            using (var conn = GetConnection(null))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = indexesSql;
                    var rdr = cmd.ExecuteReader();
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            if (!indexes.ContainsKey(rdr.GetString(0)))
                            {
                                indexes[rdr.GetString(0)] = [];
                            }
                            indexes[rdr.GetString(0)].Add(rdr.GetString(2));
                        }
                    }
                }
            }
        }

        if (!indexes.TryGetValue(tablename, out List<string>? value))
        {
            return "";
        }
        return string.Join(";" + Environment.NewLine, value) + ";";
    }

    private static string PartitonSql1(string schema, string tablename)
    {
        return $@"
select 
    'CREATE TABLE IF NOT EXISTS ' || pt.relname || ' PARTITION OF ' || base_tb.relname || ' ' || pg_get_expr(pt.relpartbound, pt.oid, true) || ';' AS PART
, pt.relname
from pg_class base_tb 
join pg_inherits i on i.inhparent = base_tb.oid 
join pg_class pt on pt.oid = i.inhrelid
where 
  base_tb.oid = '{schema}.{tablename}'::regclass;";
    }

}