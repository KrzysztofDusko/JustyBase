using JustyBase.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.Common.Contracts;

public interface IRuntimeDocumentsContainer
{
    /// <summary>
    /// returns document id
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    string AddNewDocument(string title);
    bool TryGetOpenedDocumentVmByFilePath(string path, out IHotDocumentVm? openedVm);
    protected static string NewDocumentId => $"DOC_ID_{Guid.NewGuid()}";
    bool RemoveDocumentById(string id);
    OfflineTabData GetDocumentVmById(string id);
    IEnumerable<KeyValuePair<string, OfflineTabData>> GetDocumentsKeyValueCollection();
    bool TryGetDocumentById(string id, out OfflineTabData savedTabData);
    int GetDocumentIndexById(string id);
    void AddProblemDocument(string id, IHotDocumentVm documentViewModel);

    public OfflineDocumentContainer GetOfflineDocumentContainer(string selectedTabId);
}
