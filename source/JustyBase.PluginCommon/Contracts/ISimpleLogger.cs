using System;

namespace JustyBase.PluginCommon.Contracts;

public interface ISimpleLogger
{
    void Dispose();
    Task TrackCrashAsync(Exception ex, bool isCrash);
    void TrackCrashMessagePlusOpenNotepad(string message, string type, bool isCrash);
    void TrackError(Exception ex, bool isCrash);
    void TrackCrashMessagePlusOpenNotepad(Exception ex, string type, bool isCrash);
    void OpenMessageInNotepad(string message);

    static ISimpleLogger EmptyLogger => new EmptyLogger();
}

public sealed class EmptyLogger : ISimpleLogger
{
    public void Dispose()
    {
        
    }
    private string _tempPath = Path.GetTempPath();
    public void OpenMessageInNotepad(string message)
    {
        try
        {
            var filepath = Path.Combine(_tempPath, "message_from_jb");
            File.WriteAllText(filepath, message);

            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start("notepad.exe", filepath);
            }
        }
        catch (Exception)
        {
        }
    }

    public Task TrackCrashAsync(Exception ex, bool isCrash)
    {
        return Task.CompletedTask;
    }

    public void TrackCrashMessagePlusOpenNotepad(string message, string type, bool isCrash)
    {
        OpenMessageInNotepad(
        $"""
            isCrash : {isCrash}
            type : {type}
            message : {message}
        """);
    }

    public void TrackCrashMessagePlusOpenNotepad(Exception ex, string type, bool isCrash)
    {
        TrackCrashMessagePlusOpenNotepad(
        $"""
            MESSAGE
                {ex.Message}
            STACKTRACE
                {ex.StackTrace}
        """, type, isCrash);
    }

    public void TrackError(Exception ex, bool isCrash)
    {

    }
}