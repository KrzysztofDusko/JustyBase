using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Buffers;

namespace JustyBase.Helpers.Importers;

public static class NetezzaImportHelper
{
    private const char _columnSeparator = '\t';
    private const string _columnSeparatorString = "\t";
    private const char _deafultEscapeCharInExternal = '\\';
    private const string _deafultEscapeCharInExternalSTRING = "\\";

    private const string _escapedEscapeChar = "\\\\";
    private const string _escapedColumnSeparator = $"{_deafultEscapeCharInExternalSTRING}{_columnSeparatorString}";
    private const string _escapedNewLine = $"{_deafultEscapeCharInExternalSTRING}\n";
    private static readonly SearchValues<char> _valuesToEscape = SearchValues.Create([_deafultEscapeCharInExternal, _columnSeparator, '\n', '\r']);

    private const int _progressSize = 10_000;
    private static readonly TimeSpan _progressInterval = TimeSpan.FromSeconds(1);

    private const int _DEFAULT_COMMAND_TIMEOUT = 3_600;

    public static async Task NetezzaImportExecute(DbConnection conn, string tempDataDirectory, IDbImportJob importJob, 
        string tableName, Action<string>? progress, string remotesource = "DOTNET")
    {
        string serverName = $"JDE_{Path.GetRandomFileName()}";
        
        var headersWithDataType = importJob.ReturnHeadersWithDataTypes(DatabaseTypeEnum.NetezzaSQL);

        bool isLineReader = importJob is IDbXMLImportJob;
        var pipeServer = DBReaderStreamPipeServer(importJob.AsReader, serverName, progress, isLineReader, rowsCount: importJob.RowsCount);

        await Task.Delay(50);
        progress?.Invoke("transfer to database started");
        await Task.Run(() =>
        {
            try
            {
                using var cmd = conn.CreateCommand();
                
                cmd.CommandText = cmd.CommandText =
                $"""
                 CREATE TABLE {tableName} ({String.Join(',', headersWithDataType)})
                 DISTRIBUTE ON RANDOM;
                 """;

                cmd.ExecuteNonQuery();

                progress?.Invoke($" {tableName} created");
                cmd.CommandText = @$"INSERT INTO {tableName} SELECT * FROM EXTERNAL '\\.\pipe\{serverName}' ({String.Join(',', headersWithDataType)}) ";
                string sep2 = (_columnSeparator == '\t' ? "\\t" : _columnSeparator.ToString());
                cmd.CommandText += 
                $"""
                 USING(
                    REMOTESOURCE '{remotesource}'
                    DELIMITER '{sep2}'
                    SKIPROWS 1
                    NULLVALUE ''
                    ENCODING 'utf-8'
                    ESCAPECHAR '{_deafultEscapeCharInExternal}'
                    TIMESTYLE '24HOUR'
                    MAXERRORS 0
                    LOGDIR '{tempDataDirectory}'
                    );
                """;
                cmd.ExecuteNonQuery();

                var badFilePath = Directory.EnumerateFiles(tempDataDirectory, $"{tableName}*.nzbad").FirstOrDefault();
                if (badFilePath is not null)
                {
                    progress?.Invoke($"[ERROR] {badFilePath} created");
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"[ERROR] {ex.Message}");
                tableName = ex.Message;
            }
        });

        await pipeServer;
    }

    private static async Task DBReaderStreamPipeServer(IDataReader rdr, string serverName, Action<string>? messageAction, bool preparedStringsMode, long rowsCount = -1)
    {
        await Task.Run(() =>
        {
            var server = new NamedPipeServerStream(serverName);
            server.WaitForConnection();
            StreamWriter writer = new StreamWriter(server, Encoding.UTF8, 65_536);

            object[] row = new object[rdr.FieldCount];
            TypeCode[]? dataTypesCodes = null;

            if (!preparedStringsMode)
            {
                dataTypesCodes = new TypeCode[rdr.FieldCount];
                for (int j = 0; j < rdr.FieldCount; j++)
                {
                    row[j] = rdr.GetName(j);
                    dataTypesCodes[j] = Type.GetTypeCode(rdr.GetFieldType(j));
                }
            }

            writer.Write(String.Join(_columnSeparator, row));
            writer.Write('\n');
            writer.Flush();

            long progressLineNumber = 0;
            Span<char> spanBuffer = stackalloc char[64];
            Stopwatch sw = Stopwatch.StartNew();
            while (rdr.Read())
            {
                for (int i = 0; i < rdr.FieldCount; i++)
                {
                    if (!preparedStringsMode)
                    {
                        if (!rdr.IsDBNull(i) && dataTypesCodes is not null)
                        {
                            switch (dataTypesCodes[i])
                            {
                                case TypeCode.Empty:
                                case TypeCode.Object:
                                case TypeCode.DBNull:
                                    break;
                                case TypeCode.Boolean:
                                    var val1 = (rdr.GetBoolean(i) == true) ? 1 : 0;
                                    writer.Write(val1);
                                    break;
                                case TypeCode.Char:
                                case TypeCode.SByte:
                                case TypeCode.Byte:
                                case TypeCode.Int16:
                                case TypeCode.UInt16:
                                case TypeCode.Int32:
                                case TypeCode.UInt32:
                                case TypeCode.Int64:
                                case TypeCode.UInt64:
                                    writer.Write(rdr.GetInt64(i));
                                    break;
                                case TypeCode.Single:
                                    _ = (rdr.GetFloat(i)).TryFormat(spanBuffer, out int written, "F6", ImportEssentials.NUMBER_WITH_DOT_FORMAT);
                                    writer.Write(spanBuffer[..written]);
                                    break;
                                case TypeCode.Double:
                                    _ = (rdr.GetDouble(i)).TryFormat(spanBuffer, out int written2, "F6", ImportEssentials.NUMBER_WITH_DOT_FORMAT);
                                    writer.Write(spanBuffer[..written2]);
                                    break;
                                case TypeCode.Decimal:
                                    _ = (rdr.GetDecimal(i)).TryFormat(spanBuffer, out int written3, "F6", ImportEssentials.NUMBER_WITH_DOT_FORMAT);
                                    writer.Write(spanBuffer[..written3]);
                                    break;
                                case TypeCode.DateTime:
                                    _ = (rdr.GetDateTime(i)).TryFormat(spanBuffer, out int written4, "yyyy-MM-dd HH:mm:ss");
                                    writer.Write(spanBuffer[..written4]);
                                    break;
                                case TypeCode.String:
                                    var val = rdr.GetString(i);
                                    val = SanitizeStringValueInExternal(val as string);
                                    writer.Write(val);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        var val = rdr.GetString(i);
                        val = SanitizeStringValueInExternal(val as string);
                        writer.Write(val);
                    }

                    if (i < rdr.FieldCount - 1)
                    {
                        writer.Write(_columnSeparator);
                    }
                    else
                    {
                        writer.Write('\n');
                    }
                }
                writer.Flush();

                if (++progressLineNumber % _progressSize == 0 && sw.Elapsed > _progressInterval)
                {
                    if (rowsCount > 0)
                    {
                        messageAction?.Invoke($"{((double)progressLineNumber / rowsCount):P1} rows loaded");
                    }
                    else
                    {
                        messageAction?.Invoke($"{progressLineNumber:N0} rows loaded");
                    }
                    sw.Restart();
                }
            }
            writer.Flush();
            //server.Flush();

            server.Close();
            rdr.Close();
        });
    }

    /// <summary>
    /// remove/sanitize some "special" chars
    /// </summary>
    /// <param name="exteranlTableSeparator"></param>
    /// <param name="stringSep"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    private static string SanitizeStringValueInExternal(string val)
    {
        if (val is null)
        {
            return "";
        }
        if (val.AsSpan().IndexOfAny(_valuesToEscape) == -1)
        {
            return val;
        }
        
        if (val.Contains(_deafultEscapeCharInExternal))
        {
            val = val.Replace(_deafultEscapeCharInExternalSTRING, _escapedEscapeChar);
        }
        if (val.Contains(_columnSeparator))
        {
            val = val.Replace(_columnSeparatorString, _escapedColumnSeparator);
        }
        if (val.Contains('\n'))
        {
            val = val.Replace("\n", _escapedNewLine);
        }
        if (val.Contains('\r'))
        {
            val = val.Replace("\r", "");
        }

        return val;
    }
}
