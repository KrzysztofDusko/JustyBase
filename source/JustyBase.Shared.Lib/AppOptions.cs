using System.Text.Json.Serialization;

namespace JustyBase;

public sealed class AppOptions
{
    public List<string> StartsFolderPaths { get; set; } = new();
    public int ResultRowsLimit { get; set; } = 20_000;
    public int? ResultRowsLimitWarning { get; set; }//currently only in pro
    public int ConnectionTimeout { get; set; } = 5; //s
    public int CommandTimeout { get; set; } = 3_600; //s = 3_600 = 1 hour
    //AllSnippets[name] = (snippetType, desc, text, keyword - not used)
    public Dictionary<string, (string snippetType, string? Description, string? Text, string? Keyword)> AllSnippets { get; set; } = new();
    public string SepInExportedCsv { get; set; } = ";";
    public string SepRowsInExportedCsv { get; set; } = "windows";
    public string EncondingName { get; set; } = "UTF-8";
    public string DecimalDelimInCsv { get; set; } = "'";
    public bool ImportExisting { get; set; }
    public bool DontRefreshImportedTableSchema { get; set; }
    public bool UseXlsb { get; set; } = true;
    public string DefaultXlsxSheetName { get; set; } = "sheet";
    public bool? CloseUndocked { get; set; } = true;
    public bool CollapseFoldingOnStartup { get; set; } = true;
    public bool AcceptDiagData { get; set; } = false;
    public bool AcceptCrashData { get; set; } = false;
    public int ThemeNum { get; set; } = 0;
    public string? ConnectionNameInSchemaSearch { get; set; }
    public bool CaseSensitive { get; set; }
    public bool SearchInSource { get; set; }
    public bool WholeWords { get; set; }
    public bool RegexMode { get; set; }
    public bool RefreshOnStartupInSchemaSearch { get; set; }
    public bool SingleLineTabs { get; set; }
    public bool AutocompleteOnReturn { get; set; } = false;
    public bool ConfirmDocumentClosing { get; set; } = false;
    public double ControlContentThemeFontSize { get; set; } = 12.0;
    public double CompletitionFontSize { get; set; } = 13.0;
    public int DefaultFontSizeForDocuments { get; set; } = 13;
    public string DocumentFontName { get; set; } = "Cascadia Code";
    public double LineSpacing { get; set; } = 1.0;
    public bool ShowDetailsButton { get; set; } = false;
    public bool DoGcCollect { get; set; } = false;//remove ?
    public int LayoutNum { get; set; } = 1;
    public bool UseSplashScreen { get; set; } = true;
    public bool AutoDownloadUpdate { get; set; } = true;
    public bool AutoDownloadPlugins { get; set; } = true;
    public bool UpdateMitigateNextGenFirewalls { get; set; } = true; // palo alto
    public int LimitHistoryMonths { get; set; } = 6;

    //TODO enum
    public const string FAST_SNIPET_TXT = "fast";
    public const string TYPO_SNIPET_TXT = "typo";
    public const string STANDARD_SNIPET_TXT = "standard";

    public bool ResetPlugins { get; set; } = false;

    public void AddDefaultValues()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        AllSnippets ??= new Dictionary<string, (string snippetType, string? desc, string? text, string? keyword)>()
        {
            {"sx",(FAST_SNIPET_TXT,null,"select","sx") },
            {"SX",(FAST_SNIPET_TXT,null,"SELECT","SX") },
            {"sx*",(FAST_SNIPET_TXT,null,"select * from","sx*") },
            {"SX*",(FAST_SNIPET_TXT,null,"SELECT * FROM","SX*") },
            {"wx",(FAST_SNIPET_TXT,null,"where","wx") },
            {"WX",(FAST_SNIPET_TXT,null,"WHERE","WX") },
            {"fx",(FAST_SNIPET_TXT,null,"from","fx") },
            {"FX",(FAST_SNIPET_TXT,null,"FROM","FX") },
            {"gx",(FAST_SNIPET_TXT,null,"group by","gx") },
            {"GX",(FAST_SNIPET_TXT,null,"GROUP BY","GX") },
            {"hx",(FAST_SNIPET_TXT,null,"having","hx") },
            {"HX",(FAST_SNIPET_TXT,null,"HAVING","HX") },
            {"lx",(FAST_SNIPET_TXT,null,"limit","lx") },
            {"LX",(FAST_SNIPET_TXT,null,"LIMIT","LX") },
            {"ox",(FAST_SNIPET_TXT,null,"order by","ox") },
            {"OX",(FAST_SNIPET_TXT,null,"ORDER BY","OX*") },
            {"dx",(FAST_SNIPET_TXT,null, "drop table","dx") },
            {"DX",(FAST_SNIPET_TXT,null, "DROP TABLE","DX") },

            {"DISTINCT",(TYPO_SNIPET_TXT,null, null, null) },
            {"GROUP",(TYPO_SNIPET_TXT,null, null, null) },
            {"ORDER",(TYPO_SNIPET_TXT,null, null, null) },
            {"PARTITION",(TYPO_SNIPET_TXT,null, null, null) },
            {"BETWEEN",(TYPO_SNIPET_TXT,null, null, null) },
            {"LIMIT",(TYPO_SNIPET_TXT,null, null, null) },
            {"FIRST_VALUE",(TYPO_SNIPET_TXT,null, null, null) },
            {"LAST_VALUE",(TYPO_SNIPET_TXT,null, null, null) },
            {"DENSE_RANK",(TYPO_SNIPET_TXT,null, null, null) },
            {"DROP",(TYPO_SNIPET_TXT,null, null, null) },
            {"CROSS",(TYPO_SNIPET_TXT,null, null, null) },
            {"JOIN",(TYPO_SNIPET_TXT,null, null, null) },
            {"LEFT",(TYPO_SNIPET_TXT,null, null, null) },
            //{"INTO",(TYPO_SNIPET_TXT,null, null, null) },
            {"DATE_PART",(TYPO_SNIPET_TXT,null, null, null) },
            {"DECODE",(TYPO_SNIPET_TXT,null, null, null) },
            {"NULLIF",(TYPO_SNIPET_TXT,null, null, null) },
            {"COALESCE",(TYPO_SNIPET_TXT,null, null, null) },


             {"CASE WHEN ${Caret} THEN ; END",(STANDARD_SNIPET_TXT,null,"CASE WHEN ${Caret}\nTHEN\n;\nEND", null) },
             {"SELECT * FROM ${name}  ;",(STANDARD_SNIPET_TXT,null,"SELECT\n*\nFROM ${name} \n;", null) },
             {"CREATE TABLE ${name} AS  (  )  DISTRIBUTE ON RANDOM;",(STANDARD_SNIPET_TXT,null,"CREATE TABLE ${name} AS \n(\n\n) \nDISTRIBUTE ON RANDOM;", null) },
             {"CREATE TABLE IF NOT EXISTS ${name} AS  (  ) DISTRIBUTE ON RANDOM;",(STANDARD_SNIPET_TXT,null,"CREATE TABLE IF NOT EXISTS ${name} AS \n(\n\n) \nDISTRIBUTE ON RANDOM;", null) },
             {"DROP TABLE ${name} IF EXISTS;",(STANDARD_SNIPET_TXT,null,"DROP TABLE ${name} IF EXISTS;", null) },
             {"DROP TABLE ${name};",(STANDARD_SNIPET_TXT,null,"DROP TABLE ${name};", null) },
             {"GROOM TABLE ${name} VERSIONS;",(STANDARD_SNIPET_TXT,null,"GROOM TABLE ${name} VERSIONS;", null) },
             {"GROOM TABLE ${name} RECLAIM BACKUPSET NONE;",(STANDARD_SNIPET_TXT,null,"GROOM TABLE ${name} RECLAIM BACKUPSET NONE;", null) },
             {"@mysessions",(STANDARD_SNIPET_TXT,null,
             """
                SELECT * FROM _V_SESSION WHERE USERNAME = USER@",@"@@let __Let $snpt_date_id1=20201220|$snpt_date_id2=20201031@",@"@@for __LetFor $snpt_date_id|20200131|202000229|20200331|20200430|20200531@",@"@@activequeries select
                    q.qs_planid
                    , q.qs_sessionid
                    , q.qs_clientid
                    , s.dbname
                    , s.username
                    , q.qs_cliipaddr
                    , q.qs_sql
                    , q.qs_state
                    , q.qs_tsubmit
                    , q.qs_tstart
                    , case when q.qs_tstart = 'epoch' then '0' else abstime 'now' - q.qs_tstart end as ELAPSED_SECS, initcap(q.qs_pritxt) AS PRIORYTY
                    , TRIM(TO_CHAR(ROUND(case when qs_estcost >= 0 then qs_estcost else 18446744073709551616 + qs_estcost end / 1000.0,0),'999 999 999')) AS ESTIMATED_SECS
                    , q.qs_estdisk / 1024 AS ESTIMATED_DISK_MB
                        , TRIM(TO_CHAR(ROUND(q.qs_estmem / 1024.0, 0), '999 999 999')) AS ESTIMATED_MEMORY_MB
                            , q.qs_snippets AS SNIPPETS
                , q.qs_cursnipt AS CURRENTSNIPET
                , q.qs_resrows AS RESOULTROWS
                , q.qs_resbytes AS RESOULTBYES
                from
                    _v_qrystat q,
                    _v_session s
                where q.qs_sessionid = s.id
             """
             , null) },

            {"@tableSizes",(STANDARD_SNIPET_TXT,null,
             """
             --SET CATALOG TEST;
             SELECT OBJID, TABLENAME, OWNER, 
                 CREATEDATE, RELNATTS, ALLOCATED_BYTES::bigint as ALLOCATED_BYTES, USED_BYTES::bigint as USED_BYTES, SKEW, CAST(NULL as NUMERIC) as ROW_COUNT, 
                 ALLOCATED_BLOCKS, USED_BLOCKS, BLOCK_SIZE, USED_MIN, USED_MAX, USED_AVG, 
                 RELDISTMETHOD, MATER_COUNT, MATER_BLOCKS, MATER_BYTES, MATER_OVERHEAD
             FROM _V_TABLE_STORAGE_STAT
             WHERE UPPER(OBJTYPE) = 'TABLE'
             ORDER BY  TABLENAME, OWNER;
            
             select RELNAME, RELREFS, RELTUPLES from _T_CLASS;
            
             SELECT o.OBJNAME as TABLENAME, o.OWNER, z.DSID, z.HWID, NULL as DATA_PART,
                 z.ALLOCATED_BLOCKS, z.USED_BLOCKS, z.ALLOCATED_BYTES::bigint as ALLOCATED_BYTES, z.USED_BYTES::bigint as USED_BYTES,
                         z.SORTED_BLOCKS, z.SORTED_BYTES
                 FROM _V_SYS_OBJECT_DSLICE_INFO z
                 join _v_object_data o on o.objid = z.tblid
             where o.objdb = current_db;@",@"@@deleted SET show_deleted_records = 1;
             select createxid,deletexid, * from YOURTABLENAME WHERE deletexid <> 0;
             SET show_deleted_records = 0;
             """
             , null) },
            {"@merge",(STANDARD_SNIPET_TXT,null,
             """
             MERGE INTO merge_demo1 AS A 
             using merge_demo2 AS B
             ON A.ID = B.ID
             WHEN MATCHED THEN
             UPDATE SET A.LastName = B.LastName
             WHEN NOT MATCHED THEN
             INSERT VALUES (B.ID, B.FirstName, B.LastName); 
             --https://dwgeek.com/netezza-merge-command-manipulate-records.html/
             """
             , null) },
            {"@window",(STANDARD_SNIPET_TXT,null,
             """
             https://www.ibm.com/support/knowledgecenter/SSTNZ3/com.ibm.ips.doc/postgresql/dbuser/c_dbuser_window_analytic_funcs.html@",@"@@window2 <partition_by_clause> = PARTITION BY <value_expression> [, ...]+
             <order_by_clause> = ORDER BY <value_expression> [asc | desc] [nulls 
             {first|last}] [, ...]+
             <frame_spec_clause> = <frame_extent> [<exclusion clause>]
             <frame_extent> = 
                 ROWS  UNBOUNDED PRECEDING
                 |ROWS  <constant> PRECEDING
                 |ROWS   CURRENT ROW
                 |RANGE  UNBOUNDED PRECEDING
                 |RANGE  <constant> PRECEDING
                 |RANGE  CURRENT ROW
                 |ROWS BETWEEN {UNBOUNDED PRECEDING| <constant> PRECEDING | CURRENT 
             ROW } AND { UNBOUNDED FOLLOWING | <constant>  FOLLOWING | CURRENT ROW }
                 |RANGE BETWEEN {UNBOUNDED PRECEDING| <constant> PRECEDING | CURRENT 
             ROW } AND { UNBOUNDED FOLLOWING | <constant>  FOLLOWING | CURRENT ROW } 
             <exclusion_clause> =  EXCLUDE CURRENT ROW | EXCLUDE TIES | EXCLUDE 
             GROUP | EXCLUDE  NO OTHERS
             """
             , null) },

            {"declare",(STANDARD_SNIPET_TXT,"declare variable","declare &${name} = ${value};${Caret}", "declare") },
            {"REGEXP_LIKE",(STANDARD_SNIPET_TXT,"REGEXP_LIKE","REGEXP_LIKE('${input}','${pattern}')${Caret}", "REGEXP_LIKE") },
            {"@export xlsb",(STANDARD_SNIPET_TXT,"export to excel file",$"@expXlsx: SELECT * FROM ${{tableName}} -> {desktopPath}\\${{fileName}}.xlsb${{Caret}};", "export") },
            {"@export csv",(STANDARD_SNIPET_TXT,"export to csv",$"@expCsv: SELECT * FROM ${{tableName}} -> {desktopPath}\\${{fileName}}.csv${{Caret}};", "export") },

            {"@export csv/parquet full", (STANDARD_SNIPET_TXT,"export to csv/parquet with options",
                $$"""
                expCsv: SELECT * FROM ${tableName} -> {{desktopPath}}\${fileName}.csv
                {
                #delimiter ${|}
                #lineDelimiter ${windows}
                #encoding ${UTF8}
                #compression ${zstd}
                #upFrontRowsCount true
                }${Caret};
                """
                ,"export")},

            {"SELECT", (STANDARD_SNIPET_TXT,"SELECT","SELECT","SELECT") },
            {"AS", (STANDARD_SNIPET_TXT,"AS","AS","AS") },
            {"WHERE", (STANDARD_SNIPET_TXT,"WHERE","WHERE","WHERE") },
            {"HAVING", (STANDARD_SNIPET_TXT,"HAVING","HAVING","HAVING") },
            {"FROM", (STANDARD_SNIPET_TXT,"FROM","FROM","FROM") },
            {"GROUP BY", (STANDARD_SNIPET_TXT,"GROUP BY","GROUP BY","GROUP BY") },
            {"ORDER BY", (STANDARD_SNIPET_TXT,"ORDER BY","ORDER BY","ORDER BY") },
            {"ROW_NUMBER()", (STANDARD_SNIPET_TXT,"ROW_NUMBER()","ROW_NUMBER()","ROW_NUMBER()") },
            {"PARTITION BY", (STANDARD_SNIPET_TXT,"PARTITION BY","PARTITION BY","PARTITION BY") },
            {"ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW", (STANDARD_SNIPET_TXT,"ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW","ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW","ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW") },
            {"UNBOUNDED", (STANDARD_SNIPET_TXT,"UNBOUNDED","UNBOUNDED","UNBOUNDED") },
            {"FOLLOWING", (STANDARD_SNIPET_TXT,"FOLLOWING","FOLLOWING","FOLLOWING") },
            {"LIKE", (STANDARD_SNIPET_TXT,"LIKE","LIKE","LIKE") },
            {"UPDATE", (STANDARD_SNIPET_TXT,"UPDATE","UPDATE","UPDATE") },
            {"DISTRIBUTE", (STANDARD_SNIPET_TXT,"DISTRIBUTE","DISTRIBUTE","DISTRIBUTE") },
            {"RANDOM", (STANDARD_SNIPET_TXT,"RANDOM","RANDOM","RANDOM") },
            {"SUBSTRING", (STANDARD_SNIPET_TXT,"SUBSTRING","SUBSTRING","SUBSTRING") },
            {"UNION ALL", (STANDARD_SNIPET_TXT,"UNION ALL","UNION ALL","UNION ALL") },
            {"ALL ", (STANDARD_SNIPET_TXT,"ALL ","ALL ","ALL ") },
            {"COMMIT", (STANDARD_SNIPET_TXT,"COMMIT","COMMIT","COMMIT") },
            {"UNBOUNDED FOLLOWING", (STANDARD_SNIPET_TXT,"UNBOUNDED FOLLOWING","UNBOUNDED FOLLOWING","UNBOUNDED FOLLOWING") },
            {"UNBOUNDED PRECEDING", (STANDARD_SNIPET_TXT,"UNBOUNDED PRECEDING","UNBOUNDED PRECEDING","UNBOUNDED PRECEDING") },
            {"PRECEDING", (STANDARD_SNIPET_TXT,"PRECEDING","PRECEDING","PRECEDING") },
            {"STRLEFT", (STANDARD_SNIPET_TXT,"STRLEFT","STRLEFT","STRLEFT") },
            {"STRRIGHT", (STANDARD_SNIPET_TXT,"STRRIGHT","STRRIGHT","STRRIGHT") },
            {"RENAME", (STANDARD_SNIPET_TXT,"RENAME","RENAME","RENAME") },
        };

        ResultRowsLimitWarning ??= ResultRowsLimit / 10;
    }
}


[JsonSerializable(typeof(AppOptions))]
public partial class MyJsonContextAppOptions : JsonSerializerContext
{
}

