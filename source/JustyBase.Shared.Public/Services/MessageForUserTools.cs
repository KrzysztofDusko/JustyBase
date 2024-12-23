using JustyBase.Common.Contracts;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace JustyBase.Helpers.Interactions;

public sealed partial class MessageForUserTools : IMessageForUserTools
{
    private static readonly MessageForUserTools Instance = new();
    public static void DispatcherAction(Action actionToDispatch)
    {
        Instance.DispatcherActionInstance(actionToDispatch);
    }

    public void ShowOrShowInExplorerHelper(string path, string? argOverRide = null)
    {
        if (OperatingSystem.IsWindows() && path is not null && (File.Exists(path) || Directory.Exists(path)))
        {
            using Process showInExplorer = new();
            showInExplorer.StartInfo.FileName = "explorer";
            showInExplorer.StartInfo.Arguments = $"/select, \"{path}\"";
            if (argOverRide is not null)
            {
                showInExplorer.StartInfo.Arguments = argOverRide;
            }
            showInExplorer.Start();
        }
    }
    public void OpenInExplorerHelper(string path)
    {
        ShowOrShowInExplorerHelper(path, $"\"{path}\"");
    }


    // To support flashing.
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FlashWindowEx(ref FLASHWINFO pwfi);
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

    private static void FlashWindowExIfNeededByHwnd(nint hWnd)
    {
        FLASHWINFO fInfo = new FLASHWINFO();
        fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
        fInfo.hwnd = hWnd;
        fInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
        fInfo.uCount = UInt32.MaxValue;
        fInfo.dwTimeout = 0;
        FlashWindowEx(ref fInfo);
    }

    public void ScreenShot()
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
                ShowSimpleMessageBoxInstance(ex);
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
                    ShowSimpleMessageBoxInstance(ex);
                }
            }
        }
    }

    void IMessageForUserTools.ShowSimpleMessageBoxInstance(string messageForUser, string title)
    {
        ShowSimpleMessageBox(messageForUser, title);
    }

}