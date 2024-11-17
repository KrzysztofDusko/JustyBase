using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace JustyBase.Tools.ImportHelpers;
public sealed class DBReaderWithMessages : DbDataReader
{
    private readonly DbDataReader _rdr;
    private readonly Action<long>? _messageAction;
    
    public DBReaderWithMessages(DbDataReader dataReader, Action<long>? messageAction = null)
    {
        _rdr = dataReader;
        _messageAction = messageAction;
    }

    public override DataTable? GetSchemaTable()
    {
        return _rdr.GetSchemaTable();
    }
    public override object this[int ordinal] => _rdr[ordinal];

    public override object this[string name] => _rdr[name];

    public override int Depth => _rdr.Depth;

    public override int FieldCount => _rdr.FieldCount;

    public override bool HasRows => _rdr.HasRows;

    public override bool IsClosed => _rdr.IsClosed;

    public override int RecordsAffected => _rdr.RecordsAffected;

    public override bool GetBoolean(int ordinal)
    {
        return _rdr.GetBoolean(ordinal);
    }

    public override byte GetByte(int ordinal)
    {
        return _rdr.GetByte(ordinal);
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
    {
        return _rdr.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    }

    public override char GetChar(int ordinal)
    {
        return _rdr.GetChar(ordinal);
    }

    public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
    {
        return _rdr.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
    }

    public override string GetDataTypeName(int ordinal)
    {
        return _rdr.GetDataTypeName(ordinal);
    }

    public override DateTime GetDateTime(int ordinal)
    {
        return _rdr.GetDateTime(ordinal);
    }

    public override decimal GetDecimal(int ordinal)
    {
        return _rdr.GetDecimal(ordinal);
    }

    public override double GetDouble(int ordinal)
    {
        return _rdr.GetDouble(ordinal);
    }

    public override IEnumerator GetEnumerator()
    {
        return _rdr.GetEnumerator();
    }

    public override Type GetFieldType(int ordinal)
    {
        return _rdr.GetFieldType(ordinal);
    }

    public override float GetFloat(int ordinal)
    {
        return _rdr.GetFloat(ordinal);
    }

    public override Guid GetGuid(int ordinal)
    {
        return _rdr.GetGuid(ordinal);
    }

    public override short GetInt16(int ordinal)
    {
        return _rdr.GetInt16(ordinal);
    }

    public override int GetInt32(int ordinal)
    {
        return _rdr.GetInt32(ordinal);
    }

    public override long GetInt64(int ordinal)
    {
        return _rdr.GetInt64(ordinal);
    }

    public override string GetName(int ordinal)
    {
        return _rdr.GetName(ordinal);
    }

    public override int GetOrdinal(string name)
    {
        return _rdr.GetOrdinal(name);
    }

    public override string GetString(int ordinal)
    {
        return _rdr.GetString(ordinal);
    }

    public override object GetValue(int ordinal)
    {
        return _rdr.GetValue(ordinal);
    }

    public override int GetValues(object[] values)
    {
        return _rdr.GetValues(values);
    }

    public override bool IsDBNull(int ordinal)
    {
        return _rdr.IsDBNull(ordinal);
    }

    public override bool NextResult()
    {
        return _rdr.NextResult();
    }

    long _lineNumber = 0;

    private Stopwatch? _lastMessageStopwatch;
    private readonly TimeSpan _messageInterval = TimeSpan.FromSeconds(5);
    public override bool Read()
    {
        if (_messageAction is not null)
        {
            _lastMessageStopwatch ??= Stopwatch.StartNew();
            if (++_lineNumber % 5_000 == 0 && _lastMessageStopwatch.Elapsed >= _messageInterval)
            {
                _messageAction.Invoke(_lineNumber);
                _lastMessageStopwatch.Restart();
            }
        }

        return _rdr.Read();
    }
}