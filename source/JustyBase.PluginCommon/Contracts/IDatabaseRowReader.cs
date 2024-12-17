namespace JustyBase.PluginCommon.Contracts;
public interface IDatabaseRowReader
{
    public object?[] ReadOneRow();
}