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


public interface ISomeEditorOptions
{
    Dictionary<string, (string snippetType, string? Description, string? Text, string? Keyword)> GetAllSnippets {  get; }
    Dictionary<string, string> FastReplaceDictionary { get; }
    List<string> TypoPatternList { get; }
    Dictionary<string, string> VariablesDictStatic { get; set; }
    bool CollapseFoldingOnStartup { get; }

    public static readonly Dictionary<string, (string name, string assetName, bool isXml)> REGISTERED_EXTENSIONS = new (StringComparer.OrdinalIgnoreCase)
{
            {".sql", ("SQL","SQL-Mode.xshd",false)},
            {".cs", ("CS","CSharp-Mode.xshd",false)},
            {".py", ("PY","Python-Mode.xshd",false)},
            {".ps1", ("PS1","PowerShell.xshd",false)},
            {".vb", ("VB", "VB-Mode.xshd", false)},
            {".json", ("JSON", "Json.xshd", false)},
            {".xml", ("XML", "XML-Mode.xshd", true)},
            {".html", ("HTML", "HTML-Mode.xshd", true) },
            {".css", ("CSS", "HTML-Mode.xshd", false) },
            {".js", ("CSS", "MarkDown-Mode.xshd", false) },
            {".dtsx", ("DTSX", "HTML-Mode.xshd", true) },
            {".txt", ("TXT", "MarkDown-Mode.xshd", false) }
};
}

