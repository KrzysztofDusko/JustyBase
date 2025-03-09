using JustyBase.Common.Contracts;
using JustyBase.PluginCommon.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Velopack;

namespace JustyBase;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        var provider = CodePagesEncodingProvider.Instance;
        Encoding.RegisterProvider(provider);
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        //if (Directory.GetFiles(@"\\.\pipe\").Contains(JbMessagePipePath)) 
        if (File.Exists(JbMessagePipePath))
        {
            using StreamWriter streamWriter = new(JbMessagePipePath);
            //try to open next sql file from system (not JB inner option)
            if (args.Length == 1 && args[0].EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                streamWriter.WriteLine(args[0]);
            }
            streamWriter.WriteLine("RESTORE"); //send restore message to running instance
            return;
        }
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception globalException)
        {
            GetSimpleLogger?.TrackCrashMessagePlusOpenNotepad(globalException, "Global try_catch", true);
            if (globalException.InnerException is not null)
            {
                var innerException = globalException.InnerException;
                GetSimpleLogger?.TrackCrashMessagePlusOpenNotepad(innerException, "Global try_catch_inner", true);
                GetSimpleLogger?.TrackCrashAsync(innerException, true).Wait(TimeSpan.FromSeconds(5));
                if (innerException is System.Security.Cryptography.CryptographicException)
                {
                    GetMessagesService.ShowSimpleMessageBoxInstance(innerException);
                }
            }
            GetSimpleLogger?.TrackCrashAsync(globalException, true).Wait(TimeSpan.FromSeconds(5));
        }
    }


    private static IGeneralApplicationData GetGeneralApplicationData => App.GetRequiredService<IGeneralApplicationData>();
    private static ISimpleLogger GetSimpleLogger => App.GetRequiredService<ISimpleLogger>();
    private static IMessageForUserTools GetMessagesService => App.GetRequiredService<IMessageForUserTools>();


    public static void SetUpDispatcherExceptionHandling()
    {
        Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;
        Dispatcher.UIThread.UnhandledExceptionFilter += UIThread_UnhandledExceptionFilter;
    }
    private static void UIThread_UnhandledExceptionFilter(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
    {
        GetSimpleLogger.TrackCrashMessagePlusOpenNotepad(e.Exception.Message, "UIThread_UnhandledExceptionFilter", isCrash: true);
        GetMessagesService.ShowSimpleMessageBoxInstance(e.Exception);
    }

    private static void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        GetSimpleLogger.TrackCrashMessagePlusOpenNotepad(e.Exception.Message, "UIThread_UnhandledExceptionFilter", isCrash: true);
        GetMessagesService.ShowSimpleMessageBoxInstance(e.Exception);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        GetSimpleLogger.TrackCrashMessagePlusOpenNotepad(e?.ExceptionObject?.ToString() ?? "empty message", "CurrentDomain_UnhandledException_1", isCrash: true);
        if (e.ExceptionObject is Exception exception)
        {
            GetSimpleLogger.TrackCrashMessagePlusOpenNotepad(exception, "CurrentDomain_UnhandledException_2", true);
        }
        if (e.ExceptionObject is TypeInitializationException exp1 && exp1.InnerException is not null)
        {
            GetSimpleLogger.TrackCrashMessagePlusOpenNotepad(exp1, "CurrentDomain_UnhandledException_3", true);
            GetSimpleLogger.TrackCrashAsync(exp1, true).Wait(TimeSpan.FromSeconds(5));
        }

        GetSimpleLogger.TrackCrashAsync(new Exception(e?.ToString()), true).Wait(TimeSpan.FromSeconds(5));
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        //to actually observe the task, uncomment the below line of code
        e.SetObserved();
        Debug.WriteLine("TaskScheduler_UnobservedTaskException");

        GetGeneralApplicationData.SaveConfig();

        StringBuilder sb = new();

        sb.AppendLine($"""
                Unobserved Task Exception Message: 
                    {e.Exception.Message}
                Unobserved Task Exception StackTrace
                    {e.Exception.StackTrace}
                Unobserved Task Exception Source
                    {e.Exception.Source}
                ##### InnerExceptions start
                """);
        if (e.Exception.InnerExceptions is not null)
        {
            foreach (Exception exp1 in e.Exception.InnerExceptions)
            {
                sb.AppendLine("Unobserved Task Exception Message");
                sb.AppendLine(exp1.Message);

                sb.AppendLine("Unobserved Task Exception StackTrace");
                sb.AppendLine(exp1.StackTrace);

                sb.AppendLine("Unobserved Task Exception Source");
                sb.AppendLine(exp1.Source);
            }
        }
        sb.AppendLine("##### InnerExceptions end");

        string msgText = sb.ToString();
      
        if (!msgText.Contains("com.canonical.AppMenu.Registrar") && !IngoredErrorMessages.Contains(msgText))
        {
            GetSimpleLogger.TrackCrashMessagePlusOpenNotepad(sb.ToString(), "TaskScheduler_UnobservedTaskException UnobservedTaskException", isCrash: true);
            GetMessagesService.ShowSimpleMessageBoxInstance(msgText, "Error");
        }        
    }

    public static readonly HashSet<string> IngoredErrorMessages =
    [
        "Unobserved Task Exception Message: \r\n    A Task's exception(s) were not observed either by Waiting on the Task or accessing its Exception property. As a result, the unobserved exception was rethrown by the finalizer thread. (Operacja We/Wy została przerwana z powodu zakończenia wątku lub żądania aplikacji.)\r\nUnobserved Task Exception StackTrace\r\n    \r\nUnobserved Task Exception Source\r\n    \r\n##### InnerExceptions start\r\nUnobserved Task Exception Message\r\nOperacja We/Wy została przerwana z powodu zakończenia wątku lub żądania aplikacji.\r\nUnobserved Task Exception StackTrace\r\n   at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.ThrowException(SocketError error, CancellationToken cancellationToken)\r\n   at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token)\r\n   at System.Threading.Tasks.ValueTask.ValueTaskSourceAsTask.<>c.<.cctor>b__4_0(Object state)\r\nUnobserved Task Exception Source\r\nSystem.Net.Sockets\r\n##### InnerExceptions end\r\n"
    ];

    public const string JbMessagePipeName = @"JUST_X";
    public const string JbMessagePipePath = $@"\\.\pipe\{JbMessagePipeName}";

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        //.UseReactiveUI()
        //.WithInterFont()
        .LogToTrace();
}
