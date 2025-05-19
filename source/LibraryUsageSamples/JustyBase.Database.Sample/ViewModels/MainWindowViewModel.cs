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
using JustyBase.PluginCommon.Enums;
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
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "servername", "" },
                { "database", "" },
                { "username", "" },
                { "password", "" },
                { "port", "" }
            };

        foreach (var param in parameters.Keys.ToList())
        {
            var startIndex = netezzaTest.IndexOf($"{param}=", StringComparison.OrdinalIgnoreCase);
            if (startIndex >= 0)
            {
                startIndex += param.Length + 1; // Skip parameter name and equals sign
                var endIndex = netezzaTest.IndexOf(';', startIndex);
                if (endIndex < 0) endIndex = netezzaTest.Length;
                parameters[param] = netezzaTest[startIndex..endIndex];
            }
        }

        var (servername, database, username, password, port) =
            (parameters["servername"], parameters["database"],
             parameters["username"], parameters["password"], parameters["port"]);

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
            Info += "no sql detected\n";
            return "";
        }

        var matches = _variableRegex().Matches(sql);
        if (!string.IsNullOrWhiteSpace(sql) && matches.Any())
        {
            var matchesList = matches.Cast<Match>()
                .OrderByDescending(m => m.Length)
                .ToList();
            foreach (Match match in matchesList)
            {
                var variableName = match.Groups["variable"].Value;
                var vvm = new AskForVariable
                {
                    DataContext = new AskForVariableViewModel
                    {
                        VariableName = variableName
                    }
                };
                await vvm.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
                var variableValue = ((AskForVariableViewModel)vvm.DataContext).VariableValue;
                if (string.IsNullOrEmpty(variableValue))
                {
                    sql = "";
                    break;
                }
                sql = sql.Replace(variableName, variableValue, StringComparison.OrdinalIgnoreCase);
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
                        x => x[columnIndex].ToString() ?? ""
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<string>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<string>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(byte):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], byte?>(
                        reader.GetName(i),
                        x => (byte)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<byte>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<byte>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(short):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], short?>(
                        reader.GetName(i),
                        x => (short)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<short>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<short>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(ushort):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], ushort?>(
                        reader.GetName(i),
                        x => (ushort)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<ushort>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<ushort>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(int):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], int?>(
                        reader.GetName(i),
                        x => (int)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<int>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<int>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(uint):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], uint?>(
                        reader.GetName(i),
                        x => (uint)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<uint>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<uint>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(long):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], long?>(
                        reader.GetName(i),
                        x => (long)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<long>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<long>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(ulong):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], ulong?>(
                        reader.GetName(i),
                        x => (ulong)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<ulong>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<ulong>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(float):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], float>(
                        reader.GetName(i),
                        x => (float)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<float>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<float>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(double):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], double>(
                        reader.GetName(i),
                        x => (double)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<double>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<double>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(decimal):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], decimal>(
                        reader.GetName(i),
                        x => (decimal)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<decimal>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<decimal>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(DateTime):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], DateTime>(
                        reader.GetName(i),
                        x => (DateTime)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<DateTime>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<DateTime>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(TimeSpan):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], decimal>(
                        reader.GetName(i),
                        x => (decimal)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<TimeSpan>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<TimeSpan>(a, b, columnIndex, -1))
                        }
                        ));
                    break;
                case Type t when t == typeof(bool):
                    _resultFlatCollection.Columns.Add(new TextColumn<object[], decimal>(
                        reader.GetName(i),
                        x => (decimal)x[columnIndex]
                        , null, new TextColumnOptions<object[]>()
                        {
                            CompareAscending = ((a, b) => ExtraComparer<bool>(a, b, columnIndex, 1)),
                            CompareDescending = ((a, b) => ExtraComparer<bool>(a, b, columnIndex, -1))
                        }
                        ));
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

    [GeneratedRegex(@"([^\:]|$)+(?<variable>(\$|\:)[a-zA-Z]+[a-zA-Z0-9_]+)", RegexOptions.IgnoreCase, 200)]
    private static partial Regex _variableRegex();


    public static HierarchicalTreeDataGridSource<SchemaItem> FallBackchemaSource = new HierarchicalTreeDataGridSource<SchemaItem>(new ObservableCollection<SchemaItem>()
    {
        new SchemaItem() { Name = "Please", Type = "Wait" }
    })
    {
        Columns =
        {
            new HierarchicalExpanderColumn<SchemaItem>(
                new TextColumn<SchemaItem, string>("Name", x => x.Name),
                x => x.Children),
            new TextColumn<SchemaItem, string>("Type", x => x.Type),
            new TextColumn<SchemaItem, string>("Database", x => x.Database)
        }
    };

    [RelayCommand]
    private async Task RefreshDb()
    {
        _dbSchemaSource = await GetSchema();
        OnPropertyChanged(nameof(DbSchemaSource));
    }

    public Task<HierarchicalTreeDataGridSource<SchemaItem>> DbSchemaSource => GetSchema();

    private HierarchicalTreeDataGridSource<SchemaItem>? _dbSchemaSource;
    private async Task<HierarchicalTreeDataGridSource<SchemaItem>> GetSchema()
    {
        if (!IsAdvancedMode)
        {
            return FallBackchemaSource;
        }
        if (_dbSchemaSource == null)
        {
            await Task.Run(_netezza.CacheMainDictionary);

            //await _netezza.CacheAllObjects(new TypeInDatabaseEnum[] { TypeInDatabaseEnum.Procedure,
            //                TypeInDatabaseEnum.View, TypeInDatabaseEnum.ExternalTable, TypeInDatabaseEnum.Synonym
            //        });

            var databases = _netezza.GetDatabases("").ToList();

            var data = new ObservableCollection<SchemaItem>();
            foreach (var db in databases)
            {
                var currentdb = new SchemaItem() { Name = db, Type = "Database" };
                data.Add(currentdb);

                var schemas = _netezza.GetSchemas(db, "");
                foreach (var schema in schemas)
                {
                    var curentSchema = new SchemaItem() { Database = db, Name = schema, Type = "Schema" };
                    currentdb.Children.Add(curentSchema);
                    var tables = _netezza.GetDbObjects(db, schema, "", TypeInDatabaseEnum.Table);
                    foreach (var table in tables)
                    {
                        var currentTable = new SchemaItem()
                        {
                            Database = db,
                            Name = table.Name,
                            Type = "Table"
                        };
                        curentSchema.Children.Add(currentTable);
                        var columns = _netezza.GetColumns(db, schema, table.Name, "");
                        foreach (var column in columns)
                        {
                            var currentColumn = new SchemaItem()
                            {
                                Database = db,
                                Name = column.Name,
                                Type = "Column"
                            };
                            currentTable.Children.Add(currentColumn);
                        }
                    }
                    var views = _netezza.GetDbObjects(db, schema, "", TypeInDatabaseEnum.View);
                    foreach (var view in views)
                    {
                        var currentView = new SchemaItem()
                        {
                            Database = db,
                            Name = view.Name,
                            Type = "View"
                        };
                        curentSchema.Children.Add(currentView);
                        var columns = _netezza.GetColumns(db, schema, view.Name, "");
                        foreach (var column in columns)
                        {
                            var currentColumn = new SchemaItem()
                            {
                                Database = db,
                                Name = column.Name,
                                Type = "Column"
                            };
                            currentView.Children.Add(currentColumn);
                        }
                    }
                }
            }

            _dbSchemaSource = new HierarchicalTreeDataGridSource<SchemaItem>(data)
            {
                Columns =
                    {
                        new HierarchicalExpanderColumn<SchemaItem>(
                            new TextColumn<SchemaItem, string>("Name", x => x.Name),
                            x => x.Children),
                        new TextColumn<SchemaItem, string>("Type", x => x.Type),
                        new TextColumn<SchemaItem, string>("Database", x => x.Database)
                    }
            };
        }
        return _dbSchemaSource;
    }

    private static int ExtraComparer<T>(object[] x, object[] y, int columnIndex, int sign) where T : IComparable
    {
        if (x[columnIndex] is null && (y[columnIndex] is null))
        {
            return 0;
        }
        if (x[columnIndex] is null)
        {
            return -sign;
        }
        if (y[columnIndex] is null)
        {
            return sign;
        }
        return sign * ((T)x[columnIndex]).CompareTo((T)y[columnIndex]);
    }

}

public sealed class SchemaItem
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Database { get; set; } = "";
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

