using System;
using JustyBase.Editor.CompletionProviders;

namespace JustyBase.Editor;
public sealed class CompletionDataSql(string text, string desc, bool isSelected, Glyph glyph, SnippetManager? snippetManager) 
    : ICompletionDataEx
{
    private readonly SnippetManager? _snippetManager = snippetManager;

    private readonly Glyph _glyph = glyph;

    public bool IsSelected { get; } = isSelected;

    public string SortText => throw new NotImplementedException();

    public string Text { get; } = text;

    public object Content => Text;

    public object Description { get; } = desc;

    public double Priority { get; } = 0;

    public bool AutocompleteOnReturn {  get; set; }

    public CommonImage Image => _glyph.ToImageSource();

    //[SkipLocalsInit]
    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
    {
        if (_glyph == Glyph.Snippet && CompleteSnippet(textArea, completionSegment, e))
        {
            return;
        }

        //https://github.com/KrzysztofDusko/JustDataEvoProject/issues/112
        if (e is CommonTextEventArgs inputEventArgs && inputEventArgs.Text?.Length > 0)
        {
            return;
        }
        if (e is KeyEventArgs kea && kea?.Key == Key.Return && !AutocompleteOnReturn)
        { 
            return;
        }

        //??
        /*
        if (Text.Length >= completionSegment.Length && completionSegment.Length > 0)
        {
            int start = completionSegment.Offset;
         
            while (start > 0 && start > completionSegment.Offset - 10)
            {
                bool res = true;
                for (int i = 0; i < completionSegment.Length; i++)
                {
                    var c = char.ToUpperInvariant(Text[i]);
                    var d = char.ToUpperInvariant(textArea.Document.GetCharAt(start + i));
                    if (d == ' ' || d == '\n' || d == '\t' || d == '(' || d == '.')
                    {
                        start++;
                        res = true;
                        break;
                    }
                    if (c != d)
                    {
                        res = false;
                        break;
                    }
                }
                if (res)
                {
                    break;
                }
                start--;
            }
            if (start>=0 && start != completionSegment.Offset)
            {
                int newLen = completionSegment.Offset - start + completionSegment.Length;
                completionSegment = new AnchorSegment(textArea.Document, start, newLen);
            }
        }
        else if (completionSegment.Length == 0)
        {
            Span<char> chars = stackalloc char[128];
            int len = EditorHelpers.GetLastWord(textArea, chars, strict:true);
            if (len < 127)
            {
                completionSegment = new AnchorSegment(textArea.Document, completionSegment.Offset - len, completionSegment.Length + len);
            }
        }
        */
        textArea.Document.Replace(completionSegment, Text);
    }

    private bool CompletSnippetOnEnter(EventArgs e)
    { 
        return AutocompleteOnReturn && e is KeyEventArgs keyEventArgs && keyEventArgs.Key == Key.Return;
    }

    private bool CompleteSnippet(TextArea textArea, ISegment completionSegment, EventArgs e)
    {
        char? completionChar = null;
        var txea = e as CommonTextEventArgs;
        if (txea != null && txea.Text?.Length > 0)
            completionChar = txea.Text[0];
        else if (e is KeyEventArgs kea && kea.Key == Key.Tab)
            completionChar = '\t';

        if (completionChar == '\t' || CompletSnippetOnEnter(e))
        {
            var snippet =  _snippetManager?.FindSnippet(Text);
            if (snippet != null)
            {
                var editorSnippet = snippet.CreateAvalonEditSnippet();
                using (textArea.Document.RunUpdate())
                {
                    int tmpOffset = completionSegment.Offset;
                    int tmpLength = completionSegment.Length;
                    if (tmpOffset>=1 && textArea.Document.GetCharAt(tmpOffset - 1) == '@')
                    {
                        tmpOffset--;
                        tmpLength++;
                    }

                    textArea.Document.Remove(tmpOffset, tmpLength);
                    editorSnippet.Insert(textArea);
                }
                if (txea != null)
                {
                    txea.Handled = true;
                }

                return true;
            }
        }

        return false;
    }
}
