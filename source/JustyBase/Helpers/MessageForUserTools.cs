using JustyBase.PluginCommon.Contracts;
using JustyBase.Services;
using JustyBase.Views;
using System;
using System.Diagnostics;

namespace JustyBase.Helpers.Interactions;

public sealed partial class MessageForUserTools
{
    public static void ShowSimpleMessageBox(string messageForUser, string title = "Information", Window window = null)
    {
        ShowSimpleMessageBoxInstance(messageForUser, title, window);
    }

    public void ShowSimpleMessageBoxInstance(Exception ex)
    {
        if (ex.Message.StartsWith("ORA")) // TODO proper detection of "standard" messages
        {
            ShowSimpleMessageBoxInstance($"Message\r\n{ex.Message}", "Error", null);
        }
        else
        {
            ShowSimpleMessageBoxInstance($"Message\r\n{ex.Message}\r\nStack trace\r\n{ex.StackTrace}", "Error", null);
        }
    }

    private static void ShowSimpleMessageBoxInstance(string messageForUser, string title = "Information", Window window = null)
    {
        var avaloniaSpecificHelpers = App.GetRequiredService<IAvaloniaSpecificHelpers>();
        DispatcherAction(() =>
        {
            try
            {
                new MessageWindow(messageForUser, title) { WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog(window ?? avaloniaSpecificHelpers.GetMainWindow());
            }
            catch (Exception)
            {
                Debug.Assert(false);
            }
        });
    }

    public void FlashWindowExIfNeeded()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        nint hWnd = 0;
        var res = true;
        DispatcherAction(() =>
        {
            var mv = App.GetRequiredService<IAvaloniaSpecificHelpers>().GetMainWindow();
            if (mv.WindowState != WindowState.Minimized)
            {
                res = false;
            }
            else
            {
                hWnd = TopLevel.GetTopLevel(mv).TryGetPlatformHandle().Handle;
            }

            if (!res)
            {
                return;
            }
            MessageForUserTools.FlashWindowExIfNeededByHwnd(hWnd);
        });
    }

    public void DispatcherActionInstance(Action actionToDispatch)
    {
        Dispatcher.UIThread.Post(() =>
        //https://github.com/KrzysztofDusko/JustDataEvoProject/issues/153
        {
            try
            {
                actionToDispatch?.Invoke();
            }
            catch (Exception ex)
            {
                App.GetRequiredService<ISimpleLogger>().TrackCrashMessagePlusOpenNotepad(ex, "Error", false);
            }
        });
    }

    public void DispatcherActionInstance(Action actionToDispatch, object dispatcherPriority)
    {
        Dispatcher.UIThread.Post(() =>
        //https://github.com/KrzysztofDusko/JustDataEvoProject/issues/153
        {
            try
            {
                actionToDispatch?.Invoke();
            }
            catch (Exception)
            {
            }
        }, priority: (DispatcherPriority)dispatcherPriority);
    }
}
