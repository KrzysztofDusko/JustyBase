using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
using NetezzaOdbcPlugin;

namespace JustyBase.Database.Sample.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly IDatabaseService _netezzaOdbc;

    [ObservableProperty]
    public partial bool CsvSelected { get; set; }

    //[ObservableProperty]
    //private readonly string _info = "";

    public string Info
    {
        get => Dispatcher.UIThread.Invoke<string>(() => Document.Text); 
        set 
        {
            Dispatcher.UIThread.Post(() => Document.Text = value);
            OnPropertyChanged(nameof(Document));
        }
    }

    [ObservableProperty]
    public partial TextDocument Document { get; set; } = new TextDocument();

    [ObservableProperty]
    public partial string SelectedMode { get; set; } = "plain";

    [ObservableProperty]
    public partial bool XlsbSelected { get; set; } = true;

    [ObservableProperty]
    public partial bool XlsxSelected { get; set; }

    public ObservableCollection<string> DatabasesList =>
    [
        "Oracle", "NetezzaOdbc"
    ];

    [ObservableProperty]
    public partial string SelectedDatabase { get; set; } = "NetezzaOdbc";

    public MainWindowViewModel(IAvaloniaSpecificHelpers avaloniaSpecificHelpers)
    {
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        string? NetezzaTest = Environment.GetEnvironmentVariable("NetezzaTest");
        if (NetezzaTest is null)
        {
            throw new ArgumentNullException(nameof(NetezzaTest));
        }
        _netezzaOdbc = NetezzaOdbc.FromOdbc(NetezzaTest, 10);
        _netezzaOdbc.Name = "NetezzaTest";
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
            await _currentImport.ImportFromFileAllSteps(_netezzaOdbc.DatabaseType, _netezzaOdbc, "", randomName);
            res = randomName;
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
        return sql??"";
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        try
        {
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
                    _netezzaOdbc.TempDataDirectory = Path.GetTempPath();
                    var res = await _netezzaOdbc.PerformImportFromXmlAsync(new DbXMLImportJob(), xmlBytes,
                        (s) => Info += s);
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
            var filePath = await ExportRaw(sql);
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
            var filePath = await ExportRaw(sql);

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

    private async Task<string?> ExportRaw(string sql)
    {
        var filePath = Path.Combine(Path.GetTempPath(), StringExtension.RandomSuffix("exported") + ".xlsb");
        await Task.Run(() =>
        {
            var con = _netezzaOdbc.GetConnection(null, pooling: false);
            con.Open();
            var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            var rdr = cmd.ExecuteReader();
            rdr.HandleExcelOutput(filePath, sql, null, x => Info += x);
        });

        return filePath;
    }


    public IDatabaseService GetTestDatabaseService(string _connectionName)
    {
        return _netezzaOdbc;
    }

}