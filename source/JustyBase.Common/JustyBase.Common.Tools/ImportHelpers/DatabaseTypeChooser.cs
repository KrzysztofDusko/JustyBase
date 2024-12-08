using JustyBase.PluginCommon;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.StringExtensions;
using SpreadSheetTasks;
using System.Diagnostics;

namespace JustyBase.Tools.ImportHelpers;

public sealed class DatabaseTypeChooser
{
    public string[]? NormalizedColumnHeaderNames { get; set; }
    public string[]? OriginalColumnHeaderNames { get; set; } // column headers not normalized
    public DbTypeWithSize[]? ColumnTypesBestMatch { get; set; }
    public const int DEFAULT_NVARCHAR_LENGTH = 255;

    private Trio[][]? _innerTypeArray;

    private int _fieldCount;
    public void InitTypes(int fieldCount)
    {
        _fieldCount = fieldCount;
        ColumnTypesBestMatch = new DbTypeWithSize[fieldCount];
        NormalizedColumnHeaderNames = new string[fieldCount];
        OriginalColumnHeaderNames = new string[fieldCount];
        _innerTypeArray = new Trio[fieldCount][];
        for (int i = 0; i < _innerTypeArray.Length; i++)
        {
            _innerTypeArray[i] = new Trio[7];// 7 data types
            _innerTypeArray[i][(int)DbSimpleType.Integer] = new Trio();
            _innerTypeArray[i][(int)DbSimpleType.Numeric] = new Trio();
            _innerTypeArray[i][(int)DbSimpleType.Nvarchar] = new Trio();
            _innerTypeArray[i][(int)DbSimpleType.Date] = new Trio();
            _innerTypeArray[i][(int)DbSimpleType.TimeStamp] = new Trio();
            _innerTypeArray[i][(int)DbSimpleType.NoInfo] = new Trio();
            _innerTypeArray[i][(int)DbSimpleType.Boolean] = new Trio();
        }
    }
    public void ChooseTypes(int textMargin = 5)
    {
        if (NormalizedColumnHeaderNames is null)
            throw new NullReferenceException("ColumnHeadersNames should be not null");
        
        if (OriginalColumnHeaderNames is null)
            throw new NullReferenceException("_originalColumnHeadersNames should be not null");
        
        if (ColumnTypesBestMatch is null)
            throw new NullReferenceException("ColumnTypesBestMatch should be not null");
        
        if (_innerTypeArray is null)
            throw new NullReferenceException("_innerTypeDictionaryHelper should be not null");
        
        for (int i = 0; i < _fieldCount; i++)
        {
            if (OriginalColumnHeaderNames[i].EndsWith("_#TEXT"))
            {
                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.Nvarchar)
                {
                    TextLength = Math.Max(DEFAULT_NVARCHAR_LENGTH, _innerTypeArray[0][(int)DbSimpleType.Nvarchar].LengthOrPrecision)
                };
                continue;
            }
            else if (OriginalColumnHeaderNames[i].EndsWith("_#NUMERIC"))
            {
                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.Numeric) { NumericPrecision = 20, NumericScale = 6 };
                continue;
            }
            else if (OriginalColumnHeaderNames[i].EndsWith("_#INTEGER"))
            {
                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.Integer);
                continue;
            }
            else if (OriginalColumnHeaderNames[i].EndsWith("_#DATE"))
            {
                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.Date);
                continue;
            }
            else if (OriginalColumnHeaderNames[i].EndsWith("_#TIMESTAMP"))
            {
                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.TimeStamp);
                continue;
            }

            var typesCountLevel1 = _innerTypeArray[i];

            Dictionary<DbSimpleType, Trio> dc = new Dictionary<DbSimpleType, Trio>();
            for (int j = 0; j < typesCountLevel1.Length; j++)
            {
                dc[(DbSimpleType)j] = typesCountLevel1[j];
            }

            var bestChoiceTemp = dc.Where(arg => arg.Key != DbSimpleType.NoInfo && arg.Value.HowManyTimes > 0);

            if (bestChoiceTemp is null)
            {
                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.Nvarchar) { TextLength = DEFAULT_NVARCHAR_LENGTH };
                continue;
            }

            // choose best suited type
            var bestChoice = bestChoiceTemp.OrderByDescending(arg => (arg.Value).HowManyTimes).FirstOrDefault();
            bool containNumeric = typesCountLevel1[(int)DbSimpleType.Numeric].HowManyTimes > 0;
            bool containNvarchar = typesCountLevel1[(int)DbSimpleType.Nvarchar].HowManyTimes > 0;
            bool containInteger = typesCountLevel1[(int)DbSimpleType.Integer].HowManyTimes > 0;
            bool containTimestamp = typesCountLevel1[(int)DbSimpleType.TimeStamp].HowManyTimes > 0;
            bool containBoolean = typesCountLevel1[(int)DbSimpleType.Boolean].HowManyTimes > 0;

            bool isTypeMix = ((containNumeric ? 1 : 0) + (containInteger ? 1 : 0) + (containTimestamp ? 1 : 0) + (containBoolean ? 1 : 0)) > 1;

            if (containNvarchar)
            {
                int proposedNumericLength = typesCountLevel1[(int)DbSimpleType.Nvarchar].LengthOrPrecision;
                if (containNumeric)
                {
                    int l = typesCountLevel1[(int)DbSimpleType.Numeric].LengthOrPrecision;
                    if (proposedNumericLength < l)
                    {
                        proposedNumericLength = l;
                    }
                }
                if (containInteger)
                {
                    int l = typesCountLevel1[(int)DbSimpleType.Integer].LengthOrPrecision;
                    if (proposedNumericLength < l)
                    {
                        proposedNumericLength = l;
                    }
                }
                if ((typesCountLevel1[(int)DbSimpleType.TimeStamp].HowManyTimes > 0 ||
                    typesCountLevel1[(int)DbSimpleType.Date].HowManyTimes > 0
                    ) && proposedNumericLength < 20)
                {
                    proposedNumericLength = 20;
                }

                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.Nvarchar) { TextLength = (proposedNumericLength == 1 ? 1 : proposedNumericLength + textMargin) };
            }
            else if (isTypeMix)
            {
                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.Nvarchar) { TextLength = 50 };
            }
            else if (containNumeric)
            {
                int a = typesCountLevel1[(int)DbSimpleType.Numeric].LengthOrPrecision;
                int b = typesCountLevel1[(int)DbSimpleType.Numeric].Scale;
                if (containInteger && typesCountLevel1[(int)DbSimpleType.Integer].LengthOrPrecision + b > a)
                {
                    a = typesCountLevel1[(int)DbSimpleType.Integer].LengthOrPrecision + b; // in column : 1,2,5.1,10 then 
                }
                if (a < b + 5)
                {
                    a = b + 5;
                }
                if (a < 10)
                {
                    a = 10;
                }
                if (containInteger && a < b + 16)
                {
                    a = b + 16;
                }
                int precision = (a > 38 ? 38 : a);
                int scale = (b > ImportEssentials.NumericPrecision ? ImportEssentials.NumericPrecision : b);
                ColumnTypesBestMatch[i] = new DbTypeWithSize(DbSimpleType.Numeric) { NumericPrecision = precision, NumericScale = scale };
            }
            else
            {
                ColumnTypesBestMatch[i] = bestChoice.Key switch
                {
                    DbSimpleType.Integer => new DbTypeWithSize(DbSimpleType.Integer),
                    DbSimpleType.Nvarchar => new DbTypeWithSize(DbSimpleType.Nvarchar) { TextLength = bestChoice.Value.LengthOrPrecision },
                    DbSimpleType.Numeric => new DbTypeWithSize(DbSimpleType.Numeric) { NumericPrecision = bestChoice.Value.LengthOrPrecision, NumericScale = bestChoice.Value.Scale },
                    DbSimpleType.Date => new DbTypeWithSize(DbSimpleType.Date),
                    DbSimpleType.TimeStamp => new DbTypeWithSize(DbSimpleType.TimeStamp),
                    DbSimpleType.Boolean => new DbTypeWithSize(DbSimpleType.Boolean),
                    _ => new DbTypeWithSize(DbSimpleType.Nvarchar) { TextLength = DEFAULT_NVARCHAR_LENGTH },
                };
            }
        }
    }

    public Type GetNativeType(int i)
    {
        if (ColumnTypesBestMatch is null)
            throw new NullReferenceException("ColumnTypesBestMatch is null");
        return ColumnTypesBestMatch[i].GetNativeType();
    }

    //XML specific
    public void HandleValueTextMode(ReadOnlySpan<char> val, int columnNumber, DbSimpleType dbSimpleType)
    {
        if (_innerTypeArray is null)
            throw new NullReferenceException("_innerTypeDictionaryHelper is null");

        var typesCountLevel2 = _innerTypeArray[columnNumber][(int)dbSimpleType];
        if (dbSimpleType == DbSimpleType.Numeric)
        {
            int dotPossition = val.IndexOf('.');
            if (dotPossition == -1)
            {
                dotPossition = val.Length;
            }

            if (typesCountLevel2.LengthOrPrecision < dotPossition + ImportEssentials.NumericPrecision)
            {
                typesCountLevel2.LengthOrPrecision = dotPossition + ImportEssentials.NumericPrecision;
            }

            typesCountLevel2.Scale = ImportEssentials.NumericPrecision;
        }

        if ((dbSimpleType == DbSimpleType.Nvarchar || dbSimpleType == DbSimpleType.Integer) && typesCountLevel2.LengthOrPrecision < val.Length)
        {
            typesCountLevel2.LengthOrPrecision = val.Length > 0 ? val.Length : 1;
        }

        typesCountLevel2.HowManyTimes++;
    }

    private void HandleExcelValue(ref FieldInfo nativeVal, int columnNumber)
    {
        if (_innerTypeArray is null)
            throw new NullReferenceException("_innerTypeDictionaryHelper is null");

        int minimumLength = -1;
        DbSimpleType dbSimpleTypeTmp;
        switch (nativeVal.type)
        {
            case ExcelDataType.Null:
                dbSimpleTypeTmp = DbSimpleType.NoInfo;
                break;
            case ExcelDataType.Int64:
                minimumLength = (int)Math.Floor(Math.Log10(Math.Abs(nativeVal.int64Value)) + 2); // +2 because of +/-
                dbSimpleTypeTmp = DbSimpleType.Integer;
                break;
            case ExcelDataType.Double:
                double tempD = nativeVal.doubleValue;
                minimumLength = (int)Math.Floor(Math.Log10(Math.Abs(tempD)) + 2) + ImportEssentials.NumericPrecision + 1;
                dbSimpleTypeTmp = DbSimpleType.Numeric;
                break;
            case ExcelDataType.DateTime:
                dbSimpleTypeTmp = DbSimpleType.TimeStamp;
                break;
            case ExcelDataType.String:
                dbSimpleTypeTmp = DbSimpleType.Nvarchar;
                minimumLength = nativeVal.strValue.Length;
                break;
            case ExcelDataType.Boolean:
                dbSimpleTypeTmp = DbSimpleType.Boolean;
                break;
            case ExcelDataType.Error:
                dbSimpleTypeTmp = DbSimpleType.NoInfo;
                break;
            case ExcelDataType.Int32: // ????
                dbSimpleTypeTmp = DbSimpleType.NoInfo;
                break;
            default:
                dbSimpleTypeTmp = DbSimpleType.NoInfo;
                break;
        }

        var typesCountLevel2 = _innerTypeArray[columnNumber][(int)dbSimpleTypeTmp];

        typesCountLevel2.HowManyTimes++;

        if (typesCountLevel2.LengthOrPrecision < minimumLength)
        {
            typesCountLevel2.LengthOrPrecision = minimumLength;
        }
        typesCountLevel2.Scale = ImportEssentials.NumericPrecision;//after dot
    }

    public long RowsCount = -1;
    public List<string[]> PreviewRows { get; set; } = new List<string[]>();
    public void ExcelTypeDetection(ExcelReaderAbstract excelDataReader, string sheetName, Action<string>? messageAction = null, long timeoutInSec = -1)
    {
        excelDataReader.ActualSheetName = sheetName;
        if (excelDataReader is not CsvReader)
        {
            excelDataReader.Read(); //skip headers 
        }
        int columnCount = excelDataReader.FieldCount;

        InitTypes(columnCount);
        if (NormalizedColumnHeaderNames is null)
            throw new NullReferenceException("ColumnHeadersNames is null");
        if (OriginalColumnHeaderNames is null)
            throw new NullReferenceException("OriginalColumnHeadersNames is null");
        if (ColumnTypesBestMatch is null)
            throw new NullReferenceException("ColumnTypesBestMatch is null");
        if (_innerTypeArray is null)
            throw new NullReferenceException("_innerTypeDictionaryHelper is null");

        for (int i = 0; i < _fieldCount; i++)
        {
            OriginalColumnHeaderNames[i] = excelDataReader.GetName(i);
            NormalizedColumnHeaderNames[i] = OriginalColumnHeaderNames[i].NormalizeDbColumnName();
        }
        
        var timestampBeforeLongLoop = Stopwatch.GetTimestamp();
        Stopwatch messageStopwatch = Stopwatch.StartNew();
        bool analyseIncomplete = false;
        if (excelDataReader is CsvReader csv)
        {
            csv.TransformValuesAutomaticly = false;
            while (csv.Read())
            {
                RowsCount++;
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    var nvarcharColumn = _innerTypeArray[columnIndex][(int)DbSimpleType.Nvarchar];
                    if (nvarcharColumn.HowManyTimes > 0 && nvarcharColumn.LengthOrPrecision >= 50)
                    {
                        int len = csv.GetSpanLength(columnIndex);
                        nvarcharColumn.HowManyTimes++;
                        nvarcharColumn.LengthOrPrecision = Math.Max(nvarcharColumn.LengthOrPrecision, len);
                    }
                    else
                    {
                        csv.TransFromSpanValue(columnIndex);
                        ref var nativeVal = ref csv.GetNativeValue(columnIndex);
                        HandleExcelValue(ref nativeVal, columnIndex);
                    }
                    if (RowsCount < 5)
                    {
                        if (columnIndex == 0)
                        {
                            PreviewRows.Add(new string[columnCount]);
                        }
                        PreviewRows[(int)RowsCount][columnIndex] = csv.GetString(columnIndex);
                    }
                }
                if (RowsCount > 0 && RowsCount % 50_000 == 0 && messageStopwatch.ElapsedMilliseconds > 1_000)
                {
                    messageAction?.Invoke($"{csv.RelativePositionInStream():P1} / ({RowsCount:N0} rows) analysed");
                    if (timeoutInSec != -1 && messageStopwatch.Elapsed.Seconds > timeoutInSec && messageStopwatch.Elapsed.Seconds >= 10)
                    {
                        messageAction?.Invoke($"analysed stopped ! (timout of {timeoutInSec:N0} sec)");
                        RowsCount = -1;
                        analyseIncomplete = true;
                        break;
                    }
                    messageStopwatch.Restart();
                }
            }
            csv.TransformValuesAutomaticly = true;
        }
        else
        {
            while (excelDataReader.Read())
            {
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    ref var nativeVal = ref excelDataReader.GetNativeValue(columnIndex);
                    HandleExcelValue(ref nativeVal, columnIndex);
                }
            }
        }

        var elapsed = Stopwatch.GetElapsedTime(timestampBeforeLongLoop).Milliseconds;
        messageAction?.Invoke($"type analysis took {elapsed} ms");
        ChooseTypes(analyseIncomplete ? 100 : 5);
        messageAction?.Invoke("--" + string.Join('|', ColumnTypesBestMatch.ToList()));
    }
}
internal sealed class Trio
{
    public int HowManyTimes;
    public int LengthOrPrecision;
    public int Scale;
}