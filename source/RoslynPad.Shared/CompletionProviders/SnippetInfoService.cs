using System.Collections.Generic;
using System.Linq;

namespace JustyBase.Editor;

internal sealed class SnippetInfoService
{
    private readonly ISomeEditorOptions _someEditorOptions;
    public SnippetInfoService(ISomeEditorOptions someEditorOptions)
    {
        _someEditorOptions = someEditorOptions;
        SnippetManager = new SnippetManager(_someEditorOptions); 
    }
    public SnippetManager SnippetManager { get; }

    public IEnumerable<SnippetInfo> GetSnippets()
    {
        return SnippetManager.Snippets.Select(x => new SnippetInfo(x.Name, x.Name, x.Description));
    }
}

public sealed class SnippetInfo
{
    public string Shortcut { get; }

    public string Title { get; }

    public string Description { get; }

    public SnippetInfo(string shortcut, string title, string description)
    {
        Shortcut = shortcut;
        Title = title;
        Description = description;
    }
}
