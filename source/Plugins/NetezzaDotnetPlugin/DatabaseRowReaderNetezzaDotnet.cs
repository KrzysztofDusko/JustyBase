using System.Data.Common;
using System.Text;
using JustyBase.PluginCommon.Contracts;

namespace NetezzaDotnetPlugin;

public sealed class DatabaseRowReaderNetezzaDotnet : IDatabaseRowReader
{
    private readonly DbDataReader _reader;
    private readonly int _fieldCount;

    public DatabaseRowReaderNetezzaDotnet(DbDataReader reader)
    {
        _reader = reader;
        _fieldCount = reader.FieldCount;
    }

    public object?[] ReadOneRow()
    {
        var fields = new object?[_fieldCount];
        for (int i = 0; i < _fieldCount; ++i)
        {
            object o = _reader.GetValue(i);
            if (o is DBNull) // string pool is builtin in driver
            {
                fields[i] = null;
            }
            else
            {
                fields[i] = o;
            }
        }

        return fields;
    }
}
