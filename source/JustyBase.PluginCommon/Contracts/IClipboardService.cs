namespace JustyBase.PluginCommon.Contracts;

public interface IClipboardService
{
    Task<string> GetTextAsync();
    Task SetTextAsync(string txt);
    Task<object?> GetDataAsync(string format);
    Task<string[]> GetFormatsAsync();
}
