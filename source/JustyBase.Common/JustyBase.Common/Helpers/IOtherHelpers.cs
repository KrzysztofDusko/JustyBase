
namespace JustyBase.Common.Helpers
{
    public interface IOtherHelpers
    {
        string CsvTxtPreviewer(string path);
        Task DownloadAllPlugins(string pluginDirectory, string downloadBasePath);
    }
}