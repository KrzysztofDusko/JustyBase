namespace JustyBase.PluginDatabaseBase.Database;
public interface IDatabaseRowReader
{
    public object?[] ReadOneRow();
}