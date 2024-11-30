using System;

namespace JustyBase.Editor;

//https://stackoverflow.com/questions/9223674/highlight-all-occurrences-of-selected-word-in-avalonedit
public class MarkSameWord : DocumentColorizingTransformer
{
    private readonly string _selectedText;

    public MarkSameWord(string selectedText)
    {
        _selectedText = selectedText;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (string.IsNullOrEmpty(_selectedText))
        {
            return;
        }

        int lineStartOffset = line.Offset;
        string text = CurrentContext.Document.GetText(line);
        int start = 0;
        int index;
        //var res = App.Current.FindResource("MyTabBackgroundColor");
        while ((index = text.IndexOf(_selectedText, start, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            ChangeLinePart(
                lineStartOffset + index, // startOffset
                lineStartOffset + index + _selectedText.Length, // endOffset
                //element => element.TextRunProperties.BackgroundBrush = Brushes.LightSkyBlue
                element => element.BackgroundBrush = Brushes.Gray
                //element => element.BackgroundBrush = res as IBrush
                );
            start = index + 1; // search for next occurrence
        }
    }
}

