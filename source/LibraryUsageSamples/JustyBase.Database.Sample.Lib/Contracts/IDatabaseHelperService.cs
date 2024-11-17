using System;
using System.Threading.Tasks;
using JustyBase.PluginCommon.Enums;

namespace JustyBase.Database.Sample.Contracts;

public interface IDatabaseHelperService
{
    DatabaseTypeEnum DatabaseType { get; set; }
    Action<string>? MessageAction { get; set; }
    Task<string> PerformImportFromXmlExcelBytesAsync(byte[] xmlBytes);
    Task PerformImportFromFileAsync(string filePath);
    Task<string?> PerformExportToFile(string sql, ExportEnum exportEnum, CompressionEnum csvCompression);
    Task OpenWithDefaultProgramAsync(string path);
}

public enum ExportEnum
{
    xlsx,
    csv,
    xlsb
}