// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Frozen;
using System.Collections.Generic;

namespace JustyBase.Editor;

public sealed class SnippetManager
{
    private readonly FrozenDictionary<string, CodeSnippet> DefaultSnippets;

    private readonly ISomeEditorOptions _someEditorOption;
    public SnippetManager(ISomeEditorOptions someEditorOption)
    {
        _someEditorOption = someEditorOption;
        List<CodeSnippet> snippets = GetGeneralSnippets();

        DefaultSnippets = snippets.ToFrozenDictionary(x => x.Name);
    }

    public IEnumerable<CodeSnippet> Snippets => DefaultSnippets.Values;

    public CodeSnippet? FindSnippet(string name)
    {
        DefaultSnippets.TryGetValue(name, out var snippet);
        return snippet;
    }
    private List<CodeSnippet> GetGeneralSnippets()
    {
        var snippets = new List<CodeSnippet>();
        if (_someEditorOption is not null)
        {
            foreach (var item in _someEditorOption.GetAllSnippets)
            {
                snippets.Add(new CodeSnippet
                    (
                    item.Key,
                    item.Value.Description,
                    item.Value.Text ?? item.Key,
                    item.Value.Keyword
                    )
                );
            }
        }

        return snippets;
    }
}
