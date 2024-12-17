namespace JustyBase.PluginDatabaseBase.Models;
public record NetezzaKeyItem(char KeyType, string PKDATABASE, string PKSCHEMA, string PKRELATION, List<(string colName, string referencedPkColName)> ColumnList, string UPDT_TYPE, string DEL_TYPE);
