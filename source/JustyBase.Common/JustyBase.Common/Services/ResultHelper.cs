using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using JustyBase.Common.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.StringExtensions;
using JustyBase.Tools;
using SpreadSheetTasks;
using Sylvan.Data.Csv;

namespace JustyBase.Services;
public sealed class ResultHelper
{
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly IMessageForUserTools  _messageForUserTools;
    private readonly char _csvColumnSeparator;
    private readonly string _csvRowSeparator;
    private readonly Encoding _csvEncoding;

    public ResultHelper(IGeneralApplicationData generalApplicationData, IMessageForUserTools messageForUserTools)
    {
        _generalApplicationData = generalApplicationData;
        _messageForUserTools = messageForUserTools;
        _csvColumnSeparator = _generalApplicationData.Config.SepInExportedCsv[0];
        _csvRowSeparator = _generalApplicationData.Config.SepRowsInExportedCsv switch
        {
            "windows" => "\r\n",
            _ => "\n"
        };
        try
        {
            _csvEncoding = AdvancedExportOptions.ParseEnconding(_generalApplicationData.Config.EncondingName);
        }
        catch (Exception ex1)
        {
            _generalApplicationData.GlobalLoggerObject.TrackError(ex1, isCrash: false);
            _generalApplicationData.Config.EncondingName = "UTF-8";
            _generalApplicationData.SaveConfig();
            _csvEncoding = AdvancedExportOptions.ParseEnconding(_generalApplicationData.Config.EncondingName);
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex1);
        };
    }
    public void CreateCsvFile(TextWriter stringWriter, DbDataReader rdr, bool headers)
    {
        using var csvWriter = CsvDataWriter.Create(stringWriter, new CsvDataWriterOptions()
        {
            NewLine = _csvRowSeparator,
            Delimiter = _csvColumnSeparator,
            WriteHeaders = headers
        });
        csvWriter.Write(rdr);

        //var csvWriter = new CsvWriter(stringWriter, CsvRowSeparator, CsvColumnSeparator, CsvEncoding, headers);
        //csvWriter.Write(rdr);
    }
    private string SheetName => _generalApplicationData.Config.DefaultXlsxSheetName;
    public string DefaultExcelExtension => _generalApplicationData.Config.UseXlsb == true ? ".xlsb" : ".xlsx";
    public async Task CreateExcelFile(string filePathToExport, DbDataReader rdr, string SQL)
    {
        bool IsXlsb = filePathToExport.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase);
        bool IsXlsx = filePathToExport.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);
        bool IsCsvZip = filePathToExport.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        bool IsParquet = filePathToExport.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase);
        var csvCompression = filePathToExport.GetCsvCompressionEnum();

        try
        {
            _ = new FileInfo(filePathToExport);
        }
        catch (Exception ex)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance($"File name is not valid \r\n {ex.Message}");
            return;
        }

        try
        {
            if (IsXlsb || IsXlsx)
            {
                await Task.Run(() =>
                {
                    ExcelWriter excelFile;
                    excelFile = IsXlsb ? new XlsbWriter(filePathToExport) { SuppressSomeDate = true } : new XlsxWriter(filePathToExport) { SuppressSomeDate = true };

                    excelFile.DocPopertyProgramName = "Justy";
                    try
                    {
                        excelFile.AddSheet($"{SheetName}1");
                        excelFile.WriteSheet(rdr, doAutofilter: true);
                        excelFile.AddSheet($"SQL", hidden: true);
                        excelFile.WriteSheet(SQL.GetSqLParts());
                    }
                    finally
                    {
                        excelFile.Dispose();
                    }
                });
            }
            else if (IsCsvZip)
            {
                await Task.Run(() =>
                {
                    using var  helperStream = new FileStream(filePathToExport, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var archive = new ZipArchive(helperStream, ZipArchiveMode.Create,true);
                    var entry = archive.CreateEntry(Path.GetFileName(filePathToExport[..^4]));
                    Stream openedEntry = entry.Open();
                    using var streamWriter = new StreamWriter(openedEntry);
                    CreateCsvFile(streamWriter, rdr,true);
                });
            }
            else if (IsParquet)
            {
                ParquetFileWritterFromDataReader parquetWritter = new ParquetFileWritterFromDataReader(rdr);
                using var fileStream = File.Open(filePathToExport, FileMode.Create, FileAccess.Write, FileShare.None);
                await parquetWritter.CreateFile(fileStream);
            }
            else
            {
                await Task.Run(() => 
                {
                    using var fileStream = File.Open(filePathToExport, FileMode.Create, FileAccess.Write, FileShare.None);
                    Stream helperStream = csvCompression switch
                    {
                        CompressionEnum.None => fileStream,
                        CompressionEnum.Brotli => new BrotliStream(fileStream, CompressionLevel.Optimal),
                        CompressionEnum.Gzip => new GZipStream(fileStream, CompressionLevel.Optimal),
                        CompressionEnum.Zstd => new ZstdSharp.CompressionStream(fileStream),
                        _ => throw new NotImplementedException(),
                    };
                    using var streamWriter = new StreamWriter(helperStream);
                    CreateCsvFile(streamWriter, rdr, true);
                });
            }
        }
        catch (Exception ex)
        {
            _generalApplicationData.GlobalLoggerObject.TrackError(ex, isCrash: false);
            _messageForUserTools.ShowSimpleMessageBoxInstance("File name is not valid ? " + "\n" + ex.Message);
            return;
        }
    }

    public async Task CreateXlsbOrXlsxFile(string filePathToExport, List<(DbDataReader,string)> listOfResults)
    {
        bool IsXlsb = filePathToExport.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase);
        bool IsXlsx = filePathToExport.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

        try
        {
            new FileInfo(filePathToExport);
        }
        catch (Exception ex)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
            return;
        }

        try
        {
            await Task.Run(() =>
            {
                ExcelWriter excelFile;
                excelFile = IsXlsb ? new XlsbWriter(filePathToExport) { SuppressSomeDate = true } : new XlsxWriter(filePathToExport) { SuppressSomeDate = true };

                excelFile.DocPopertyProgramName = "Justy";
                try
                {
                    int i1 = 1;
                    foreach (var (rdr, SQL) in listOfResults)
                    {
                        excelFile.AddSheet($"{SheetName}_{i1}");
                        excelFile.WriteSheet(rdr,doAutofilter: true);
                        excelFile.AddSheet($"SQL_{i1}", hidden: true);
                        excelFile.WriteSheet(SQL.GetSqLParts());
                        i1++;
                    }
                }
                finally
                {
                    excelFile.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            _generalApplicationData.GlobalLoggerObject.TrackError(ex, isCrash: false);
            _messageForUserTools.ShowSimpleMessageBoxInstance("File name is not valid ? " + "\n" + ex.Message);
            return;
        }
    }
}