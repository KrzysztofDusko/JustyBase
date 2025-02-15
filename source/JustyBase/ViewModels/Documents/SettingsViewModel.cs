using Avalonia.Themes.Fluent;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common;
using JustyBase.Common.Contracts;
using JustyBase.Common.Models;
using JustyBase.Services;
using JustyBase.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Documents;

public partial class SettingsViewModel : DocumentBaseVM
{
    private readonly IEncryptionHelper _encryptionHelper;
    private readonly IMessageForUserTools _messageForUserTools;
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;

    public SettingsViewModel(IGeneralApplicationData generalApplicationData,
        IEncryptionHelper encryptionHelper, IMessageForUserTools messageForUserTools, IAvaloniaSpecificHelpers avaloniaSpecificHelpers)
    {
        _generalApplicationData = generalApplicationData;
        _encryptionHelper = encryptionHelper;
        _messageForUserTools = messageForUserTools;
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        Title = "Settings";

        ReloadSettings();
        CleanDataFolderCommand = new RelayCommand(ClearDataFolder);
        OpenFileDialogCmd = new AsyncRelayCommand<string>(OpenFileDialog);
        GetColors();
        _dispatcherTimer.Tick += DispatcherTimer_Tick;
        _dispatcherTimer?.Stop();
    }
    ///?
    private readonly DispatcherTimer _dispatcherTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };
    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        _dispatcherTimer?.Stop();
        SetColor();
    }

    private async Task OpenFileDialog(string opt)
    {
        try
        {
            var sp = _avaloniaSpecificHelpers.GetStorageProvider();
            var storageFolder =
                await sp.TryGetFolderFromPathAsync(
                    Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JustyBaseLegacy"));

            var fileList = await sp.OpenFilePickerAsync(new()
            {
                AllowMultiple = false,
                SuggestedStartLocation = storageFolder,
                Title = "Load file",
                FileTypeFilter = [new("ElementsData") { Patterns = [opt], MimeTypes = ["*/*"] }, FilePickerFileTypes.All]
            });
            if (fileList is null || fileList.Count == 0)
            {
                return;
            }
            var path = fileList[0].Path.LocalPath;

            if (path.EndsWith("snipets.json.enc"))
            {
                var snipets = JsonSerializer.Deserialize<SnippetsModel>(_encryptionHelper.GetEncodedContentOfTextFile(path));
                if (snipets is not null)
                {
                    _generalApplicationData.ClearTempSippetsObjects();
                    foreach (var item in snipets.Keywords)
                    {
                        if (!_generalApplicationData.Config.AllSnippets.ContainsKey(item))
                        {
                            _generalApplicationData.Config.AllSnippets[item] = new(AppOptions.TYPO_SNIPET_TXT, null, null, null);
                        }
                    }
                    foreach (var item in snipets.Snippets)
                    {
                        var item2 = item.Replace("(^)", "${Caret}");
                        if (!_generalApplicationData.Config.AllSnippets.ContainsKey(item2))
                        {
                            _generalApplicationData.Config.AllSnippets[item2] = new(AppOptions.STANDARD_SNIPET_TXT, null, item2, null);
                        }
                    }
                    foreach (var item in snipets.MonkeySnippets)
                    {
                        var item2 = item.Replace("(^)", "${Caret}").Replace("@@", "@");
                        int ind1 = item2.IndexOf(' ');
                        if (ind1 == -1 || ind1 == item2.Length)
                        {
                            return;
                        }
                        string name = item2[..ind1];
                        string txt = item2[(ind1 + 1)..].Replace("__", "@");

                        if (!_generalApplicationData.Config.AllSnippets.ContainsKey(name))
                        {
                            _generalApplicationData.Config.AllSnippets[name] = new(AppOptions.STANDARD_SNIPET_TXT, null, txt, txt);
                        }
                    }
                }
                else
                {
                    _messageForUserTools.ShowSimpleMessageBoxInstance("Import failed");
                }
            }
            else //...
            {
                _messageForUserTools.ShowSimpleMessageBoxInstance("to do");
            }

        }
        catch (Exception ex)
        {
            _generalApplicationData.GlobalLoggerObject.TrackError(ex, isCrash: false);
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
        }
    }


    private void ReloadSettings()
    {
        FullSettingsJsonString = JsonSerializer.Serialize(_generalApplicationData.Config, MyJsonContextAppOptions.Default.AppOptions);

        ResultRowsLimit = _generalApplicationData.Config.ResultRowsLimit;
        ConnectionTimeout = _generalApplicationData.Config.ConnectionTimeout;
        CommandTimeout = _generalApplicationData.Config.CommandTimeout;

        SepInExportedCsv = _generalApplicationData.Config.SepInExportedCsv;
        SepRowsInExportedCsv = _generalApplicationData.Config.SepRowsInExportedCsv;
        EncondingName = _generalApplicationData.Config.EncondingName;
        DecimalDelimInCsv = _generalApplicationData.Config.DecimalDelimInCsv;

        ExcelFormat = _generalApplicationData.Config.UseXlsb == true ? "xlsb" : "xlsx";
        DefaultXlsxSheetName = _generalApplicationData.Config.DefaultXlsxSheetName;
        CloseUndocked = _generalApplicationData.Config.CloseUndocked == true;

        AcceptDiagData = _generalApplicationData.Config.AcceptDiagData;
        AcceptCrashData = _generalApplicationData.Config.AcceptCrashData;
        AutocompleteOnReturn = _generalApplicationData.Config.AutocompleteOnReturn;
        ConfirmDocumentClosing = _generalApplicationData.Config.ConfirmDocumentClosing;
        LineSpacing = _generalApplicationData.Config.LineSpacing;
        ShowDetailsButton = _generalApplicationData.Config.ShowDetailsButton;
        DocumentFontName = _generalApplicationData.Config.DocumentFontName;
        ControlContentThemeFontSize = _generalApplicationData.Config.ControlContentThemeFontSize;
        CompletitionFontSize = _generalApplicationData.Config.CompletitionFontSize;
        DefaultFontSizeForDocuments = _generalApplicationData.Config.DefaultFontSizeForDocuments;

        ControlContentThemeFontSize = _generalApplicationData.Config.ControlContentThemeFontSize;
        CompletitionFontSize = _generalApplicationData.Config.CompletitionFontSize;
        DefaultFontSizeForDocuments = _generalApplicationData.Config.DefaultFontSizeForDocuments;

        UseSplashScreen = _generalApplicationData.Config.UseSplashScreen;

        AutoDownloadUpdate = _generalApplicationData.Config.AutoDownloadUpdate;
        AutoDownloadPlugins = _generalApplicationData.Config.AutoDownloadPlugins;
        AllowToLoadPlugins = _generalApplicationData.Config.AllowToLoadPlugins;
        //UpdateMitigatePaloAlto = _generalApplicationData.Config.UpdateMitigateNextGenFirewalls;
    }
    public ICommand CleanDataFolderCommand { get; }
    public ICommand OpenFileDialogCmd { get; }
    private void ClearDataFolder()
    {
        DirectoryInfo di = new(IGeneralApplicationData.DataDirectory);

        foreach (FileInfo file in di.GetFiles())
        {
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
            }
        }
        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            try
            {
                dir.Delete(true);
            }
            catch (Exception)
            {
            }
        }
    }

    public int? ResultRowsLimit
    {
        get;
        set
        {
            if (value < 100)
            {
                value = 100;
            }
            else if (value > 10_000_000)
            {
                value = 10_000_000;
            }
            SetProperty(ref field, value);
            _generalApplicationData.Config.ResultRowsLimit = ResultRowsLimit ?? 10_000;
        }
    }

    public int ConnectionTimeout
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.ConnectionTimeout = ConnectionTimeout;
        }
    }

    public int CommandTimeout
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.CommandTimeout = CommandTimeout;
        }
    }

    [ObservableProperty]
    public partial string FullSettingsJsonString { get; set; }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            _generalApplicationData.Config = JsonSerializer.Deserialize<AppOptions>(FullSettingsJsonString);
            ErrorInfo = "Success";
        }
        catch (Exception ex)
        {
            ErrorInfo = ex.Message;
        }
    }

    [ObservableProperty]
    public partial string ErrorInfo { get; set; }

    public List<string> SepInExportedCsvList { get; set; } =
    [
        ";",",","|"
    ];

    public List<string> SepRowsInExportedCsvList { get; set; } =
    [
        "windows","linux","unix"
    ];

    public List<string> EncondingNameList { get; set; } =
    [
        "UTF-8","Unicode","ASCII","UTF32","UTF16","Latin1"
    ];

    public List<string> DecimalDelimInCsvList { get; set; } =
    [
        ".",","
    ];

    public List<string> ExcelFormatList { get; set; } =
    [
        "xlsx","xlsb"
    ];

    public string ExcelFormat
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.UseXlsb = (ExcelFormat == "xlsb");

        }
    }

    public string SepRowsInExportedCsv
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.SepRowsInExportedCsv = SepRowsInExportedCsv;

        }
    }

    public string SepInExportedCsv
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.SepInExportedCsv = SepInExportedCsv;

        }
    }

    public string EncondingName
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.EncondingName = EncondingName;
        }
    }

    public string DecimalDelimInCsv
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.DecimalDelimInCsv = DecimalDelimInCsv;
        }
    }

    public string DefaultXlsxSheetName
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.DefaultXlsxSheetName = DefaultXlsxSheetName;
        }
    }

    public bool CloseUndocked
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.CloseUndocked = CloseUndocked;
        }
    }

    public bool AcceptDiagData
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.AcceptDiagData = AcceptDiagData;
        }
    }

    public bool AcceptCrashData
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.AcceptCrashData = AcceptCrashData;
        }
    }

    [ObservableProperty]
    public partial double ControlContentThemeFontSize { get; set; }
    [ObservableProperty]
    public partial double CompletitionFontSize { get; set; }

    [ObservableProperty]
    public partial double DefaultFontSizeForDocuments { get; set; }

    public bool AutocompleteOnReturn
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.AutocompleteOnReturn = AutocompleteOnReturn;
        }
    }

    public bool UseSplashScreen
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.UseSplashScreen = UseSplashScreen;
        }
    }

    public bool AutoDownloadUpdate
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.AutoDownloadUpdate = AutoDownloadUpdate;
        }
    }

    public bool AutoDownloadPlugins
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.AutoDownloadPlugins = AutoDownloadPlugins;
        }
    }

    public bool AllowToLoadPlugins
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.AllowToLoadPlugins = AllowToLoadPlugins;
        }
    }

    //public bool UpdateMitigatePaloAlto
    //{
    //    get;
    //    set
    //    {
    //        SetProperty(ref field, value);
    //        _generalApplicationData.Config.UpdateMitigateNextGenFirewalls = UpdateMitigatePaloAlto;
    //    }
    //}

    public bool ConfirmDocumentClosing
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.ConfirmDocumentClosing = ConfirmDocumentClosing;
        }
    }

    private void StartColorTimer()
    {
        _dispatcherTimer?.Stop();
        _dispatcherTimer?.Start();
    }

    public Color Accent
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color AltHigh
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color AltLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color AltMedium
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color AltMediumLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color AltMediumHigh
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color BaseHigh
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color BaseLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color BaseMedium
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color BaseMediumHigh
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color BaseMediumLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeHigh
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();

        }
    }

    public Color ChromeLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeMedium
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeMediumLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeWhite
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeGray
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeBlackHigh
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeBlackLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeBlackMedium
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeBlackMediumLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeDisabledHigh
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ChromeDisabledLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ListLow
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color ListMedium
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    public Color RegionColor
    {
        get;
        set
        {
            SetProperty(ref field, value);
            StartColorTimer();
        }
    }

    private ColorPaletteResources GetSelectedPalett()
    {
        ColorPaletteResources pal = new()
        {
            Accent = Accent,
            AltHigh = AltHigh,
            AltLow = AltLow,
            AltMedium = AltMedium,
            AltMediumHigh = AltMediumHigh,
            AltMediumLow = AltMediumLow,
            BaseHigh = BaseHigh,
            BaseLow = BaseLow,
            BaseMedium = BaseMedium,
            BaseMediumHigh = BaseMediumHigh,
            BaseMediumLow = BaseMediumLow,

            ChromeBlackHigh = ChromeBlackHigh,
            ChromeBlackLow = ChromeBlackLow,
            ChromeBlackMedium = ChromeBlackMedium,
            ChromeBlackMediumLow = ChromeBlackMediumLow,

            ChromeDisabledHigh = ChromeDisabledHigh,
            ChromeDisabledLow = ChromeDisabledLow,
            ChromeGray = ChromeGray,

            ChromeHigh = ChromeHigh,
            ChromeLow = ChromeLow,
            ChromeMedium = ChromeMedium,
            ChromeMediumLow = ChromeMediumLow,

            ChromeWhite = ChromeWhite,

            ListLow = ListLow,
            ListMedium = ListMedium,
            RegionColor = RegionColor
        };
        return pal;
    }

    [RelayCommand]
    private void SerializeSelectedPalett()
    {
        ColorPaletteResources pal = GetSelectedPalett();

        var txt = JsonSerializer.Serialize(pal, MyJsonContextColorPaletteResources.Default.ColorPaletteResources);
        File.WriteAllText(IGeneralApplicationData.ColorsPath, txt);
        _messageForUserTools.ShowSimpleMessageBoxInstance("please restart application");
    }
    [RelayCommand]
    private void BackToDefaults()
    {
        try
        {
            File.Delete(IGeneralApplicationData.ColorsPath);
        }
        catch (Exception ex)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
        }
        _messageForUserTools.ShowSimpleMessageBoxInstance("please restart application");
    }
    [RelayCommand]
    private void ChangeFontSize(object parametr)
    {
        if (parametr.ToString() == "+")
        {
            FluentThemeManager.MakeFontsBigger();
        }
        else if (parametr.ToString() == "-")
        {
            FluentThemeManager.MakeFontsSmaller();
        }
        else
        {
            FluentThemeManager.MakeFontsDefault();
        }
        _messageForUserTools.ShowSimpleMessageBoxInstance("please restart application");
    }

    public double LineSpacing
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.LineSpacing = LineSpacing;
            LineSpacingStr = $"current value: {LineSpacing:N2}";
        }
    }

    [ObservableProperty]
    public partial string LineSpacingStr { get; set; }

    public bool ShowDetailsButton
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.ShowDetailsButton = ShowDetailsButton;
        }
    }

    public string DocumentFontName
    {
        get;
        set
        {
            SetProperty(ref field, value);
            _generalApplicationData.Config.DocumentFontName = DocumentFontName;
            foreach (var (_, value1) in _generalApplicationData.GetDocumentsKeyValueCollection())
            {
                value1.HotDocumentViewModel?.ResetFontStyle?.Invoke();
            }
        }
    }

    [RelayCommand]
    private void ChangeLineSpacing(object parametr)
    {
        if (parametr.ToString() == "+")
        {
            LineSpacing += 0.01;
        }
        else if (parametr.ToString() == "-")
        {
            LineSpacing -= 0.01;
        }
        else
        {
            LineSpacing = 1.0;
        }

        if (LineSpacing > 1.2)
        {
            LineSpacing = 1.2;
        }
        if (LineSpacing < 0.8)
        {
            LineSpacing = 0.8;
        }
    }

    //TODO
    private void SetColor()
    {
        ColorPaletteResources pal = GetSelectedPalett();
        FluentThemeManager.StaticFluentThemeManager.Switch(-1, pal);  
    }

    private void GetColors()
    {
        var pal = JustyBase.Themes.FluentThemeManager.GetCurrentPalette();


        Accent = pal.Accent;
        AltHigh = pal.AltHigh;
        AltLow = pal.AltLow;
        AltMedium = pal.AltMedium;
        AltMediumHigh = pal.AltMediumHigh;
        AltMediumLow = pal.AltMediumLow;
        BaseHigh = pal.BaseHigh;
        BaseLow = pal.BaseLow;
        BaseMedium = pal.BaseMedium;
        BaseMediumHigh = pal.BaseMediumHigh;
        BaseMediumLow = pal.BaseMediumLow;

        ChromeBlackHigh = pal.ChromeBlackHigh;
        ChromeBlackLow = pal.ChromeBlackLow;
        ChromeBlackMedium = pal.ChromeBlackMedium;
        ChromeBlackMediumLow = pal.ChromeBlackMediumLow;

        ChromeDisabledHigh = pal.ChromeDisabledHigh;
        ChromeDisabledLow = pal.ChromeDisabledLow;
        ChromeGray = pal.ChromeGray;

        ChromeHigh = pal.ChromeHigh;
        ChromeLow = pal.ChromeLow;
        ChromeMedium = pal.ChromeMedium;
        ChromeMediumLow = pal.ChromeMediumLow;

        ChromeWhite = pal.ChromeWhite;

        ListLow = pal.ListLow;
        ListMedium = pal.ListMedium;
        RegionColor = pal.RegionColor;
    }


    [ObservableProperty]
    public partial object SeletedOption { get; set; }

}