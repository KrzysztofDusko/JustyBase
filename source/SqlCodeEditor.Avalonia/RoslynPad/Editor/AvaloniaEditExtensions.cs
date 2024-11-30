namespace JustyBase.Editor;

public static class AvaloniaEditExtensions
{
    public static bool IsOpen(this CompletionWindowBase window) => window?.IsEffectivelyVisible == true;

    public static void MakeSimillar(SqlCodeEditor source, SqlCodeEditor desitnation)
    {
        //desitnation.SyntaxHighlighting = source.SyntaxHighlighting;
        //desitnation.Document = source.Document;

        desitnation.Document.Text = source.Text;
        desitnation.TextArea.Caret.Line = source.TextArea.Caret.Line;
        desitnation.TextArea.Caret.Column = source.TextArea.Caret.Column;
        desitnation.TextArea.Caret.Offset = source.TextArea.Caret.Offset;

        desitnation.TextArea.Caret.BringCaretToView();
        //desitnation.ScrollToVerticalOffset(source.VerticalOffset);
        //desitnation.ScrollToHorizontalOffset(source.HorizontalOffset);
        desitnation.SelectionStart = source.SelectionStart;
        desitnation.SelectionLength = source.SelectionLength;

        //desitnation = null;
        //source.TextArea.TextView.VisualLines
        //double vertOffset = (source.TextArea.TextView.DefaultLineHeight) * 10;
        //desitnation.ScrollToVerticalOffset(vertOffset);

    }
}
