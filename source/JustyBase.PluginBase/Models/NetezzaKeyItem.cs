namespace JustyBase.PluginDatabaseBase.Database;
public record NetezzaKeyItem(char KeyType, string PKDATABASE, string PKSCHEMA, string PKRELATION, List<(string colName, string referencedPkColName)> columnList, string UPDT_TYPE, string DEL_TYPE);
