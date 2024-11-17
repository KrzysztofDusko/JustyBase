namespace JustyBase.PluginCommon.Contracts;

public interface IDbXMLImportJob : IDbImportJob
{
    Task AnalyzeXmlClipboardDataAndStoreLines(object someData, Action<string>? messageAction = null);
    void SetTypedValue(int columnNumber, bool isBoolean = false);
}