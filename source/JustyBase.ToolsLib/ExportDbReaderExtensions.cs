using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Extensions;
using JustyBase.Tools.ImportHelpers;
using K4os.Compression.LZ4.Streams;
using SpreadSheetTasks;
using Sylvan.Data.Csv;
using System.Data.Common;
using System.IO.Compression;
using System.Text;

namespace JustyBase.Tools;

public static class ExportDbReaderExtensions
{
    public static void HandleExcelOutput(this DbDataReader rdr, string filePathToExport, string sql,
        string? docPropertyProgramName, Action<int>? progressAction)
    {
        ExcelWriter excelFile;
        if (filePathToExport.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase))
        {
            excelFile = new XlsxWriter(filePathToExport)
            {
                SuppressSomeDate = true,
            };
        }
        else
        {
            excelFile = new XlsbWriter(filePathToExport)
            {
                SuppressSomeDate = true,
            };
        }

        if (docPropertyProgramName is not null)
        {
            excelFile.DocPopertyProgramName = docPropertyProgramName;
        }

        try
        {
            int i = 1;
            do
            {
                if (rdr.FieldCount != -1)
                {
                    excelFile.AddSheet($"Sheet{i}");
                    excelFile.On10k += progressAction;
                    excelFile.WriteSheet(rdr, doAutofilter: true);
                    excelFile.AddSheet($"SQL{i}", hidden: true);
                    excelFile.WriteSheet(sql.GetSqLParts());
                    i++;
                }
            } while (rdr.NextResult());
        }
        finally
        {
            excelFile.Dispose();
        }
    
    }

    public static string HandleCsvOrParquetOutput(this DbDataReader rdr,string filePathToExport, AdvancedExportOptions? opt,Action<long>? progressAction)
    {
        string finalFilePath = filePathToExport;
        int resultNumber = 1;
        do
        {
            if (rdr.FieldCount != -1)
            {
                string filePathToExportX = filePathToExport;
                if (resultNumber > 1)
                {
                    filePathToExportX += $"_{resultNumber}";
                }

                if (opt is null)
                {
                    opt = new AdvancedExportOptions();
                    if (filePathToExport.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.CompresionType = CompressionEnum.Zip;
                        filePathToExportX = filePathToExportX[..^4];
                    }
                    if (filePathToExport.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.CompresionType = CompressionEnum.Brotli;
                        filePathToExportX = filePathToExportX[..^3];
                    }
                    if (filePathToExport.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.CompresionType = CompressionEnum.Gzip;
                        filePathToExportX = filePathToExportX[..^3];
                    }
                    if (filePathToExport.EndsWith(".zst", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.CompresionType = CompressionEnum.Zstd;
                        filePathToExportX = filePathToExportX[..^4];
                    }
                    opt.LineDelimiter = "\r\n";
                    opt.Delimiter = '|';
                    opt.Encod = Encoding.UTF8;
                    opt.Header = true;
                }

                StreamWriter streamWriter = null!;
                Stream? helperStream = null;
                Action additionalAction = null!;
                try
                {
                    if (opt.CompresionType == CompressionEnum.L4z)
                    {
                        finalFilePath = filePathToExportX + ".lz4";
                        var fileStream = File.Open(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        helperStream = LZ4Stream.Encode(fileStream);
                        streamWriter = new StreamWriter(helperStream);
                        additionalAction = () =>
                        {
                            streamWriter.Dispose();
                            fileStream.Dispose();
                            helperStream.Dispose();
                        };
                    }
                    else if (opt.CompresionType == CompressionEnum.Brotli)
                    {
                        finalFilePath = filePathToExportX + ".br";
                        var fileStream = File.Open(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        helperStream = new BrotliStream(fileStream, CompressionLevel.Optimal);
                        streamWriter = new StreamWriter(helperStream);

                        additionalAction = () =>
                        {
                            streamWriter.Dispose();
                            fileStream.Dispose();
                            helperStream.Dispose();
                        };
                    }
                    else if (opt.CompresionType == CompressionEnum.Gzip)
                    {
                        finalFilePath = filePathToExportX + ".gz";
                        var fileStream = File.Open(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        helperStream = new GZipStream(fileStream, CompressionLevel.Optimal);
                        streamWriter = new StreamWriter(helperStream);

                        additionalAction = () =>
                        {
                            streamWriter.Dispose();
                            fileStream.Dispose();
                            helperStream.Dispose();
                        };
                    }
                    else if (opt.CompresionType == CompressionEnum.Zstd)
                    {
                        finalFilePath = filePathToExportX + ".zst";
                        var fileStream = File.Open(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        helperStream = new ZstdSharp.CompressionStream(fileStream);
                        streamWriter = new StreamWriter(helperStream);

                        additionalAction = () =>
                        {
                            streamWriter.Dispose();
                            fileStream.Dispose();
                            helperStream.Dispose();
                        };
                    }
                    else if (opt.CompresionType == CompressionEnum.Zip)
                    {
                        finalFilePath = filePathToExportX + ".zip";
                        helperStream = new FileStream(finalFilePath, FileMode.Create);
                        var archive = new ZipArchive(helperStream, ZipArchiveMode.Create, true);
                        var entry = archive.CreateEntry(Path.GetFileName(filePathToExportX));
                        Stream openedEntry = entry.Open();
                        streamWriter = new StreamWriter(openedEntry);
                        additionalAction = () =>
                        {
                            openedEntry.Dispose();
                            archive.Dispose();
                            helperStream.Dispose();
                        };
                    }
                    else
                    {
                        if (opt.Encod is not null)
                        {
                            streamWriter = new StreamWriter(filePathToExportX, append: false, encoding: opt.Encod);
                        }
                        else
                        {
                            streamWriter = new StreamWriter(filePathToExportX);
                        }
                        additionalAction = () => streamWriter.Dispose();
                    }


                    if (filePathToExport.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase))
                    {
                        ParquetFileWritterFromDataReader parquetWritter = new ParquetFileWritterFromDataReader(new DBReaderWithMessages(rdr, progressAction));
                        parquetWritter.CreateFile(streamWriter.BaseStream).Wait();
                    }
                    else
                    {
                        using var csvWriter = CsvDataWriter.Create(streamWriter, new CsvDataWriterOptions()
                        {
                            NewLine = opt.LineDelimiter,
                            Delimiter = opt.Delimiter,
                            WriteHeaders = opt.Header
                        });
                        csvWriter.Write(new DBReaderWithMessages(rdr, progressAction));
                    }
                }
                finally
                {
                    additionalAction.Invoke();
                }

                resultNumber++;
            }
        } while (rdr.NextResult());


        return finalFilePath;
    }

}
