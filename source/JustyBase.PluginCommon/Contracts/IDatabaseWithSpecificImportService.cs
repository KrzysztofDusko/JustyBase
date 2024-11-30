using JustyBase.PluginCommon.Enums;
using JustyBase.StringExtensions;

namespace JustyBase.PluginCommon.Contracts;

public interface IDatabaseWithSpecificImportService
{
    public DatabaseTypeEnum DatabaseType { get; init; }

    Task DbSpecificImportPart(IDbImportJob importJob, string randName, Action<string>? progress,
        bool tableExists = false);

    async Task<string> PerformImportFromXmlAsync(IDbXMLImportJob importJob, object data,
        Action<string>? messageAction)
    {
        var randName = StringExtension.RandomSuffix("IMP_");
        try
        {
            await importJob.AnalyzeXmlClipboardDataAndStoreLines(data);
            await DbSpecificImportPart(importJob, randName, messageAction);
        }
        catch (Exception ex)
        {
            randName = ex.Message;
        }

        return randName;
    }
}