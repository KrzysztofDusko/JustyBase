using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.Common.Contracts;

public interface ISavedTabData
{
    string InitTile { get; set; }
    string InitSqlText { get; set; }
    string InitFilePath { get; set; }
    int InitConnectionIndex { get; set; }

    int FontSize { get; set; }
    string GetFilePathFromDocumentVM { get; }

    object DocumentViewModel { get; set; }

    T? DocumentViewModelAsT<T>() where T : class
        => DocumentViewModel as T;

    string GetTextFromDocumentVM();
    string GetTitleFromDocumentVM();
    void RemoveAsterixFromTitleFromDocumentVM();
}
