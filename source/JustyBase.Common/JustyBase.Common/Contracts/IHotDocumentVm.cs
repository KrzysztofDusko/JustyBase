namespace JustyBase.Common.Contracts;

public interface IHotDocumentVm
{
    string Id { get; }
    int SelectedConnectionIndex { get; }
    int FontSize { get; }
    string FilePath { get;}
    string? TextFromDocumentVM { get; }
    string TitleFromDocumentVm { get; }
    void RemoveAsterixFromTitleFromDocumentVM();
    Action ResetFontStyle { get; set; }
}
