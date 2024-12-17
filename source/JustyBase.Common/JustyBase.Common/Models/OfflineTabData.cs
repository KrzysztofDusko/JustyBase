using JustyBase.Common.Contracts;
using JustyBase.PluginCommon.Contracts;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JustyBase.Common.Models;

public sealed class OfflineTabData
{
    public required string MyId { get; set; }
    public required string Title { get; set; }
    public required string? SqlText { get; set;} // SqlText or SqlFilePath will be saved
    public required string? SqlFilePath { get; set;} // SqlText or SqlFilePath will be saved
    public int ConnectionIndex { get; set; } = -1;
    public int FontSize { get; set; } = ISomeEditorOptions.DEFAULT_DOCUMENT_FONT_SIZE;

    [JsonIgnore]
    public IHotDocumentVm? HotDocumentViewModel { get; set; }

    public T? HotDocumentViewModelAsT<T>() where T : class
    {
        return HotDocumentViewModel as T;
    }

    public bool RefreshDocumentColdState()
    {
        if (HotDocumentViewModel is null)
        {
            Debug.Assert(false);
            return false;
        }

        string fileName = HotDocumentViewModel.FilePath;
        HotDocumentViewModel.RemoveAsterixFromTitleFromDocumentVM();
        Title = HotDocumentViewModel.TitleFromDocumentVm;
        ConnectionIndex = HotDocumentViewModel.SelectedConnectionIndex;
        FontSize = HotDocumentViewModel.FontSize;

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            SqlFilePath = fileName;
            SqlText = null;
        }
        else
        {
            var sqlTxt = HotDocumentViewModel.TextFromDocumentVM ?? SqlText;
            SqlFilePath = null;
            SqlText = sqlTxt;
        }
        return true;
    }
}
