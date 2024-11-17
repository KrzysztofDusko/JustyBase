using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using System.Data;
using System.Diagnostics.CodeAnalysis;


namespace JustyBase.Tools.ImportHelpers.XML;

public sealed class DataReaderFromLines : IDataReader
{
    private readonly OneCellValue[][] _linesX;
    private readonly DatabaseTypeChooser _databaseTypeChooser;
    private int _currentRowNum = 0;
    private OneCellValue[] CurrentRow => _linesX[_currentRowNum];
    public DataReaderFromLines(OneCellValue[][] linesX, DatabaseTypeChooser databaseTypeChooser)
    {
        _linesX = linesX;
        _databaseTypeChooser = databaseTypeChooser;
    }

    public object this[int i] => _linesX[i];

    public object this[string name] => throw new NotImplementedException();

    public int Depth => throw new NotImplementedException();

    private bool _isClosed = false;
    public bool IsClosed => _isClosed;

    public int RecordsAffected => throw new NotImplementedException();

    public int FieldCount => CurrentRow?.Length ?? _linesX[0].Length; // becouse of null rows

    public void Close()
    {
        _isClosed = true;
    }

    public void Dispose()
    {
        Close();
    }

    public bool GetBoolean(int i)
    {
        return bool.Parse(CurrentRow[i].OriginalValue);
    }

    public byte GetByte(int i)
    {
        return byte.Parse(CurrentRow[i].OriginalValue);
    }

    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
    {
        throw new NotImplementedException();
    }

    public char GetChar(int i)
    {
        return char.Parse(CurrentRow[i].OriginalValue);
    }

    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
    {
        throw new NotImplementedException();
    }

    public IDataReader GetData(int i)
    {
        throw new NotImplementedException();
    }

    public string GetDataTypeName(int i)
    {
        return _databaseTypeChooser.GetNativeType(i).ToString();
    }

    public DateTime GetDateTime(int i)
    {
        return DateTime.Parse(CurrentRow[i].OriginalValue);
    }


    public decimal GetDecimal(int i)
    {
        return decimal.Parse(CurrentRow[i].OriginalValue, ImportEssentials.NumberExcelStyle, ImportEssentials.NUMBER_WITH_DOT_FORMAT);
    }

    public double GetDouble(int i)
    {
        return double.Parse(CurrentRow[i].OriginalValue, ImportEssentials.NumberExcelStyle, ImportEssentials.NUMBER_WITH_DOT_FORMAT);
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type GetFieldType(int i)
    {
        return _databaseTypeChooser.GetNativeType(i);
    }

    public float GetFloat(int i)
    {
        return float.Parse(CurrentRow[i].OriginalValue, ImportEssentials.NUMBER_WITH_DOT_FORMAT);
    }

    public Guid GetGuid(int i)
    {
        throw new NotImplementedException();
    }

    public short GetInt16(int i)
    {
        return short.Parse(CurrentRow[i].OriginalValue);
    }

    public int GetInt32(int i)
    {
        return int.Parse(CurrentRow[i].OriginalValue);
    }

    public long GetInt64(int i)
    {
        return long.Parse(CurrentRow[i].OriginalValue);
    }

    public string GetName(int i)
    {
        return _databaseTypeChooser.NormalizedColumnHeaderNames[i];
    }

    public int GetOrdinal(string name)
    {
        return Array.IndexOf(_databaseTypeChooser.NormalizedColumnHeaderNames, name);
    }

    public DataTable? GetSchemaTable()
    {
        return null;
    }

    public string GetString(int i)
    {
        if (CurrentRow is null)
        {
            return "";
        }
        if (_databaseTypeChooser.ColumnTypesBestMatch[i].DatabaseTypeSimple == DbSimpleType.Nvarchar)
        {
            //if prefered is text then use orignal value 2023/01 vs 2023-01-01
            return CurrentRow[i]?.OriginalValue ?? "";
        }
        else
        {
            //if prefered is not text use "optimized" text representation
            return CurrentRow[i]?.TypePreferedValue ?? "";
        }
    }

    public object GetValue(int i)
    {
        return _databaseTypeChooser.ColumnTypesBestMatch[i].DatabaseTypeSimple switch
        {
            DbSimpleType.Integer => GetInt64(i),
            DbSimpleType.Numeric => GetDecimal(i),
            DbSimpleType.Nvarchar => GetString(i),
            DbSimpleType.Date => GetData(i),
            DbSimpleType.TimeStamp => GetDateTime(i),
            DbSimpleType.NoInfo => GetString(i),
            DbSimpleType.Boolean => GetBoolean(i),
            _ => GetString(i),
        };
    }

    public int GetValues(object[] values)
    {
        for (int i = 0; i < CurrentRow.Length; i++)
        {
            values[i] = GetValue(i);
        }
        return values.Length;
    }

    public bool IsDBNull(int i)
    {
        return CurrentRow[i] is null;
    }

    public bool NextResult()
    {
        throw new NotImplementedException();
    }

    public bool Read()
    {
        if (_isClosed)
        {
            return false;
        }
        var res = ++_currentRowNum < _linesX.Length;
        if (!res)
        {
            _isClosed = true;
        }

        return res;
    }
}
