using System;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using JustyBase.PluginCommon.Enums;

namespace JustyBase.Common.Tools;

public sealed class AdvancedExportOptions
{
    //public ExportOptions Type { get; set; }
    //public string Path { get; set; }
    public char Delimiter { get; set; } = ';';
    public string LineDelimiter { get; set; } = "\n";
    //public string NullValue { get; set; }
    public bool Header { get; set; } = true;
    public Encoding Encod { get; set; } = Encoding.UTF8;
    public CompressionEnum CompresionType { get; set; } = CompressionEnum.None;
    //public string TabName { get; set; }
    //public string PivotTableTabName { get; set; }
    //public string PivotTableName { get; set; }
    //public bool PrintHeaders { get; set; }
    //public string StartCell { get; set; }
    //public bool ForceRefresh { get; set; }
    //public bool Clear { get; set; }

    private readonly static Regex _rxOptions = new Regex(@"\s*#(?<optionName>(type|path|delimiter|lineDelimiter|nullValue|header|encoding|compression|tabname|pivotTableTabName|pivotTableName|printHeaders|startCell|forceRefresh|clear))\s(?<optionValue>.*)\s*");

    public static AdvancedExportOptions ParseFromString(string optionsString)
    {
        AdvancedExportOptions opt = new AdvancedExportOptions()
        {
            Delimiter = '|',
            LineDelimiter = "\n",
            Encod = Encoding.UTF8,
            Header = true
        };

        if (!string.IsNullOrWhiteSpace(optionsString))
        {
            string[] optionlines = optionsString.Split(Environment.NewLine);
            for (int j = 0; j < optionlines.Length; j++)
            {
                var match = _rxOptions.Match(optionlines[j]);
                if (match.Success)
                {
                    string optionName = match.Groups["optionName"].Value;
                    string optionValue = match.Groups["optionValue"].Value;
                    if (optionName.Equals("delimiter", StringComparison.OrdinalIgnoreCase))
                    {
                        if (optionValue == "semicolon")
                        {
                            opt.Delimiter = ';';
                        }
                        else if (optionValue.Length >= 2 && optionValue[0] == '\'')
                        {
                            opt.Delimiter = optionValue[1];
                        }
                        else
                        {
                            opt.Delimiter = optionValue[0];
                        }

                    }
                    if (optionName.Equals("header", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.Header = optionValue.Trim() == "true";
                    }
                    if (optionName.Equals("encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.Encod = ParseEnconding(optionValue.Trim());
                    }
                    if (optionName.Equals("LineDelimiter", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.LineDelimiter = optionValue.Trim() == "windows" ? Environment.NewLine : "\n";
                    }
                    if (optionName.Equals("compression", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.CompresionType = optionValue.Trim().GetCsvCompressionEnum();
                    }
                }
            }
        }

        return opt;
    }

    private static UTF8Encoding? _encodingCache;
    private static UTF8Encoding Utf8EncWithoutBM => _encodingCache ??= new UTF8Encoding(false);
    public static Encoding ParseEnconding(string encodingStr)
    {
        if (encodingStr == "ASCII")
        {
            return Encoding.ASCII;
        }
        else if (encodingStr == "UTF8" || encodingStr == "UTF-8")
        {
            return Encoding.UTF8;
        }
        else if (encodingStr == "UTF8_BM" || encodingStr == "UTF-8_BM")
        {
            return Utf8EncWithoutBM;
        }
        else if (encodingStr == "UTF32" || encodingStr == "UTF-32")
        {
            return Encoding.UTF32;
        }
        else if (encodingStr == "Unicode" || encodingStr == "UTF16" || encodingStr == "UTF-16")
        {
            return Encoding.Unicode;
        }
        else if (encodingStr == "Latin1")
        {
            return Encoding.Latin1;
        }
        else if (encodingStr == "Default")
        {
            return Encoding.Default;
        }
        else if (encodingStr == "BigEndianUnicode")
        {
            return Encoding.BigEndianUnicode;
        }
        else
        {
            return Encoding.GetEncoding(encodingStr);
        }
    }
}

