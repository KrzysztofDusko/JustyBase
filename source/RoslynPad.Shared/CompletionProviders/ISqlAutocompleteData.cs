using System;
using System.Collections.Generic;

namespace JustyBase.Editor;

public interface ISqlAutocompleteData
{
    IAsyncEnumerable<CompletionDataSql> GetWordsList(string input, Dictionary<string, List<string>> aliasDbTable, Dictionary<string, List<string>> subqueriesHints
        , Dictionary<string, List<string>> withs
        , Dictionary<string, List<string>> tempTables
        //, IEnumerable<string> betweenSelectAndFrom
        );
}
