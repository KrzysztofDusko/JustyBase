using JustyBase.PluginCommon.Enums;
using SpreadSheetTasks;
using System.Data;

namespace JustyBase.Common.Tools.ImportHelpers;

public sealed class DataReaderFromExcelReaderAbstract : IDataReader
{
    private readonly ExcelReaderAbstract _excelAbstractReader;
    private readonly DatabaseTypeChooser _databaseTypeChooser;
    private readonly bool _isCsvReader = false;
    private readonly CsvReader? _csvReader;//special case becouse of that excel cannot store decimals but Csv can..
    public DataReaderFromExcelReaderAbstract(ExcelReaderAbstract excelReader, DatabaseTypeChooser databaseTypeChooser)
    {
        ArgumentNullException.ThrowIfNull(excelReader, nameof(excelReader));
        ArgumentNullException.ThrowIfNull(databaseTypeChooser, nameof(databaseTypeChooser));
        _excelAbstractReader = excelReader;
        _isCsvReader = _excelAbstractReader is CsvReader;
        if (_isCsvReader)
        {
            _csvReader = _excelAbstractReader as CsvReader;
        }
        _databaseTypeChooser = databaseTypeChooser;
    }

    public object this[int i] => _excelAbstractReader.GetValue(i);

    public object this[string name] => throw new NotImplementedException();

    public int Depth => throw new NotImplementedException();

    private bool _isClosed = false;
    public bool IsClosed => _isClosed;

    public int RecordsAffected => throw new NotImplementedException();

    public int FieldCount => _excelAbstractReader.FieldCount;

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
        ref var w = ref _excelAbstractReader.GetNativeValue(i);
        if (w.type == ExcelDataType.Boolean)
        {
            return w.boolValue;
        }
        else if (w.type == ExcelDataType.Int64)
        {
            return w.int64Value == 1;
        }
        else if (w.type == ExcelDataType.Int32)
        {
            return w.int32Value == 1;
        }
        else
        {
            throw new InvalidCastException();
        }
    }

    public byte GetByte(int i)
    {
        return Convert.ToByte(_excelAbstractReader.GetValue(i));
    }

    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
    {
        throw new NotImplementedException();
    }

    public char GetChar(int i)
    {
        return Convert.ToChar(_excelAbstractReader.GetValue(i));
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
        return _excelAbstractReader.GetDateTime(i);
    }

    public decimal GetDecimal(int i)
    {
        if (_isCsvReader)//special case becouse of that excel connot store decimals but Csv can..
        {
            return _csvReader!.GetDecimal(i);
        }
        else
        {
            return (decimal)_excelAbstractReader.GetDouble(i);
        }
    }

    public double GetDouble(int i)
    {
        return _excelAbstractReader.GetDouble(i);
    }

    public Type GetFieldType(int i)
    {
        return _databaseTypeChooser.GetNativeType(i);
    }

    public float GetFloat(int i)
    {
        return (float)_excelAbstractReader.GetDouble(i);
    }

    public Guid GetGuid(int i)
    {
        throw new NotImplementedException();
    }

    public short GetInt16(int i)
    {
        return (short)_excelAbstractReader.GetInt32(i);
    }

    public int GetInt32(int i)
    {
        return _excelAbstractReader.GetInt32(i);
    }

    public long GetInt64(int i)
    {
        return _excelAbstractReader.GetInt64(i);
    }

    public string GetName(int i)
    {
        return _databaseTypeChooser!.NormalizedColumnHeaderNames![i];
    }

    public int GetOrdinal(string name)
    {
        return Array.IndexOf(_databaseTypeChooser!.NormalizedColumnHeaderNames!, name);
    }

    public DataTable? GetSchemaTable()
    {
        return null;
    }

    public string GetString(int i)
    {
        return _excelAbstractReader.GetString(i);
    }

    public object GetValue(int i)
    {
        return _databaseTypeChooser!.ColumnTypesBestMatch![i].DatabaseTypeSimple switch
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
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = GetValue(i);
        }
        return values.Length;
    }

    public bool IsDBNull(int i)
    {
        ref var valTmp = ref _excelAbstractReader.GetNativeValue(i);
        return valTmp.type == ExcelDataType.Null;
    }

    public bool NextResult()
    {
        throw new NotImplementedException();
    }

    public bool Read()
    {
        if (!_isClosed)
        {
            _isClosed = !_excelAbstractReader.Read();
        }
        return !_isClosed;
    }
}
