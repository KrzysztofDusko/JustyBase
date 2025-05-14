using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommons;
using JustyBase.Services;
using NetezzaDotnetPlugin;

namespace JustyBase.Database.Sample.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly IEncryptionHelper _encryptionHelper;
    private readonly IDatabaseService _netezza;

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
            return;
        }

        await clipboard.SetTextAsync(Info);
        _avaloniaSpecificHelpers.CloseMainWindow();
    }

    public async Task ImportFromPath(string path)
    {
        SelectedTabIndex = 0;
        var _currentImport = new ImportFromExcelFile(x => Info+=x, ISimpleLogger.EmptyLogger)
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

    private async ValueTask<string> GetSql()
    {
        string? sql = Document.Text;

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

        if (!string.IsNullOrWhiteSpace(sql) && _variable_regex().IsMatch(sql))
        {
            var matches = Regex.Matches(sql, @"\$[a-zA-Z0-9_]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var matchesList = matches.Cast<Match>()
                .OrderByDescending(m => m.Length)
                .ToList();
            foreach (Match match in matchesList)
            {
                var vvm = new AskForVariable
                {
                    DataContext = new AskForVariableViewModel 
                    { 
                        VariableName = match.Value 
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

        return sql??"";
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
            if (filePath is null) return;
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
            if (filePath is not null)
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

    private async Task<string?> ExportRaw(string sql, string path)
    {
        bool csvSelected = CsvSelected;
        string ext = GetExtension(csvSelected);

        var filePath = Path.Combine(path, StringExtension.RandomSuffix("exported") + ext);
        await Task.Run(() =>
        {
            try
            {
                var con = _netezza.GetConnection(null, pooling: false);
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = sql;
                var rdr = cmd.ExecuteReader();

                if (csvSelected)
                {
                    rdr.HandleCsvOrParquetOutput(filePath, null, x => Info += x);
                }
                else
                {
                    rdr.HandleExcelOutput(filePath, sql, null, x => Info += x);
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

    [GeneratedRegex(@"\$[a-zA-Z0-9_]+", RegexOptions.IgnoreCase, 200)]
    private static partial Regex _variable_regex();
}
