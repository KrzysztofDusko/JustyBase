using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Database.Sample.Contracts;
using JustyBase.Database.Sample.Services;
using JustyBase.PluginCommon.Enums;
using JustyBase.Services;

namespace JustyBase.Database.Sample.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly IDatabaseHelperService _databaseHelperService;

    [ObservableProperty] private bool _csvSelected;

    //[ObservableProperty]
    private string _info = "";

    public string Info
    {
        get => Dispatcher.UIThread.Invoke<string>(() => Document.Text); 
        set 
        {
            Dispatcher.UIThread.Post(() => Document.Text = value);
            OnPropertyChanged(nameof(Document));
        }
    }

    [ObservableProperty] private TextDocument _document = new TextDocument();

    [ObservableProperty] private string _selectedMode = "plain";

    [ObservableProperty] private bool _xlsbSelected = true;

    [ObservableProperty] private bool _xlsxSelected;

    
    public ObservableCollection<string> DatabasesList =>
    [
        "Oracle", "NetezzaOdbc"
    ];

    private string _selectedDatabase = "Oracle";

    public string SelectedDatabase
    {
        get => _selectedDatabase;
        set
        {
            SetProperty(ref _selectedDatabase, value);
            _databaseHelperService.DatabaseType = _selectedDatabase == "Oracle" ? DatabaseTypeEnum.Oracle : DatabaseTypeEnum.NetezzaSQLOdbc;
        }
    }

    public MainWindowViewModel(IDatabaseHelperService databaseHelperService,
        IAvaloniaSpecificHelpers avaloniaSpecificHelpers)
    {
        _databaseHelperService = databaseHelperService;
        _databaseHelperService.MessageAction = messageText =>
        {
            Info += $"{messageText}\n";
        };
        _databaseHelperService.DatabaseType = SelectedDatabase == "Oracle" ? DatabaseTypeEnum.Oracle : DatabaseTypeEnum.NetezzaSQLOdbc;
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
    }

    //public MainWindowViewModel() : this(new DatabaseHelperService(), new AvaloniaSpecificHelpers())
    //{
        
    //}

    public List<string> CsvCompresionModes { get; } = new()
    {
        "plain",
        "zst",
        "zip",
        "brotli",
        "gzip"
    };

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
        await _databaseHelperService.PerformImportFromFileAsync(path);
    }

    private async ValueTask<string> GetSql()
    {
        string sql = Document.Text;
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
        return sql;
    }

    [RelayCommand]
    private async Task Import()
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
                    Info += "--> ";
                    var res = await _databaseHelperService.PerformImportFromXmlExcelBytesAsync(xmlBytes);
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
                await _databaseHelperService.OpenWithDefaultProgramAsync(filePath);
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

    private ExportEnum GetExportEnum()
    {
        if (XlsxSelected) return ExportEnum.xlsx;
        if (CsvSelected) return ExportEnum.csv;
        return ExportEnum.xlsb;
    }

    private CompressionEnum GetCsvCompressionEnum()
    {
        return SelectedMode switch
        {
            "zst" => CompressionEnum.Zstd,
            "brotli" => CompressionEnum.Brotli,
            "zip" => CompressionEnum.Zip,
            "gzip" => CompressionEnum.Gzip,
            _ => CompressionEnum.None
        };
    }

    private async Task<string?> ExportRaw(string sql)
    {
        try
        {
            return await _databaseHelperService.PerformExportToFile(sql, GetExportEnum(), GetCsvCompressionEnum());
        }
        catch (Exception ex)
        {
            Info += $"[ERROR]\n{ex.Message}\n*********\n{ex.StackTrace}\n";
        }

        return null;
    }
}