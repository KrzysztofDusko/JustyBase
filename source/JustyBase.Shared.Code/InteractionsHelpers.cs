using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace JustyBase.Helpers.Interactions;

public sealed partial class InteractionsHelpers
{
    private readonly static InteractionsHelpers _instance = new InteractionsHelpers();

    public static void DispatcherAction(Action actionToDispatch)
    {
        _instance.DispatcherActionInstance(actionToDispatch);
    }

    public static void FlashWindowExIfNeeded()
    {
        _instance.FlashWindowExIfNeededInstance();
    }

    public static void ShowSimpleMessageBox(Exception ex)
    {
        _instance.ShowSimpleMessageBoxInstance(ex);
    }



    // To support flashing.
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
    //Flash both the window caption and taskbar button.
    //This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
    public const UInt32 FLASHW_ALL = 3;

    // Flash continuously until the window comes to the foreground. 
    public const UInt32 FLASHW_TIMERNOFG = 12;
    [StructLayout(LayoutKind.Sequential)]
    public struct FLASHWINFO
    {
        public UInt32 cbSize;
        public IntPtr hwnd;
        public UInt32 dwFlags;
        public UInt32 uCount;
        public UInt32 dwTimeout;
    }

    public static void FlashWindowExIfNeeded2(nint hWnd)
    {
        FLASHWINFO fInfo = new FLASHWINFO();
        fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
        fInfo.hwnd = hWnd;
        fInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
        fInfo.uCount = UInt32.MaxValue;
        fInfo.dwTimeout = 0;
        FlashWindowEx(ref fInfo);
    }
    public static void ShowOrShowInExplorerHelper(string path, string argOverRide = null)
    {
        if (OperatingSystem.IsWindows() && path is not null && (File.Exists(path) || Directory.Exists(path)))
        {
            using Process showInExplorer = new Process();
            showInExplorer.StartInfo.FileName = "explorer";
            showInExplorer.StartInfo.Arguments = $"/select, \"{path}\"";
            if (argOverRide is not null)
            {
                showInExplorer.StartInfo.Arguments = argOverRide;
            }
            showInExplorer.Start();
        }
    }
    public static void OpenInExplorerHelper(string path)
    {
        ShowOrShowInExplorerHelper(path, $"\"{path}\"");
    }
    public static void ScreenShot(Action<Exception> exceptionMessage)
    {
        if (OperatingSystem.IsWindows())
        {
            bool succes = false;
            try
            {
                using Process screenClip = new Process();
                screenClip.StartInfo.FileName = "explorer";
                screenClip.StartInfo.Arguments = "ms-screenclip:";
                screenClip.Start();
                succes = true;
            }
            catch (Exception ex)
            {
                exceptionMessage(ex);
            }

            if (!succes)
            {
                try
                {
                    using Process screenClip = new Process();
                    screenClip.StartInfo.FileName = "SnippingTool.exe";
                    screenClip.StartInfo.Arguments = "/clip";
                    screenClip.Start();
                    succes = false;
                }
                catch (Exception ex)
                {
                    exceptionMessage(ex);
                }
            }
        }
    }
}