using JustyBase.PluginCommon.Enums;
using SpreadSheetTasks;
using Sylvan.Data.Csv;
using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace JustyBase.Tools.ImportHelpers;

public sealed class CsvReader : ExcelReaderAbstract
{
    public string FilePath { get; set; }

    private CsvDataReader _csvReader;
    private StreamReader _streamReader;
    private readonly CompressionEnum _csvCompression = CompressionEnum.None;
    public CompressionEnum Compression => _csvCompression;
    public CsvReader(CompressionEnum csvCompression = CompressionEnum.None)
    {
        _csvCompression = csvCompression;
    }

    private FileStream _originalFileStream;
    public override void Open(string path, bool readSharedStrings = true, bool updateMode = false, Encoding? encoding = null)
    {
        if (_csvCompression == CompressionEnum.Brotli)
        {
            _originalFileStream = File.OpenRead(path);
            var br = new BrotliStream(new BufferedStream(_originalFileStream), CompressionMode.Decompress);
            _streamReader = new StreamReader(br);
        }
        else if (_csvCompression == CompressionEnum.Gzip)
        {
            _originalFileStream = File.OpenRead(path);
            var gz = new GZipStream(new BufferedStream(_originalFileStream), CompressionMode.Decompress);
            _streamReader = new StreamReader(gz);
        }
        else if (_csvCompression == CompressionEnum.Zstd)
        {
            _originalFileStream = File.OpenRead(path);
            var gz = new ZstdSharp.DecompressionStream(new BufferedStream(_originalFileStream));
            _streamReader = new StreamReader(gz);
        }
        else
        {
            _streamReader = new StreamReader(path);
        }

        _csvReader = CsvDataReader.Create(_streamReader);
        FilePath = path;

        FieldCount = _csvReader.FieldCount;
        innerRow = new FieldInfo[FieldCount];
        _decimalVals = new decimal[FieldCount];
        _isDecimalArray = new bool[FieldCount];
        for (int i = 0; i < FieldCount; i++)
        {
            innerRow[i].type = ExcelDataType.String;
            innerRow[i].strValue = _csvReader.GetName(i);
        }
    }
    public override string[] GetScheetNames()
    {
        return [Path.GetFileName(FilePath).Replace('.', '_')];
    }
    public bool TransformValuesAutomaticly { get; set; } = true;
    public override bool Read()
    {
        var innerReaderRead = _csvReader.Read();
        if (innerReaderRead && TransformValuesAutomaticly)
        {
            for (int i = 0; i < _csvReader.FieldCount; i++)
            {
                TransFromSpanValue(i);
            }
        }
        return innerReaderRead;
    }

    private bool[]? _isDecimalArray = null;
    private decimal[]? _decimalVals = null;

    private static bool IsTextColumnName(string columnName)
    {
        return columnName.Equals("Regon", StringComparison.OrdinalIgnoreCase) || columnName.Equals("Pesel", StringComparison.OrdinalIgnoreCase);
    }

    private readonly SearchValues<char> _searchValues = SearchValues.Create(",.E");

    public void TransFromSpanValue(int i)
    {
        var strVal = _csvReader.GetFieldSpan(i);
        innerRow[i].type = ExcelDataType.Null;
        if (strVal.Length == 0)
        {
            innerRow[i].type = ExcelDataType.Null;
        }
        else if (TreatAllColumnsAsText)
        {
            innerRow[i].type = ExcelDataType.String;
            innerRow[i].strValue = strVal.ToString();
        }
        else if (IsTextColumnName(_csvReader.GetName(i)))
        {
            innerRow[i].type = ExcelDataType.String;
            innerRow[i].strValue = strVal.ToString();
        }
        else if ((strVal[0] == '-' || Char.IsDigit(strVal[0])) && strVal.Length < 40 && strVal.ContainsAny(_searchValues)
                && (
                    decimal.TryParse(strVal, out decimal decimalRes) || 
                    decimal.TryParse(strVal, System.Globalization.NumberStyles.Any, ExcelReaderAbstract.invariantCultureInfo, out decimalRes)
                )
            )
        {
            innerRow[i].type = ExcelDataType.Double;
            innerRow[i].doubleValue = (double)decimalRes;//forLengthDetection in FullScanExcelReader
            _isDecimalArray[i] = true;
            _decimalVals[i] = decimalRes;
        }
        else if (strVal.Length < 20 && strVal[0] != '0' && Int64.TryParse(strVal, out Int64 int64Val))
        {
            innerRow[i].type = ExcelDataType.Int64;
            innerRow[i].int64Value = int64Val;
        }
        //else if (DateTime.TryParseExact(strVal, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime datetimeVal))
        else if (DateTime.TryParse(strVal, out var datetimeVal))
        {
            innerRow[i].type = ExcelDataType.DateTime;
            innerRow[i].dtValue = datetimeVal;
        }
        else if (bool.TryParse(strVal, out bool boolVal))
        {
            innerRow[i].type = ExcelDataType.Boolean;
            innerRow[i].boolValue = boolVal;
        }
        else
        {
            innerRow[i].type = ExcelDataType.String;
            innerRow[i].strValue = strVal.ToString();
        }
    }

    public override string GetString(int i)
    {
        ref var w = ref innerRow[i];
        if (w.type == ExcelDataType.String)
        {
            return w.strValue;
        }
        else
        {
            return _csvReader.GetFieldSpan(i).ToString();
        }
    }
    public int GetSpanLength(int i)
    {
        return _csvReader.GetFieldSpan(i).Length;
    }
    public decimal GetDecimal(int i) => _decimalVals[i];
    public bool IsDecimal(int i) => _isDecimalArray[i] == true;
    public override void Dispose()
    {
        _csvReader?.Dispose();
        _streamReader.Dispose();
    }
    public override double RelativePositionInStream()
    {
        if (_streamReader.BaseStream.CanSeek)
        {
            return (double)_streamReader.BaseStream.Position / _streamReader.BaseStream.Length;
        }
        if (_csvCompression != CompressionEnum.None)
        {
            return (double)_originalFileStream.Position / _originalFileStream.Length;
        }
        return 0.5;
    }
}

