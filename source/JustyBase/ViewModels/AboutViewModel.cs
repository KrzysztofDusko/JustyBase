using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using JustyBase.Common.Helpers;
using JustyBase.PluginCommon.Contracts;
using JustyBase.Services;
using JustyBase.Views.OtherDialogs;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Velopack;

namespace JustyBase.ViewModels;

public sealed partial class AboutViewModel : ObservableObject
{
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly ISimpleLogger _simpleLogger;
    private readonly IOtherHelpers _otherHelpers;
    private readonly IMessageForUserTools _messageForUserTools;
    public AboutViewModel(IGeneralApplicationData generalApplicationData, IAvaloniaSpecificHelpers avaloniaSpecificHelpers, IOtherHelpers otherHelpers,
        ISimpleLogger simpleLogger, IMessageForUserTools messageForUserTools)
    {
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        _generalApplicationData = generalApplicationData;
        _otherHelpers = otherHelpers;
        _simpleLogger = simpleLogger;
        _messageForUserTools = messageForUserTools;

        CurrentVersionText = _generalApplicationData.GetCurrentCopyVersion();
        if (!string.IsNullOrWhiteSpace(_generalApplicationData.DownloadPluginsBasePath))
        {
            _mgr = new UpdateManager(_generalApplicationData.DownloadPluginsBasePath);
        }

    }

    [ObservableProperty]
    public partial bool IsUpdateAvaiable { get; set; } = false;

    [ObservableProperty]
    public partial string VersionText { get; set; }

    [ObservableProperty]
    public partial string CurrentVersionText { get; set; }

    [ObservableProperty]
    public partial string WaringText { get; set; }

    private static UpdateManager? _mgr;
    private static UpdateInfo _newVersion;
    public static async Task<UpdateInfo> GetUpdateInfo()
    {
        if (_mgr is null || !_mgr.IsInstalled)
        {
            return null;
        }
        try
        {
            _newVersion = await _mgr.CheckForUpdatesAsync().ConfigureAwait(true);
        }
        catch { }

        return _newVersion;
    }

    [RelayCommand]
    private async Task CheckVersion()
    {
        if (_mgr is null)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("provide JB_DOWNLOAD_BASE_PATH environment variable to update");
            return;
        }

        try
        {
            if (_mgr.IsInstalled)
            {
                _newVersion = await _mgr.CheckForUpdatesAsync();
            }
        }
        catch (Exception ex)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
        }
        UpdateStatus();
    }

    [RelayCommand]
    private void DownloadPlugins()
    {
        _generalApplicationData.Config.ResetPlugins = true;
        _messageForUserTools.ShowSimpleMessageBoxInstance("Please restart application");
    }

    private void UpdateStatus()
    {
        if (_mgr is null)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("provide DEBUG_PLUGIN_BASE_PATH environment variable to update");
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine($"Velopack version: {VelopackRuntimeInfo.VelopackNugetVersion}");
        sb.AppendLine($"This app version: {(_mgr.IsInstalled ? _mgr.CurrentVersion : "(n/a - not installed)")}");

        if (_newVersion != null)
        {
            sb.AppendLine($"Update available: {_newVersion.TargetFullRelease.Version}");
            IsUpdateAvaiable = true;
        }
        else
        {
            IsUpdateAvaiable = false;
        }

        if (_mgr.UpdatePendingRestart is not null)
        {
            sb.AppendLine("Update ready, pending restart to install");
        }

        _messageForUserTools.ShowSimpleMessageBoxInstance(sb.ToString());
    }

    private void Progress(int percent)
    {
        VersionText = $"Downloading ({percent}%)...";
    }

    private void Working()
    {
        IsUpdateAvaiable = false;
        VersionText = "Working...";
    }
    private async Task DownloadUpdate()
    {
        if (!_mgr.IsInstalled)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("n/a - not installed");
            return;
        }
        Working();
        try
        {
            await _mgr.DownloadUpdatesAsync(_newVersion, Progress);
        }
        catch (Exception ex)
        {
#if !DEBUG
            _simpleLogger.TrackCrashMessagePlusOpenNotepad(ex, "Update check error", isCrash: false);
#else
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
#endif
        }
    }

    /// <summary>
    /// mitigate paloalto
    /// </summary>
    /// <returns></returns>
    //private async Task DownloadUpdateMitigatePaloAlto()
    //{
    //    Working();

    //    try
    //    {
    //        var fileName = _newVersion.TargetFullRelease.FileName;
    //        var filePath = $"{_generalApplicationData.DownloadPluginsBasePath}{_newVersion.TargetFullRelease.FileName}";
    //        var di = new DirectoryInfo(Environment.ProcessPath);
    //        var parentDirectory = di.Parent.Parent;
    //        var dirToDownload = Path.Combine(parentDirectory.FullName, "packages");
    //        if (Directory.Exists(dirToDownload))
    //        {
    //            await _otherHelpers.DownloadFileWithReverse(filePath + ".rev", Path.Combine(dirToDownload, fileName));
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _simpleLogger.TrackCrashMessagePlusOpenNotepad(ex, "Update check error", isCrash: true);
    //        _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
    //    }
    //}

    [RelayCommand]
    public async Task Update()
    {
        if (_mgr is null)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("provide DEBUG_PLUGIN_BASE_PATH environment variable to update");
            return;
        }
        if (!_mgr.IsInstalled)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("Not installed");
            return;
        }
        if (!_generalApplicationData.Config.UpdateMitigateNextGenFirewalls)
        {
            await DownloadUpdate();
            UpdateStatus();
        }
        else
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("MitigateNextGenFirewalls is not longer supported");
            return;
            //await DownloadUpdateMitigatePaloAlto();
        }

        //var releaseNotesMd = _newVersion.TargetFullRelease.NotesMarkdown;

        var avm = new AskForConfirmViewModel()
        {
            Title = "Question",
            TextMessage = $"Update now?"
        };
        await new AskForConfirm()
        {
            DataContext = avm
        }.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());

        if (avm.ResultAsString == "Yes")
        {
            _mgr.ApplyUpdatesAndRestart(_newVersion);
        }
        else
        {
            _mgr.WaitExitThenApplyUpdates(_newVersion, silent: true, restart: false);
        }
    }
}