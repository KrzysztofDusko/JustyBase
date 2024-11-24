using CommunityToolkit.HighPerformance.Buffers;
using System.Data.Common;

namespace JustyBase.PluginDatabaseBase.Database;

public sealed class DatabaseRowReaderGeneral : IDatabaseRowReader
{
    private readonly StringPool[] _stringPool;
    private readonly DbDataReader _reader;
    private readonly int _fieldCount;
    private const int MAX_STR_LEN = 32;

    public DatabaseRowReaderGeneral(DbDataReader reader)
    {
        _reader = reader;
        _fieldCount = reader.FieldCount;

        _stringPool = new StringPool[_fieldCount];
        for (int i = 0; i < _fieldCount; i++)
        {
            _stringPool[i] = new StringPool();
        }
    }

    public object?[] ReadOneRow()
    {
        var fields = new object?[_fieldCount];
        for (int i = 0; i < _fieldCount; ++i)
        {
            object o = _reader.GetValue(i);
            if (o is string str && str.Length <= MAX_STR_LEN)
            {
                fields[i] = _stringPool[i].GetOrAdd(str);
            }
            else if (o is DBNull)
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
