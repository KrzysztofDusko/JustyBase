using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.StringExtensions;
using JustyBase.Tools.Import;
using SpreadSheetTasks;
using System.Text;

namespace JustyBase.Tools.ImportHelpers;

public sealed class ImportFromExcelFile
{
    private readonly Action<string>? _exceptionMessageAction;
    private readonly ISimpleLogger? _logToWeb;
    public ImportFromExcelFile(Action<string>? exceptionMessageAction, ISimpleLogger? logToWeb)
    {
        _exceptionMessageAction = exceptionMessageAction;
        _logToWeb = logToWeb;
    }

    public Action<string>? StandardMessageAction {  get; set; }

    public List<string> SheetNamesToImport { get; set; }

    private ExcelReaderAbstract _excelReader;
    public ExcelReaderAbstract ExcelReader => _excelReader;

    /// <summary>
    /// initialize and read tab names
    /// purpouse of spliting loginc with InitImport + rest of codwe is to allow user to change data types and select specific excel sheet.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="encoding"></param>
    public bool InitImport(Encoding? encoding = null)
    {
        if (FilePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || FilePath.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase))
        {
            _excelReader = new XlsxOrXlsbReadOrEdit();
        }
        else
        {
            CompressionEnum compression = FilePath.GetCsvCompressionEnum();
            if (compression == CompressionEnum.None)
            {
                _excelReader = new CsvReader();
            }
            else
            {
                _excelReader = new CsvReader(compression);
            }
        }

        try
        {
            _excelReader.Open(FilePath, true, encoding: encoding);
            SheetNamesToImport = _excelReader.GetScheetNames().ToList<string>();
            return true;
        }
        catch (Exception ex)
        {
            _logToWeb?.TrackError(ex, isCrash: false);

            DoFileDispose();
            return false;
        }
    }

    public void DoFileDispose()
    {
        try
        {
            _excelReader?.Dispose();
        }
        catch (Exception)
        {
        }
    }

    public string FilePath { get; set; }

    public bool TreatAllColumnsAsText { get; set; } = false;
    public async IAsyncEnumerable<DbImportJob> ReadFileAndReturnSingleImportJobs()
    {
        var tabsToImport = this.SheetNamesToImport;
        var progressMessage = this.StandardMessageAction;
        try
        {
            foreach (var sheetName in _excelReader.GetScheetNames().Where(x => tabsToImport.Contains(x)))
            {
                _excelReader.TreatAllColumnsAsText = this.TreatAllColumnsAsText;
                _excelReader.ActualSheetName = sheetName;
                DatabaseTypeChooser databaseTypeChooser = new DatabaseTypeChooser();
                StandardMessageAction?.Invoke("data scan started");
                await Task.Run(() => databaseTypeChooser.ExcelTypeDetection(_excelReader, _excelReader.ActualSheetName, StandardMessageAction, (long)TimeSpan.FromHours(4).TotalSeconds));
                StandardMessageAction?.Invoke("data scan ended");
                if (_excelReader is not CsvReader)//skipHeader?
                {
                    _excelReader.Read();
                }
                if (_excelReader is CsvReader csvReader)
                {
                    string path = csvReader.FilePath;
                    var compression = csvReader.Compression;
                    _excelReader.Dispose();
                    _excelReader = new CsvReader(compression);
                    _excelReader.TreatAllColumnsAsText = this.TreatAllColumnsAsText;
                    _excelReader.Open(path);
                }

                yield return new DbImportJob(new DataReaderFromExcelReaderAbstract(_excelReader, databaseTypeChooser), databaseTypeChooser);
            }
        }
        finally
        {
            _excelReader.Dispose();
        }
    }

    public async IAsyncEnumerable<ImportStepHelper> ImportFromFileStepByStep(DatabaseTypeEnum databaseTypeEnum,IDatabaseWithSpecificImportService databaseService, string schemaName, string databasaTableName,
        Action<string, string>? adColumnInfo = null, Action<List<string[]>>? previewAction = null)
    {
        var importJobs = ReadFileAndReturnSingleImportJobs();

        int i = 0;
        await foreach (DbImportJob importJob in importJobs)
        {
            string tmp = i == 0 ? "" : $"_{i}";
            string name = databaseTypeEnum == DatabaseTypeEnum.Oracle || string.IsNullOrEmpty(schemaName) ? $"{databasaTableName}{tmp}" : $"{schemaName}.{databasaTableName}{tmp}";

            for (int j = 0; j < importJob.ColumnTypesBestMatch.Length; j++)
            {
                adColumnInfo?.Invoke(importJob.ColumnHeadersNames[j], importJob.ColumnTypesBestMatch[j].ToString());
            }
            previewAction?.Invoke(importJob.PreviewRows);
            yield return (new ImportStepHelper()
            {
                Func = () => databaseService.DbSpecificImportPart(importJob, $"{name}", StandardMessageAction),
                ImportJob = importJob
            });
            i++;
        }
        yield break;
    }

    public async Task ImportFromFileAllSteps(DatabaseTypeEnum databaseType, IDatabaseWithSpecificImportService databaseService, string? schemaName, string databasaTableName)
    {
        try
        {
            var importJobs = ReadFileAndReturnSingleImportJobs();

            int i = 0;
            await foreach (var importJob in importJobs)
            {
                string tmp = i == 0 ? "" : $"_{i}";
                string name = databaseType == DatabaseTypeEnum.Oracle || string.IsNullOrEmpty(schemaName) ? $"{databasaTableName}{tmp}" : $"{schemaName}.{databasaTableName}{tmp}";
                await databaseService.DbSpecificImportPart(importJob, $"{name}", StandardMessageAction);
                i++;
            }
        }
        catch (Exception ex)
        {
            _logToWeb?.TrackError(ex, isCrash: false);
            _exceptionMessageAction?.Invoke(ex.Message);
            _exceptionMessageAction?.Invoke(ex.StackTrace ?? "no stack trace");
        }

        return;
    }
    /// <summary>
    /// shortcut method for first Excel sheet to database (all setting default)
    /// </summary>
    /// <param name="databaseType"></param>
    /// <param name="databaseWithSpecificImportService"></param>
    /// <returns></returns>
    public async Task PerformFastImportFromFileAsync(DatabaseTypeEnum databaseType, IDatabaseWithSpecificImportService databaseWithSpecificImportService)
    {
        try
        {
            if (InitImport())
            {
                string sheetName = SheetNamesToImport[0];
                StandardMessageAction?.Invoke("\n" + sheetName);
                SheetNamesToImport.Clear();
                SheetNamesToImport.Add(sheetName);

                string randomName = StringExtension.RandomSuffix("IMP_D_");
                await ImportFromFileAllSteps(databaseType, databaseWithSpecificImportService, null, randomName);

                StandardMessageAction?.Invoke($"FINISHED ** {randomName} **");
            }
            else
            {
                StandardMessageAction?.Invoke("\n" + "import failed");
            }
        }
        catch (Exception ex)
        {
            _logToWeb?.TrackError(ex, isCrash: false);
            _exceptionMessageAction?.Invoke(ex.Message);
        }
    }

}

public sealed class ImportStepHelper
{
    public Func<Task> Func { get; set; }
    public DbImportJob ImportJob { get; set; }
}



