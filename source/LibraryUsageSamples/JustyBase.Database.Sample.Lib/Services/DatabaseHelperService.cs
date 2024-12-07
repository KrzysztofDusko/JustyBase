﻿using System.Buffers.Text;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.Text;
using JustyBase.Common.Helpers;
using JustyBase.Database.Sample.Contracts;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.Tools;
using JustyBase.Tools.ImportHelpers;
using JustyBase.Tools.ImportHelpers.XML;
using JustyBase.StringExtensions;

#if ORACLE
using Oracle.ManagedDataAccess.Client;
#endif

namespace JustyBase.Database.Sample.Services;

public sealed class DatabaseHelperService : IDatabaseHelperService
{
    private readonly string _tempDir;
    public Action<string>? MessageAction { get; set; }
    public DatabaseTypeEnum DatabaseType { get; set; } = DatabaseTypeEnum.NetezzaSQL;

    public DatabaseHelperService()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),"JustyBase");
        if (!Directory.Exists(_tempDir))
        {
            Directory.CreateDirectory(_tempDir);
        }
    }

    public async Task<string?> PerformExportToFile(string sql, ExportEnum exportEnum, CompressionEnum csvCompression)
    {
        if (!Directory.Exists(_tempDir))
            throw new ArgumentException("please provide not null + existing temp directory\n");
        var filePath = $"{_tempDir}\\exported_{Random.Shared.Next()}.{GetExportExt(exportEnum)}";

        await Task.Run(() =>
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = 3600;
                using var rdr = cmd.ExecuteReader();
                if (filePath.EndsWithAny([".xlsx", ".xlsb"]))
                {
                    rdr.HandleExcelOutput(filePath, sql, null, null);
                }
                else
                {
                    var opt = new AdvancedExportOptions
                    {
                        LineDelimiter = "\r\n",
                        Delimiter = '|',
                        Encod = Encoding.UTF8,
                        Header = true,
                        CompresionType = csvCompression
                    };

                    filePath = rdr.HandleCsvOrParquetOutput(filePath, opt, null);
                }
            }
            catch (Exception ex)
            {
                MessageAction?.Invoke(ex.Message);
            }
        });

        return filePath;
    }

    public async Task<string> PerformImportFromXmlExcelBytesAsync(byte[] xmlBytes)
    {
        IDatabaseWithSpecificImportService importTemp = new ItemToImportService(GetConnection(), _tempDir, DatabaseType);
        var randName = await importTemp.PerformImportFromXmlAsync(new DbXMLImportJob(), xmlBytes, MessageAction);
        return randName;
    }

    public async Task PerformImportFromFileAsync(string filePath)
    {
        try
        {
            var importFrom = new ImportFromExcelFile(MessageAction, null)
            {
                StandardMessageAction = MessageAction,
                FilePath = filePath
            };

            IDatabaseWithSpecificImportService importableService =
                new ItemToImportService(GetConnection(), _tempDir, DatabaseType);
            await importFrom.PerformFastImportFromFileAsync(DatabaseType, importableService);
        }
        catch (Exception ex)
        {
            MessageAction?.Invoke(ex.Message);
        }
    }

    public async Task OpenWithDefaultProgramAsync(string path)
    {
        await Task.Run(() =>
        {
            using var fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        });
    }

    private DbConnection GetConnection()
    {
#if NETEZZA
        if (DatabaseType == DatabaseTypeEnum.NetezzaSQLOdbc)
        {
            OdbcConnectionStringBuilder builder = new OdbcConnectionStringBuilder();
            builder.Driver = "NetezzaSQL";
            builder["username"] = EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("NetezzaTestUser")!);
            builder["password"] = EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("NetezzaTestPass")!);
            builder["port"] = "5480";
            builder["servername"] = EncryptionHelper.Decrypt(Environment.GetEnvironmentVariable("NetezzaTestServer")!);
            builder["database"] = "JUST_DATA";
            var conn = new OdbcConnection(builder.ConnectionString);
            conn.ConnectionTimeout = 10;
            return conn;
        }
#endif
#if ORACLE
        if (DatabaseType == DatabaseTypeEnum.Oracle)
        {
            var cs = Environment.GetEnvironmentVariable("OracleTest");
            if (cs is null)
            {
                throw new ArgumentNullException();
            }
            if (!Base64.IsValid(cs))
            {
                cs = EncryptionHelper.Encrypt(cs);
                Environment.SetEnvironmentVariable("OracleTest",cs, EnvironmentVariableTarget.User);
            }
            return new OracleConnection(EncryptionHelper.Decrypt(cs));
        }
#endif
        throw new NotImplementedException();
    }

    private static string GetExportExt(ExportEnum exportEnum) => exportEnum switch
    {
        ExportEnum.xlsx => "xlsx",
        ExportEnum.csv => "csv",
        ExportEnum.xlsb => "xlsb",
        _ => throw new NotImplementedException()
    };
}

