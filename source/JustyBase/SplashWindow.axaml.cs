using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace JustyBase;

public partial class SplashWindow : Window
{
    private readonly Action? _mainAction;

    public SplashWindow() : this(null)
    {
    }

    public SplashWindow(Action? mainAction)
    {
        InitializeComponent();
        _mainAction = mainAction;
        SetLottie();
    }

    private static readonly List<(int timeInMs, string assetPath)> StartupOptions =
        [
            (1_000, @"avares://JustyBase/Assets/gears.json"),
            //(1_500, @"avares://JustyBase/Assets/done.json"),
            //(500, @"avares://JustyBase/Assets/infinite_rainbow.json"),
            //(1_000, @"avares://JustyBase/Assets/preloader.json"),
            //(1_500, @"avares://JustyBase/Assets/rejection.json"),
            (3_000, @"avares://JustyBase/Assets/frog.json"),
            //(3_600, @"avares://JustyBase/Assets/rocket.json"),
            (1_650, @"avares://JustyBase/Assets/loading.json"),
            (2_300, @"avares://JustyBase/Assets/welcome.json")
        ];

    //public static bool SpecialWasShown = false;


    public static bool IsValentine()
    {
        return DateTime.Now.Month == 2 && DateTime.Now.Day == 14;
    }
    private void SetLottie()
    {
        if (IsValentine())
        {
            //SpecialWasShown = true;
            Background = Brushes.Transparent;
            SystemDecorations = SystemDecorations.None;
            ExtendClientAreaToDecorationsHint = false;
            Width *= 2.0;
            Height *= 2.0;
            _timeToWait = 4_000;
            Lottie.RepeatCount = 2;
            Lottie.Path = @"avares://JustyBase/Assets/Hearth1.json";
        }
        else
        {
            var nm = Random.Shared.Next(0, StartupOptions.Count);
            _timeToWait = StartupOptions[nm].timeInMs;
            Lottie.Path = StartupOptions[nm].assetPath;
        }
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        await DummyLoad();
    }
    private int _timeToWait = 1_500;
    private async Task DummyLoad()
    {
        if (OperatingSystem.IsWindows())
        {
            SetFileTypeAssociation();
        }
        // Do some background stuff here.
        await Task.Delay(_timeToWait);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                _mainAction?.Invoke();
                Close();
            }
            catch (Exception)
            {
                // ignored
            }
        });
    }



    [LibraryImport("Shell32.dll")]
    private static partial int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

    private static void SetFileTypeAssociation()
    {
        if (!OperatingSystem.IsWindows()) return;
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JustyBase", "current", "JustyBase.exe") + " \"%1\"";
        if ((string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Classes\\.sql", "", "JustyBase")! == "JustyBase")
            return;

        Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\JustyBase", "", "sql");
        Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\JustyBase", "sql files", "sql");
        Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\JustyBase\\shell\\open\\command", "", path);
        Registry.SetValue("HKEY_CURRENT_USER\\Software\\Classes\\.sql", "", "JustyBase");

        // This call notifies Windows that it needs to redo the file associations and icons
        _ = SHChangeNotify(0x08000000, 0x2000, IntPtr.Zero, IntPtr.Zero);
    }
}
