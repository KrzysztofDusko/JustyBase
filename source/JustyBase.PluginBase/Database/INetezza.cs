using JustyBase.PluginDatabaseBase.Models;
using System.Data.Common;

namespace JustyBase.PluginDatabaseBase.Database;

public interface INetezza
{
    Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> DistributionDictionary { get; }
    Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> OrganizeDictionary { get; }
    Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, NetezzaKeyItem>>>> KeysDictionary { get; }
    void FillDistInfoForDatabase(string databaseName, DbConnection? dbConnection = null);
    void FillKeysInfoForDatabase(string databaseName, DbConnection? dbConnection = null);
    void ReadExternalTable(string database, DbDataReader rdr);
    void ClearExternalTableCache();
    string GetExternalDataObject(string database, string schema, string itemNameOrSignature);
    string GetCreateFluidSample(string database, string schema, string tableName);
    string? NetezzazProcWrongReturnFix(string? procReturns);
}
