using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommons;
using System.Data;
using System.Globalization;
using System.Xml;

namespace JustyBase.Common.Tools.ImportHelpers.XML;

public sealed class DbXMLImportJob : DbImportJob, IDbXMLImportJob
{
    private OneCellValue[] _currentRow = [];

    public static string GetValueStringRepresentationWithType(out DbSimpleType proposedDbType, ReadOnlySpan<char> stringValue, bool dataTypeAdnotation = true, string textQualifier = "'")
    {
        if (stringValue.Length == 0 || stringValue.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            proposedDbType = DbSimpleType.NoInfo;
            return "";
        }

        bool integerTest = int.TryParse(stringValue, ImportEssentials.NumberExcelStyle, CultureInfo.CurrentCulture, out var intNumber);

        bool decimalTest = decimal.TryParse(stringValue, ImportEssentials.NumberExcelStyle, CultureInfo.CurrentCulture, out var decimalNumber);
        if (!decimalTest)
        {
            decimalTest = decimal.TryParse(stringValue, ImportEssentials.NumberExcelStyle, _cultureUS, out decimalNumber);
        }

        if (integerTest && (int)decimalNumber == intNumber)//"simple" number
        {
            if (stringValue[0] == '0' && stringValue.Length > 1) // 0123456
            {
                proposedDbType = DbSimpleType.Nvarchar;
                return GetTextQualifiedString(stringValue, textQualifier);
            }
            else
            {
                proposedDbType = DbSimpleType.Integer;
                return intNumber.ToString();
            }
        }

        if (decimalTest && stringValue.Length >= 9 && !stringValue.ContainsAnyExceptInRange('0', '9'))//REGON, IBAN, etc.
        {
            proposedDbType = DbSimpleType.Nvarchar;
            return GetTextQualifiedString(stringValue, textQualifier);
        }
        else if (decimalTest)//"simple" number
        {
            proposedDbType = DbSimpleType.Numeric;
            return Math.Round(decimalNumber, ImportEssentials.NumericPrecision).ToString(ImportEssentials.NUMBER_WITH_DOT_FORMAT);
        }

        if (stringValue[^1] == '%')
        {
            decimalTest = decimal.TryParse(stringValue[..^1], ImportEssentials.NumberExcelStyle, CultureInfo.CurrentCulture, out decimalNumber);
            if (!decimalTest)
            {
                decimalTest = decimal.TryParse(stringValue, ImportEssentials.NumberExcelStyle, _cultureUS, out decimalNumber);
            }
            if (decimalTest)
            {
                proposedDbType = DbSimpleType.Numeric;
                return Math.Round(decimalNumber * 0.01m, ImportEssentials.NumericPrecision).ToString(ImportEssentials.NUMBER_WITH_DOT_FORMAT);
            }
        }

        bool dataTimeTest = DateTime.TryParse(stringValue, out var dateTimeValue);
        if (!dataTimeTest)
        {
            dataTimeTest = DateTime.TryParse(stringValue, _cultureUS, DateTimeStyles.None, out dateTimeValue);
        }

        if (dataTimeTest)
        {
            proposedDbType = DbSimpleType.TimeStamp;
            string type = dataTypeAdnotation ? "timestamp " : "";
            return $"{type}{GetTextQualifiedString(dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss"), textQualifier)}";
        }
        else
        {
            proposedDbType = DbSimpleType.Nvarchar;
            return GetTextQualifiedString(stringValue, textQualifier);
        }
    }

    private static string GetTextQualifiedString(ReadOnlySpan<char> text, string textQualifier)
    {
        if (textQualifier.Length == 0)
        {
            return text.ToString();
        }
        else
        {
            return $"{textQualifier}{text}{textQualifier}";
        }
    }

    public void SetTypedValue(int columnNumber, bool isBoolean = false)
    {
        DbSimpleType nz;
        string val;
        if (isBoolean)
        {
            nz = DbSimpleType.Boolean;
            val = _currentRow[columnNumber].OriginalValue;
        }
        else
        {
            val = GetValueStringRepresentationWithType(out nz, _currentRow[columnNumber].OriginalValue, dataTypeAdnotation: false, textQualifier: "");
        }

        if (nz == DbSimpleType.Integer && _currentRow[columnNumber].OriginalValue.Trim().Length == 11 && _columnHeadersNames[columnNumber].Contains("PESEL", StringComparison.OrdinalIgnoreCase))
        {
            nz = DbSimpleType.Nvarchar;
            val = _currentRow[columnNumber].OriginalValue;
        }
        _currentRow[columnNumber].TypePreferedValue = val;


        _databaseTypeChoser.HandleValueTextMode(val, columnNumber, nz);

    }

    public async Task AnalyzeXmlClipboardDataAndStoreLines(object someData, Action<string>? messageAction = null)
    {
        XmlTextReader reader;
        if (someData is byte[] xmlBytes)
        {
            MemoryStream ms = new MemoryStream(xmlBytes);
            reader = new XmlTextReader(ms);
        }
        else if (someData is XmlTextReader xmlTextReader)
        {
            reader = xmlTextReader;
        }
        else
        {
            return;
        }

        messageAction?.Invoke("clipboard analyze stared");
        await Task.Run(() =>
        {
            int actInd = -1;
            int cellNum = 0;
            int dataNum = 0;
            int colNum = 0;
            int rowNum = 0;

            while (reader.Read())//reader.MoveToNextAttribute() ||
            {
                if (reader.NodeType == XmlNodeType.Whitespace)
                {
                    continue;
                }
                else if (reader.NodeType == XmlNodeType.Element)
                {
                    int actRow = rowNum;
                    if (reader.Name == "Cell")
                    {
                        cellNum++;
                        if (reader.HasAttributes)
                        {
                            string? indS = reader.GetAttribute("ss:Index");
                            if (!string.IsNullOrEmpty(indS))
                            {
                                actInd = int.Parse(indS) - 1;// xml has indexes from 1
                            }
                            else
                            {
                                actInd = -1;
                            }
                        }
                        else
                        {
                            actInd = -1;
                        }
                    }
                    else if (reader.Name == "Data")
                    {
                        dataNum++;
                        if (cellNum > dataNum)
                        {
                            colNum += cellNum - dataNum; //cell wihout data situation =  <Cell />
                            cellNum = dataNum;
                        }
                        string? typeTxt = reader.GetAttribute("ss:Type");
                        if (typeTxt is not null && typeTxt == "Boolean")
                        {

                        }

                        string val = reader.ReadString();

                        if (rowNum == 0)
                        {
                            var ocv = new OneCellValue
                            {
                                OriginalValue = val,
                                TypePreferedValue = val
                            };
                            _currentRow[colNum] = ocv;
                            colNum++;
                        }
                        else
                        {
                            if (actInd != -1 && actRow == rowNum)
                            {
                                colNum = actInd;
                            }
                            var ocv = new OneCellValue
                            {
                                OriginalValue = val
                            };
                            _currentRow[colNum] = ocv;
                            if (typeTxt == "Boolean")
                            {
                                ocv.OriginalValue = val == "0" ? "False" : "True";
                                SetTypedValue(colNum, isBoolean: true);
                            }
                            else
                            {
                                SetTypedValue(colNum);
                            }
                            colNum++;
                        }
                    }
                    else if (reader.Name == "Table")
                    {
                        for (int i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            if (reader.Name == "ss:ExpandedColumnCount")
                            {
                                int expandedColumnCount = int.Parse(reader.Value);
                                _currentRow = new OneCellValue[expandedColumnCount];
                                _databaseTypeChoser.InitTypes(expandedColumnCount);

                                _columnHeadersNames = new string[expandedColumnCount];
                            }
                            else if (reader.Name == "ss:ExpandedRowCount")
                            {
                                //lines = new string[Int32.Parse(reader.Value)];
                                _linesX = new OneCellValue[int.Parse(reader.Value)][];
                            }
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Row")
                {
                    cellNum = 0;
                    dataNum = 0;
                    _linesX![rowNum++] = _currentRow;
                    _currentRow = new OneCellValue[_currentRow.Length];
                    colNum = 0;
                    if (rowNum == 1)//headers
                    {
                        _columnHeadersNames = _linesX[0].Select(arg => (arg?.OriginalValue ?? StringExtension.RandomSuffix("COL_")).NormalizeDbColumnName()).ToArray();
                        StringExtension.DeDuplicate(_columnHeadersNames);
                    }
                    if (rowNum % 100_000 == 0 || rowNum == _linesX.Length - 1)
                    {
                        messageAction?.Invoke($"analyzed {rowNum:N0} rows");
                    }
                }
            }
        });

        _databaseTypeChoser.NormalizedColumnHeaderNames = _columnHeadersNames;
        if (_columnHeadersNames is null)
            throw new Exception("_columnHeadersNames is not string[]");
        _databaseTypeChoser.OriginalColumnHeaderNames = (string[])_columnHeadersNames.Clone();
        _databaseTypeChoser.ChooseTypes();
        StringExtension.DeDuplicate(_columnHeadersNames);

        AsReader = new DataReaderFromLines(_linesX!, _databaseTypeChoser);
    }
}


