using Parquet.Schema;
using Parquet;
using System.Data;

namespace JustyBase.Tools;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
public sealed class ParquetFileWritterFromDataReader
{
    private readonly IDataReader _rdr;
    private readonly int _groupSize = 32_768;
    public ParquetFileWritterFromDataReader(IDataReader rdr, int groupSize = 32_768)
    {
        _rdr = rdr;
        _groupSize = groupSize;
    }

    public async Task CreateFile(Stream fileStream)
    {
        int filedsCount = _rdr.FieldCount;
        DataField[] dataFields = new DataField[filedsCount];
        OneColumn[] columns = new OneColumn[filedsCount];

        for (int i = 0; i < dataFields.Length; i++)
        {
            var tpe = _rdr.GetFieldType(i);
            if (OneColumn.IsNotStringType(tpe))
            {
                dataFields[i] = new DataField(_rdr.GetName(i), tpe, isNullable: true);
            }
            else
            {
                dataFields[i] = new DataField(_rdr.GetName(i), typeof(string), isNullable: true);
            }
            columns[i] = new OneColumn(_groupSize, tpe);
        }

        var schema = new ParquetSchema(dataFields);

        using (ParquetWriter parquetWriter = await ParquetWriter.CreateAsync(schema, fileStream))
        {
            parquetWriter.CompressionMethod = CompressionMethod.Zstd;
            parquetWriter.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
            // create a new row group in the file
            int smallCounter = 0;
            while (_rdr.Read())
            {
                for (int i = 0; i < filedsCount; i++)
                {
                    columns[i].AddValue(_rdr, i, smallCounter);
                }
                smallCounter++;
                if (smallCounter == _groupSize)
                {
                    await WriteGroup(schema, parquetWriter, smallCounter);
                    smallCounter = 0;
                }
            }
            if (smallCounter > 0) // last inclompletgroup
            {
                await WriteGroup(schema, parquetWriter, smallCounter);
            }
        }

        async Task WriteGroup(ParquetSchema schema, ParquetWriter parquetWriter, int smallCounter)
        {
            using (ParquetRowGroupWriter groupWriter = parquetWriter.CreateRowGroup())
            {
                for (int i = 0; i < filedsCount; i++)
                {
                    Parquet.Data.DataColumn column = new Parquet.Data.DataColumn(schema.DataFields[i], columns[i].GetArray(smallCounter));
                    await groupWriter.WriteColumnAsync(column);
                }
            }
        }
    }
}

internal sealed class OneColumn
{
    private Type _columnType;
    private int _groupSize;
    private string?[]? stringValues;
    private int?[]? intValues;
    private long?[]? longValues;
    private decimal?[]? decimalValues;
    private float?[]? floatValues;
    private double?[]? doubleValues;
    private DateTime?[]? dateTimeValues;
    private bool?[]? boolValues;

    public OneColumn(int groupSize, Type columnType)
    {
        _groupSize = groupSize;
        _columnType = columnType;
        if (columnType == typeof(int))
        {
            intValues = new int?[groupSize];
        }
        else if (columnType == typeof(long))
        {
            longValues = new long?[groupSize];
        }
        else if (columnType == typeof(decimal))
        {
            decimalValues = new decimal?[groupSize];
        }
        else if (columnType == typeof(float))
        {
            floatValues = new float?[groupSize];
        }
        else if (columnType == typeof(double))
        {
            doubleValues = new double?[groupSize];
        }
        else if (columnType == typeof(DateTime))
        {
            dateTimeValues = new DateTime?[groupSize];
        }
        else if (columnType == typeof(bool))
        {
            boolValues = new bool?[groupSize];
        }
        else
        {
            stringValues = new string?[groupSize];
        }
    }
    public static bool IsNotStringType(Type tpe)
    {
        return tpe == typeof(int) || tpe == typeof(long) || tpe == typeof(decimal) || tpe == typeof(float) ||
            tpe == typeof(double) || tpe == typeof(DateTime) || tpe == typeof(bool);
    }
    public void AddValue(IDataReader rdr, int readerColumnIndex, int smallCounter)
    {
        if (rdr.IsDBNull(readerColumnIndex))
        {
            if (_columnType == typeof(int))
            {
                intValues[smallCounter] = null;
            }
            else if (_columnType == typeof(long))
            {
                longValues[smallCounter] = null;
            }
            else if (_columnType == typeof(decimal))
            {
                decimalValues[smallCounter] = null;
            }
            else if (_columnType == typeof(float))
            {
                floatValues[smallCounter] = null;
            }
            else if (_columnType == typeof(double))
            {
                doubleValues[smallCounter] = null;
            }
            else if (_columnType == typeof(DateTime))
            {
                dateTimeValues[smallCounter] = null;
            }
            else if (_columnType == typeof(bool))
            {
                boolValues[smallCounter] = null;
            }
            else if (_columnType == typeof(string))
            {
                stringValues[smallCounter] = null;
            }
            else if (_columnType == typeof(Memory<byte>))
            {
                stringValues[smallCounter] = null;
            }
            else
            {
                stringValues[smallCounter] = null;
            }
        }
        else
        {
            if (_columnType == typeof(int))
            {
                intValues[smallCounter] = rdr.GetInt32(readerColumnIndex);
            }
            else if (_columnType == typeof(long))
            {
                longValues[smallCounter] = rdr.GetInt64(readerColumnIndex);
            }
            else if (_columnType == typeof(decimal))
            {
                decimalValues[smallCounter] = rdr.GetDecimal(readerColumnIndex);
            }
            else if (_columnType == typeof(float))
            {
                floatValues[smallCounter] = rdr.GetFloat(readerColumnIndex);
            }
            else if (_columnType == typeof(double))
            {
                doubleValues[smallCounter] = rdr.GetDouble(readerColumnIndex);
            }
            else if (_columnType == typeof(DateTime))
            {
                dateTimeValues[smallCounter] = rdr.GetDateTime(readerColumnIndex);
            }
            else if (_columnType == typeof(bool))
            {
                boolValues[smallCounter] = rdr.GetBoolean(readerColumnIndex);
            }
            else if (_columnType == typeof(string))
            {
                stringValues[smallCounter] = rdr.GetString(readerColumnIndex);
            }
            else if (_columnType == typeof(Memory<byte>))
            {
                stringValues[smallCounter] = rdr.GetString(readerColumnIndex);
            }
            else
            {
                stringValues[smallCounter] = rdr.GetValue(readerColumnIndex).ToString();
            }
        }
    }

    public Array GetArray(int requestedSize)
    {
        if (_columnType == typeof(int))
        {
            if (intValues.Length != requestedSize)
            {
                Array.Resize(ref intValues, requestedSize);
            }
            return intValues;
        }
        if (_columnType == typeof(long))
        {
            if (longValues.Length != requestedSize)
            {
                Array.Resize(ref longValues, requestedSize);
            }
            return longValues;
        }
        if (_columnType == typeof(decimal))
        {
            if (decimalValues.Length != requestedSize)
            {
                Array.Resize(ref decimalValues, requestedSize);
            }
            return decimalValues;
        }
        if (_columnType == typeof(float))
        {
            if (floatValues.Length != requestedSize)
            {
                Array.Resize(ref floatValues, requestedSize);
            }
            return floatValues;
        }
        if (_columnType == typeof(double))
        {
            if (doubleValues.Length != requestedSize)
            {
                Array.Resize(ref doubleValues, requestedSize);
            }
            return doubleValues;
        }
        if (_columnType == typeof(DateTime))
        {
            if (dateTimeValues.Length != requestedSize)
            {
                Array.Resize(ref dateTimeValues, requestedSize);
            }
            return dateTimeValues;
        }
        if (_columnType == typeof(bool))
        {
            if (boolValues.Length != requestedSize)
            {
                Array.Resize(ref boolValues, requestedSize);
            }
            return boolValues;
        }
        else
        {
            if (stringValues.Length != requestedSize)
            {
                Array.Resize(ref stringValues, requestedSize);
            }
            return stringValues;
        }
    }

}


#pragma warning restore CS8602 // Dereference of a possibly null reference.
