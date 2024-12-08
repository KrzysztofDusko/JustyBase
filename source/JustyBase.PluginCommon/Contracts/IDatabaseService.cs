using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Enums;
using JustyBase.PluginDatabaseBase.Models;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace JustyBase.PluginDatabaseBase.Database;
public interface IDatabaseService : IDatabaseWithSpecificImportService
{
    public const DatabaseTypeEnum WHO_I_AM_CONST = DatabaseTypeEnum.NotSupportedDatabase;
    string Database { get; set; }
    string Ip { get; set; }
    string Name { get; set; }
    string Password { get; set; }
    string Port { get; set; }
    string Username { get; set; }
    string TempDataDirectory { get; set; }
    public ISimpleLogger Logger { get; set; }
    DbConnection Connection { get; }
    Action<string> DbMessageAction { get; set; }
    CurrentAutoCompletDatabaseMode AutoCompletDatabaseMode { get; init; }
    DatabaseConnectedLevel ConnectedLevel { get; set; }
    Task CacheAllObjects(TypeInDatabaseEnum[] typeInDatabaseArr, string databaseName = "", string procedureName = "");
    void ChangeDatabaseSpecial(DbConnection con, string databaseName);
    string ChangeDatabaseIfNeeded(DbConnection con, string selectedDatabaseName);
    DbCommand CreateCommandFromConnection(DbConnection con);
    IEnumerable<(DatabaseObject dbObject, string schema)> FindDbObject(string database, string schema, string name, bool cleanNames);
    string GetAddComment(string table, string database, string schema);
    string GetCheckDistributeText(string database, string schema, string tableName);
    IEnumerable<DatabaseColumn> GetColumns(string database, string schema, string table, string filter);
    IEnumerable<(DatabaseColumn, DatabaseObject)> GetColumnsFromAllTablesAndSchemas(string database, string schema);
    DbConnection GetConnection(string? databaseName, bool pooling = true, bool forSchema = false);
    ValueTask<string> GetCreateExternalText(string database, string schema, string tableName);
    ValueTask GetCreateExternalTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string tableName);
    string GetCreateFromCode(string fullName);
    string GetCreateProcedureCall(string database, string schema, string tableName);
    string GetCreateProcedurePatternText();
    ValueTask<string> GetCreateProcedureText(string database, string schema, string procedureName, bool forceFreshCode = false);
    ValueTask GetCreateProcedureTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string tableName, bool forceFreshCode = false);
    string GetCreateSequencePatternText();
    string GetCreateSynonymPatternText();
    ValueTask<string> GetCreateSynonymText(string database, string schema, string synonymName);
    ValueTask GetCreateSynonymTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string synonymName);
    ValueTask<string> GetCreateTableText(string database, string schema, string tableName, string? overrideTableName = null, string? middleCode = null, string? endingCode = null, List<string>? distOverride = null);
    ValueTask GetCreateTableTextStringBuilder(StringBuilder sb, string database, string schema, string tableName, string? overrideTableName = null, string? middleCode = null, string? endingCode = null, List<string>? distOverride = null);
    ValueTask<string> GetCreateViewText(string database, string schema, string tableName);
    ValueTask GetCreateViewTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string tableName);
    IEnumerable<string> GetDatabases(string filter);
    IEnumerable<DatabaseObject> GetDbObjects(string database, string schema, string filter, TypeInDatabaseEnum typeInDatabase);
    string GetDeleted(string table, string database, string schema);
    string GetDrop(string table, string database, string schema);
    string GetDuplicates(string table, string database, string schema);
    string GetEmpty(string table, string database, string schema);
    string GetExport(string table, string database, string schema);
    string GetGenerateStats(string database, string schema, string table);
    string GetGrant(string database, string schema, string table);
    string GetGroom(string database, string schema, string table);
    string GetImport(string table, string database, string schema);
    string GetKeyCodeText(string database, string schema, string tableName);
    string GetKeyUiqueCodeText(string database, string schema, string tableName);
    string GetOrganize(string database, string schema, string table);
    ValueTask<List<PorcedureCachedInfo>> GetProceduresSignaturesFromName(string database, string schema, string procName);
    ValueTask<string> GetReCreateTableText(string database, string schema, string tableName);
    ValueTask GetReCreateTableTextStringBuilder(StringBuilder stringBuilder, string database, string schema, string tableName);
    IEnumerable<string> GetSchemas(string database, string filter);
    string GetShortSelectCode(string fullName);
    string GetTableDropCode(string fullName);
    string GetTableRenameCode(string fullName);
    string GetTop100Select(string database, string schema, string table, bool snippetMode, bool addWhereToTextCols = false);
    string GetTop100SelectNumberFromTables(string database, string schema, IEnumerable<DatabaseObject> tables);
    string GetTop100SelectTextFromTables(string database, string schema, IEnumerable<DatabaseObject> tables);
    (int position, int length) HanleExceptions(ReadOnlySpan<char> sqlText, Exception exception);
    bool IsItemSourceContains(TypeInDatabaseEnum typeInDatabase, string database, string schema, string itemNameOrSignature, int procedureId, StringComparison comp, string searchWord, Regex rx);
    bool IsTypeInDatabaseSupported(TypeInDatabaseEnum tpe);
    string QuoteNameIfNeeded(string word);
    //int WaitDbToSynced();//hack??
    void CacheMainDictionary();
    void ClearCachedData();
    IDatabaseRowReader GetDatabaseRowReader(DbDataReader reader);
    string CleanSqlWord(string word, CurrentAutoCompletDatabaseMode autoCompletMode);
}


