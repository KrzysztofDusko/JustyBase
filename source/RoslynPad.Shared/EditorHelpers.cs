using System;
using System.Collections.Generic;
using JustyBase.Editor;
using System.Text.RegularExpressions;
using System.Linq;
using JustyBase.PluginCommons;

namespace JustyBase.Helpers;

public static partial class EditorHelpers
{
    /// <summary>
    /// add sql comment for selected lines
    /// </summary>
    /// <param name="editor"></param>
    public static void CommentSelectedLines(this TextEditor editor)
    {
        editor.BeginChange();
        var start = editor.SelectionStart;
        var end = start + editor.SelectionLength;

        char c1 = '\0';
        while (start > 0 && c1 != '\n' && c1 != '\r')
        {
            c1 = editor.Document.GetCharAt(--start);
        }
        if (start > 0)
        {
            start++;
        }

        int len = editor.Document.TextLength;
        bool? doComment = null;
        for (int i = start; i < end; i++)
        {
            var prevChar = i == 0 ? '\n' : editor.Document.GetCharAt(i - 1);
            if ((prevChar == '\n' || prevChar == '\n') && len >= i + 1)
            {
                if (i >= len - 1 || doComment == true || editor.Document.GetCharAt(i) != '-' || editor.Document.GetCharAt(i + 1) != '-')
                {
                    doComment = true;
                    editor.Document.Insert(i, "--");
                    i += 2;
                    end += 2;
                    len += 2;
                }
                else
                {
                    editor.Document.Remove(i, 2);
                    i++;
                    end -= 2;
                    len -= 2;
                }
            }
        }
        editor.EndChange();
    }

    public static void DoubleSelectedLine(TextEditor editor)
    {
        editor.BeginChange();
        var line = editor.Document.Lines[editor.TextArea.Caret.Line - 1];
        string txt = editor.Document.GetText(line.Offset, line.Length);
        editor.Document.Insert(line.EndOffset,Environment.NewLine+txt);
        editor.EndChange();
    }

    public static void SwapLines(TextEditor editor, DocumentLine line0, DocumentLine line1)
    {
        editor.BeginChange();
        string txt = editor.Document.GetText(line0.Offset, line0.Length);
        string ld = "\r\n";
        if (line0.DelimiterLength == 1)
        {
            ld = "\n";
        }
        editor.Document.Insert(line1.Offset, txt + ld);
        editor.Document.Remove(line0.Offset, line0.Length + line0.DelimiterLength);
        editor.Select(line1.Offset, txt.Length);
        editor.TextArea.Caret.BringCaretToView();
        editor.EndChange();
    }


    public static void InsertTextToPrevLineAndSelect(this TextEditor editor, string text, bool doSelect = true)
    {
        editor.BeginChange();
        var start = editor.SelectionStart;

        char c1 = '\0';
        while (start > 0 && c1 != '\n' && c1 != '\r')
        {
            c1 = editor.Document.GetCharAt(--start);
        }
        string startWord = "\n--REGION GENERATED_CODE\n";
        editor.Document.Insert(start, $"{startWord}{text}\n--ENDREGION\n");
        if (doSelect)
        {
            editor.Select(start + startWord.Length, text.Length);
        }
        editor.EndChange();
    }

    /// <summary>
    /// fils chars buffer with last word (before current caret position)
    /// </summary>
    /// <param name="textArea"></param>
    /// <param name="chars"></param>
    /// <returns></returns>
    public static int GetLastWord(this TextArea textArea, Span<char> chars, bool strict = false)
    {
        int offset = textArea.Caret.Offset;
        int i = 0;
        for (i = 0; i < chars.Length && i < offset; i++)
        {
            char c = textArea.Document.GetCharAt(offset - 1 - i);
            if (c == ' ' || c == '\n' || c == '\t' || c == '(' || strict && c == '.')
            {
                break;
            }
            chars[i] = c;
        }

        if (i >= 2)
        {
            for (int j = 0; j < i / 2; j++)
            {
                //swap
                var tmp = chars[j];
                chars[j] = chars[i - j - 1];
                chars[i - j - 1] = tmp;
            }
        }

        return i;
    }

    public static string? GetLastWord(this TextEditor textEditor, int position)
    {
        Stack<char> tmpStack = new Stack<char>();
        int l = position - 1;
        char c = textEditor.Document.GetCharAt(l);
        if (l > 0)
        {
            do
            {
                tmpStack.Push(c);
                c = textEditor.Document.GetCharAt(--l);
                if (tmpStack.Count > 128)
                {
                    return null;
                }
            } while (c != ' ' && c != '\r' && c != '\n' && c != '\t' && c != '(' && c != ',' && l > 0);
        }
        else
        {
            tmpStack.Push(c);
        }
        if (l==0 && position > 1 && position < 10)
        {
            tmpStack.Push(textEditor.Document.GetCharAt(0));
        }

        string result = string.Create(tmpStack.Count, tmpStack, (chars, buf) =>
        {
            for (int i = 0; i < chars.Length; i++) chars[i] = buf.Pop();
        });
        return result;
    }

    public static string GetSurrendedText(this TextArea textArea)
    {
        int offset = textArea.Caret.Offset;
        Span<char> chars = stackalloc char[128];

        int num = 0;
        while (offset > 0 && num < 64)
        {
            var c = textArea.Document.GetCharAt(offset - 1);
            if (c == ' ' || c == '\r' || c == '\n' || c == '\t' || c == '(' || c == ',' || c == ';')
            {
                break;
            };
            offset--;
            num++;
        }
        if (num == 64)
        {
            return "";
        }

        int i = 0;
        for (i = 0; i < chars.Length && offset < textArea.Document.TextLength; i++)
        {
            char c = textArea.Document.GetCharAt(offset);
            if (c == ' ' || c == '\r' || c == '\n' || c == '\t' || c == '(' || c == ',' || c == ';')
            {
                break;
            }
            chars[i] = c;
            offset++;
        }

        return chars.Slice(0,i).ToString();
    }

    public static void ReplaceVariable(this TextEditor textEditor)
    {
        var txt = textEditor.SelectedText;

        if (string.IsNullOrWhiteSpace(txt))
        {
            txt = textEditor.TextArea.GetSurrendedText();
        }
        if (string.IsNullOrWhiteSpace(txt))
        {
            return;
        }

        int num = 0;
        string replacement = "";
        if (!txt.StartsWith('$'))
        {
            replacement = $"${txt}";
        }
        else
        {
            replacement = txt[1..];
        }

        textEditor.BeginChange();
        try
        {
            while (num >= 0)
            {
                num = textEditor.Document.IndexOf(txt, num, textEditor.Document.TextLength - num, StringComparison.OrdinalIgnoreCase);
                if (num >= 0)
                {
                    if (num + txt.Length == textEditor.Document.TextLength)
                    {
                        textEditor.Document.Replace(num, txt.Length, replacement);
                        break;
                    }

                    char c1 = ' ';
                    if (num + txt.Length < textEditor.Document.TextLength)
                    {
                        c1 = textEditor.Document.GetCharAt(num + txt.Length);
                    }
                    char c2 = ' ';
                    if (num > 0)
                    {
                        c2 = textEditor.Document.GetCharAt(num - 1);
                    }
                    if (!Char.IsAsciiLetterOrDigit(c1) && c1 != '_' && !Char.IsAsciiLetterOrDigit(c2) && c2 != '_')
                    {
                        textEditor.Document.Replace(num, txt.Length, replacement);
                    }
                }
                else if (num == -1)
                {
                    break;
                }
                num += 2; // txt.StartsWith('$') ??
            }
        }
        catch (Exception)
        {
            //skip
        }

        textEditor.EndChange();
    }

    public static string GetTappedWord(this TextEditor textEditor)
    {
        string tappedWord;
        int selLen = textEditor.SelectionLength;
        if (selLen > 0 && selLen < 64)
        {
            tappedWord = textEditor.SelectedText;
        }
        else
        {
            tappedWord = textEditor.TextArea.GetSurrendedText();
        }
        tappedWord = tappedWord.Trim();
        return tappedWord;
    }

    public static string SelectQueryPhase(this TextEditor textEditor, out int currentSqlPosiotionInEditor)
    {
        string query = "";
        currentSqlPosiotionInEditor = textEditor.SelectionStart;
        if (textEditor.SelectionLength > 0)
        {
            query = textEditor.SelectedText;
        }
        else
        {
            var parts = textEditor.Text.MySplitForSqlSplit(';');
            int offset = textEditor.CaretOffset;
            int s = 0;
            int i = 0;
            for (; i < parts.Count; i++)
            {
                s += parts[i].Length;
                s++;
                if (s > offset)
                {
                    query = parts[i];
                    break;
                }
            }
            textEditor.Select(s - 1 - query.Length, query.Length);
            currentSqlPosiotionInEditor = s - 1 - query.Length;
        }
        return query;
    }


    /// <summary>
    /// common errors, TODO
    /// </summary>
    /// <param name="editor"></param>
    /// <returns></returns>
    public static bool ErrorWaningsPahse1(this SqlCodeEditor sqlCodeEditor)
    {
        if (sqlCodeEditor is null)
        {
            return false;
        }
        sqlCodeEditor.RemoveAllErrorsWarnings();
        bool matched = false;
        foreach (Match item in DuplicateKeyWordRegex().Matches(sqlCodeEditor.Text).Cast<Match>())
        {
            sqlCodeEditor.SelectError(item.Index, 4);
            matched = true;
        }
        return matched;
    }

    [GeneratedRegex("(FROM FROM)|(SELECT SELECT)|(WHERE WHERE)|(,,)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DuplicateKeyWordRegex();


}

