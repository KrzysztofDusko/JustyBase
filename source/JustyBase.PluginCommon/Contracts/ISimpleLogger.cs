namespace JustyBase.PluginCommon.Contracts;

public interface ISimpleLogger
{
    void Dispose();
    Task TrackCrashAsync(Exception ex, bool isCrash);
    void TrackCrashMessagePlusOpenNotepad(string message, string type, bool isCrash);
    void TrackError(Exception ex, bool isCrash);
    void TrackCrashMessagePlusOpenNotepad(Exception ex, string type, bool isCrash);
    void OpenMessageInNotepad(string message);
}