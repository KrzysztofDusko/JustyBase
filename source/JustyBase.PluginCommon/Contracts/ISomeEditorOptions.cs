namespace JustyBase.PluginCommon.Contracts;
public interface ISomeEditorOptions
{
    const int DEFAULT_DOCUMENT_FONT_SIZE = 13;
    Dictionary<string, (string snippetType, string? Description, string? Text, string? Keyword)> GetAllSnippets { get; }
    Dictionary<string, string> FastReplaceDictionary { get; }
    List<string> TypoPatternList { get; }
    Dictionary<string, string> VariablesDictionary { get; set; }
    bool CollapseFoldingOnStartup { get; }

    static readonly Dictionary<string, (string name, string assetName, bool isXml)> REGISTERED_EXTENSIONS = new(StringComparer.OrdinalIgnoreCase)
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

    bool IsFromatterAvaiable { get; set; }

    Task<string> GetFormatterSql(string txt);
}

