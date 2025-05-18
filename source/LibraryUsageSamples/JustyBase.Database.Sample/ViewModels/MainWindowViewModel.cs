using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using JustyBase.Common.Tools;
using JustyBase.Common.Tools.ImportHelpers;
using JustyBase.Common.Tools.ImportHelpers.XML;
using JustyBase.Editor;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommons;
using JustyBase.Services;
using NetezzaDotnetPlugin;
using SpreadSheetTasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JustyBase.Database.Sample.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly IEncryptionHelper _encryptionHelper;
    private readonly IDatabaseService _netezza;


    public bool IsAdvancedMode
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (field)
            {
                ScreenSelected = true;
            }
        }
    } = false;


    [ObservableProperty]
    public partial bool CsvSelected { get; set; }

    //[ObservableProperty]
    //private readonly string _info = "";

    public string Info
    {
        get => Dispatcher.UIThread.Invoke<string>(() => LogDocument.Text);
        set
        {
            Dispatcher.UIThread.Post(() => LogDocument.Text = value);
            OnPropertyChanged(nameof(LogDocument));
        }
    }
    [ObservableProperty]
    public partial TextDocument LogDocument { get; set; } = new TextDocument();

    [ObservableProperty]
    public partial TextDocument Document { get; set; } = new TextDocument();


    [ObservableProperty]
    public partial string SelectedMode { get; set; } = "plain";

    [ObservableProperty]
    public partial bool XlsbSelected { get; set; } = true;

    [ObservableProperty]
    public partial bool XlsxSelected { get; set; }

    [ObservableProperty]
    public partial bool ScreenSelected { get; set; }


    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; } = 1;


    public ObservableCollection<LogItemViewModel> LogItems = new ObservableCollection<LogItemViewModel>();


    public MainWindowViewModel(IAvaloniaSpecificHelpers avaloniaSpecificHelpers, IEncryptionHelper encryptionHelper)
    {
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        _encryptionHelper = encryptionHelper;
        string? netezzaTest = Environment.GetEnvironmentVariable("NetezzaTest");
        if (netezzaTest is null)
        {
            //throw new ArgumentNullException(nameof(netezzaTest));
            return;
        }
        netezzaTest = _encryptionHelper.Decrypt(netezzaTest);

        var i1 = netezzaTest.IndexOf("servername=", StringComparison.OrdinalIgnoreCase);
        var i2 = netezzaTest.IndexOf(';', i1);
        var servername = netezzaTest[(i1 + "servername=".Length)..i2];

        i1 = netezzaTest.IndexOf("database=", StringComparison.OrdinalIgnoreCase);
        i2 = netezzaTest.IndexOf(';', i1);
        var database = netezzaTest[(i1 + "database=".Length)..i2];

        i1 = netezzaTest.IndexOf("username=", StringComparison.OrdinalIgnoreCase);
        i2 = netezzaTest.IndexOf(';', i1);
        var username = netezzaTest[(i1 + "username=".Length)..i2];

        i1 = netezzaTest.IndexOf("password=", StringComparison.OrdinalIgnoreCase);
        i2 = netezzaTest.IndexOf(';', i1);
        var password = netezzaTest[(i1 + "password=".Length)..i2];

        i1 = netezzaTest.IndexOf("port=", StringComparison.OrdinalIgnoreCase);
        i2 = netezzaTest.IndexOf(';', i1);
        var port = netezzaTest[(i1 + "port=".Length)..i2];

        _netezza = new Netezza(username, password, port, servername, database, 3600)
        {
            Name = "NetezzaTest"
        };
    }

    public List<string> CsvCompresionModes { get; } =
    [
        "plain",
        "zst",
        "zip",
        "brotli",
        "gzip"
    ];

    [RelayCommand]
    private async Task CopyFromClip()
    {
        SelectedTabIndex = 0;
        var clipboard = _avaloniaSpecificHelpers.GetClipboard();
        if (clipboard is null)
        {
            Info = "ERROR - Clipboard is empty\n";
            LogItems.Add(new LogItemViewModel("ERROR - Clipboard is empty\n"));
            return;
        }

        await clipboard.SetTextAsync(Info);
        _avaloniaSpecificHelpers.CloseMainWindow();
    }

    public async Task ImportFromPath(string path)
    {
        SelectedTabIndex = 0;
        var _currentImport = new ImportFromExcelFile(x => Info += x, ISimpleLogger.EmptyLogger)
        {
            FilePath = path
        };

        if (!_currentImport.InitImport(encoding: Encoding.UTF8))
        {
            Info += $"IMPORT FAILED";
            return;
        }
        string res = "";
        string randomName = StringExtension.RandomSuffix("IMP_");
        try
        {
            await _currentImport.ImportFromFileAllSteps(_netezza.DatabaseType, _netezza, "", randomName);
            _currentImport.StandardMessageAction = m => Info += m;
            res = randomName;
            Info += randomName;
        }
        catch (Exception ex)
        {
            Info += ex.Message;
        }
    }
    public SqlCodeEditor TextEditor;
    private async ValueTask<string> GetSql()
    {
        string? sql = Document.Text;
        if (TextEditor.SelectionLength > 0)
        {
            sql = TextEditor.SelectedText;
        }

        if (!sql.Contains("FROM", StringComparison.OrdinalIgnoreCase) && !sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            var clipboard = _avaloniaSpecificHelpers.GetClipboard();
            if (clipboard is null)
            {
                Info += "ERROR - Clipboard is empty\n";
                return "";
            }
            sql = await clipboard.GetTextAsync();
        }

        var matches = _variableRegex().Matches(sql);
        if (!string.IsNullOrWhiteSpace(sql) && matches.Any())
        {
            var matchesList = matches.Cast<Match>()
                .OrderByDescending(m => m.Length)
                .ToList();
            foreach (Match match in matchesList)
            {
                var vvm = new AskForVariable
                {
                    DataContext = new AskForVariableViewModel
                    {
                        VariableName = match.Groups["variable"].Value
                    }
                };
                await vvm.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
                var variableValue = ((AskForVariableViewModel)vvm.DataContext).VariableValue;
                if (string.IsNullOrEmpty(variableValue))
                {
                    sql = "";
                    break;
                }
                sql = sql.Replace(match.Value, variableValue, StringComparison.OrdinalIgnoreCase);
            }
        }

        return sql ?? "";
    }

    [RelayCommand]
    private void AdvancedMode(object par)
    {
        if (par.ToString()== "_")
        {
            IsAdvancedMode = !IsAdvancedMode;
        }
        if (par.ToString() == "0")
        {
            IsAdvancedMode = false;
        }
    }


    [RelayCommand]
    private async Task ImportAsync()
    {
        try
        {
            SelectedTabIndex = 0;
            var clipboard = _avaloniaSpecificHelpers.GetClipboard();
            if (clipboard is null)
            {
                Info = "ERROR - Clipboard is empty\n";
                LogItems.Add(new LogItemViewModel("ERROR - Clipboard is empty\n"));
                return;
            }

            var formats = await clipboard.GetFormatsAsync();
            if (formats.Contains("XML Spreadsheet"))
            {
                var xmlData = await clipboard.GetDataAsync("XML Spreadsheet");
                if (xmlData is byte[] xmlBytes)
                {
                    Info += "-->\n";
                    _netezza.TempDataDirectory = Path.GetTempPath();
                    var res = await _netezza.PerformImportFromXmlAsync(new DbXMLImportJob(), xmlBytes,
                        (s) => Info += $"{s}\n");
                    Info += res;
                    Info += " <--\n";
                }
            }
            else if (formats.Contains("Files"))
            {
                var files = await clipboard.GetDataAsync("Files") as IEnumerable<IStorageItem>;
                if (files is not null)
                    foreach (var file in files)
                        await ImportFromPath(file.Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            Info += $"[ERROR]\n{ex.Message}\n*********\n{ex.StackTrace}\n";
        }
        finally
        {
            Info += "[END]\n";
        }
        Info += "[FINISHED]\n";
    }

    [RelayCommand]
    private async Task Export()
    {
        try
        {
            var clipboard = _avaloniaSpecificHelpers.GetClipboard();
            var storageProvider = _avaloniaSpecificHelpers.GetStorageProvider();
            if (clipboard is null)
            {
                Info = "ERROR - Clipboard is empty\n";
                return;
            }

            if (storageProvider is null)
            {
                Info = "ERROR - StorageProvider is null\n";
                return;
            }

            string sql = await GetSql();
            if (string.IsNullOrEmpty(sql))
            {
                Info = "ERROR - sql is null\n";
                return;
            }

            Info += "\nsql is running\n";
            var filePath = await ExportRaw(sql, Path.GetTempPath());
            if (string.IsNullOrEmpty(filePath)) return;
            var dataObject = new DataObject();

            var fl = await storageProvider!.TryGetFileFromPathAsync(filePath);
            if (fl is not null)
            {
                dataObject.Set(DataFormats.Files, new IStorageItem[] { fl });
                Info += "copying to clipboard\n";
                await clipboard.SetDataObjectAsync(dataObject);
            }
        }
        catch (Exception ex)
        {
            Info += $"[ERROR]\n{ex.Message}\n*********\n{ex.StackTrace}\n";
        }
        finally
        {
            Info += "[END] exported and copied\n";
        }
    }


    [RelayCommand]
    private async Task ExportToDesktop()
    {
        try
        {
            Info = "";
            string sql = await GetSql();

            if (string.IsNullOrEmpty(sql))
            {
                Info = "ERROR - sql is null\n";
                return;
            }

            Info += "\nsql is running\n";
            var filePath = await ExportRaw(sql, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }

        catch (Exception ex)
        {
            Info += $"[ERROR]\n{ex.Message}\n*********\n{ex.StackTrace}\n";
        }
        finally
        {
            Info += "[END]\n";
        }
    }

    [RelayCommand]
    private async Task ExportAndOpen()
    {
        try
        {
            Info = "";
            string sql = await GetSql();

            if (string.IsNullOrEmpty(sql))
            {
                Info = "ERROR - sql is null\n";
                return;
            }

            Info += "\nsql is running\n";
            var filePath = await ExportRaw(sql, Path.GetTempPath());

            Info += "opening\n";
            if (!string.IsNullOrEmpty(filePath))
                await OpenWithDefaultProgramAsync(filePath);
        }

        catch (Exception ex)
        {
            Info += $"[ERROR]\n{ex.Message}\n*********\n{ex.StackTrace}\n";
        }
        finally
        {
            Info += "[END]\n";
        }
    }

    [RelayCommand]
    private async Task ExportAndOpenFromList()
    {
        try
        {
            Info = "";
            string sql = await GetSql();

            Info += "\nsql is running\n";
            var filePath = Path.Combine(Path.GetTempPath(), StringExtension.RandomSuffix("exported") + ".xlsb");

            var excelFile = new XlsbWriter(filePath)
            {
                SuppressSomeDate = true,
            };
            excelFile.AddSheet("Sheet1");
            excelFile.WriteSheet(_headers, _typeCodes, _filterdResults);
            excelFile.AddSheet("SQL1", hidden: true);
            excelFile.WriteSheet(sql.GetSqLParts());
            excelFile.Dispose();

            if (!string.IsNullOrEmpty(filePath))
                await OpenWithDefaultProgramAsync(filePath);
        }

        catch (Exception ex)
        {
            Info += $"[ERROR]\n{ex.Message}\n*********\n{ex.StackTrace}\n";
        }
        finally
        {
            Info += "[END]\n";
        }
    }


    public static async Task OpenWithDefaultProgramAsync(string path)
    {
        await Task.Run(() =>
        {
            using var fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        });
    }

    private DbCommand _dbCommand;

    [RelayCommand]
    private void CancelQuery()
    {
        if (_dbCommand != null)
        {
            try
            {
                _dbCommand.Cancel();
                Info += "Query cancelled\n";
            }
            catch (Exception ex)
            {
                Info += $"[ERROR]\n{ex.Message}\n*********\n{ex.StackTrace}\n";
            }
        }
    }

    private async Task<string?> ExportRaw(string sql, string path)
    {
        bool csvSelected = CsvSelected;
        string ext = GetExtension(csvSelected);

        string filePath = "";
        await Task.Run(() =>
        {
            try
            {
                var con = _netezza.GetConnection(null, pooling: false);
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = sql;
                _dbCommand = cmd;
                var rdr = cmd.ExecuteReader();
                

                if (!ScreenSelected)
                {
                    filePath = Path.Combine(path, StringExtension.RandomSuffix("exported") + ext);
                    if (csvSelected)
                    {
                        rdr.HandleCsvOrParquetOutput(filePath, null, x => Info += x);
                    }
                    else
                    {
                        rdr.HandleExcelOutput(filePath, sql, null, x => Info += x);
                    }
                }
                else
                {
                    // Update grid with results
                    UpdateGridData(rdr);
                }
            }
            catch (Exception e)
            {
                Info += e.Message;
                SelectedTabIndex = 0;
            }
        });

        return filePath;
    }

    private string GetExtension(bool csvSelected)
    {
        string ext = ".xlsb";
        if (XlsxSelected)
        {
            ext = ".xlsx";
        }
        if (csvSelected)
        {
            if (SelectedMode == "plain")
            {
                ext = ".csv";
            }
            else if (SelectedMode == "zst")
            {
                ext = ".csv.zst";
            }
            else if (SelectedMode == "zip")
            {
                ext = ".csv.zip";
            }
            else if (SelectedMode == "br")
            {
                ext = ".csv.br";
            }
            else if (SelectedMode == "gzip")
            {
                ext = ".csv.gz";
            }
        }

        return ext;
    }

    public IDatabaseService GetTestDatabaseService()
    {
        return _netezza;
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogDocument.Text = string.Empty;
        OnPropertyChanged(nameof(LogDocument));
        LogItems.Clear();
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        try
        {
            var storageProvider = _avaloniaSpecificHelpers.GetStorageProvider();
            if (storageProvider is null)
            {
                Info = "ERROR - StorageProvider is null\n";
                return;
            }

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open SQL File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("SQL Files")
                    {
                        Patterns = new[] { "*.sql" },
                        MimeTypes = new[] { "text/plain" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            var file = files.FirstOrDefault();
            if (file is not null)
            {
                using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                Document.Text = content;
                OnPropertyChanged(nameof(Document));
                
                SelectedTabIndex = 1; // Switch to SQL editor tab
                Info += $"Opened file: {file.Name}\n";
            }
        }
        catch (Exception ex)
        {
            Info += $"[ERROR]\n{ex.Message}\n*********\n{ex.StackTrace}\n";
        }
    }


    private FlatTreeDataGridSource<object[]>? _resultFlatCollection;
    public FlatTreeDataGridSource<object[]> ResultFlatCollection => _resultFlatCollection;

    private readonly Lock _searchLock = new Lock();
    
    public string SearchResultTxt
    {
        get => field;
        set 
        { 
            SetProperty(ref field, value);
            
            // Execute the filtering on a background thread to keep UI responsive
            Task.Run(() =>
            {
                List<object[]> filteredRows;
                lock (_searchLock)
                {
                    // Partition data for parallel processing
                    var partitionCount = Environment.ProcessorCount;
                    //var partitionSize = (_allResults.Count / partitionCount) + 1;
                    
                    filteredRows = _allResults
                        .AsParallel()
                        .WithDegreeOfParallelism(partitionCount)
                        .WithMergeOptions(ParallelMergeOptions.FullyBuffered)
                        .Where(item => string.IsNullOrEmpty(field) || 
                            item.Any(val => 
                                val?.ToString()?.Contains(field, StringComparison.OrdinalIgnoreCase) == true))
                        .ToList();
                    _filterdResults = filteredRows;
                }

                // Update UI on the dispatcher thread
                Dispatcher.UIThread.Post(() =>
                {
                    ResultFlatCollection.Items = filteredRows;
                    OnPropertyChanged(nameof(ResultFlatCollection));
                    RowsCoundText = filteredRows.Count.ToString("N0");
                });
            });
        }
    }

    [ObservableProperty]
    public partial string RowsCoundText { get; set; } = "0";


    private List<object[]> _allResults;
    private List<object[]> _filterdResults;
    private List<TypeCode> _typeCodes;
    private List<string> _headers;
    private void UpdateGridData(DbDataReader reader)
    {
        _allResults = new List<object[]>();
        _filterdResults = _allResults;
        _typeCodes = new List<TypeCode>();
        _headers = new List<string>();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            _typeCodes.Add(Type.GetTypeCode(reader.GetFieldType(i)));
            _headers.Add(reader.GetName(i));
        }

        while (reader.Read())
        {
            var values = new object?[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            _allResults.Add(values);
        }
        RowsCoundText = _allResults.Count.ToString("N0");
        _resultFlatCollection = new FlatTreeDataGridSource<object[]>(_allResults);       

        // Add columns dynamically based on the reader's schema
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnIndex = i;

            switch (reader.GetFieldType(i))
            {
                case Type t when t == typeof(string):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], string>(
                        reader.GetName(i),
                        x => x[columnIndex].ToString() ?? ""));
                    break;
                case Type t when t == typeof(int):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], int>(
                        reader.GetName(i),
                        x => (int)x[columnIndex]));
                    break;
                case Type t when t == typeof(double):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], double>(
                        reader.GetName(i),
                        x => (double)x[columnIndex]));
                    break;
                default:
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], string?>(
                        reader.GetName(i),
                        x => x[columnIndex].ToString()));
                    break;
            }
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            _resultFlatCollection.Selection = new TreeDataGridCellSelectionModel<object[]>(_resultFlatCollection) { SingleSelect = false };
            OnPropertyChanged(nameof(ResultFlatCollection));
        });
    }

    [GeneratedRegex(@"([^\:]|$)+(?<variable>(\$|\:)[a-zA-Z0-9_]+)", RegexOptions.IgnoreCase, 200)]
    private static partial Regex _variableRegex();


    private HierarchicalTreeDataGridSource<SchemaItem>? _dbSchemaSource;
    public HierarchicalTreeDataGridSource<SchemaItem> DbSchemaSource
    {
        get
        {
            if (_dbSchemaSource == null)
            {
                var data = new ObservableCollection<SchemaItem>
                {
                    new SchemaItem 
                    { 
                        Name = "PUBLIC", 
                        Type = "Schema",
                        Children = 
                        {
                            new SchemaItem 
                            { 
                                Name = "CUSTOMERS", 
                                Type = "Table",
                                Schema = "PUBLIC",
                                Children =
                                {
                                    new SchemaItem { Name = "CUSTOMER_ID", Type = "Column", Schema = "PUBLIC" },
                                    new SchemaItem { Name = "NAME", Type = "Column", Schema = "PUBLIC" },
                                    new SchemaItem { Name = "EMAIL", Type = "Column", Schema = "PUBLIC" }
                                }
                            },
                            new SchemaItem 
                            { 
                                Name = "ORDERS", 
                                Type = "Table",
                                Schema = "PUBLIC",
                                Children =
                                {
                                    new SchemaItem { Name = "ORDER_ID", Type = "Column", Schema = "PUBLIC" },
                                    new SchemaItem { Name = "CUSTOMER_ID", Type = "Column", Schema = "PUBLIC" },
                                    new SchemaItem { Name = "ORDER_DATE", Type = "Column", Schema = "PUBLIC" }
                                }
                            }
                        }
                    }
                };

                _dbSchemaSource = new HierarchicalTreeDataGridSource<SchemaItem>(data)
                {
                    Columns =
                    {
                        new HierarchicalExpanderColumn<SchemaItem>(
                            new TextColumn<SchemaItem, string>("Name", x => x.Name),
                            x => x.Children),
                        new TextColumn<SchemaItem, string>("Type", x => x.Type),
                        new TextColumn<SchemaItem, string>("Schema", x => x.Schema)
                    }
                };
            }
            return _dbSchemaSource;
        }
    }

}

public sealed class SchemaItem
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Schema { get; set; } = "";
    public ObservableCollection<SchemaItem> Children { get; } = new();
}

public sealed class LogItemViewModel
{
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public LogItemViewModel(string message)
    {
        Message = message;
        Timestamp = DateTime.Now;
    }
}

