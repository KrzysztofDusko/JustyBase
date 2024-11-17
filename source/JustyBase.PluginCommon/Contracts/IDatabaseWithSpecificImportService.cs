using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Extensions;

namespace JustyBase.PluginCommon.Contracts;

public interface IDatabaseWithSpecificImportService
{
    public DatabaseTypeEnum DatabaseType { get; init; }

    Task DbSpecificImportPart(IDbImportJob importJob, string randName, Action<string>? progress,
        bool tableExists = false);

    async Task<string> PerformImportFromXmlAsync(IDbXMLImportJob importJob, object data,
        Action<string>? messageAction)
    {
        var randName = StringExtension2.RandomName("IMP_");
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