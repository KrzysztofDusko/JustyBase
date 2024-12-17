namespace JustyBase.PluginCommon.Enums;

[Flags]
public enum CurrentAutoCompletDatabaseMode
{
    NotSet = 0,
    DatabaseSchemaTable = 1,
    SchemaTable = 2,
    SchemaOptional = 4,
    DatabaseAndSchemaOptional = 8,
    MakeUpperCase = 16,// db -> DB if not typed with quotes "db"
    NullSchemaCanBeAccepted = 32,
};