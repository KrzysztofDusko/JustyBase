using JustyBase.PluginCommon.Contracts;
using System;
using System.Collections.Generic;

namespace JustyBase.Editor;

public static class SqlCodeEditorHelpers
{
    public const int BRACKET_SEARCH_LEN = 1024 * 65_536;
    public const char leftBracket = '(';
    public const char rightBracket = ')';
    public const int TypoLimit = 1;
    public static Dictionary<string, Dictionary<string, HighlightingBrush>> SqlColors { get; set; } = new()
    {
        {".sql|Light" ,
            new()
            {
                { "Keywords", new SimpleHighlightingBrush(Colors.Blue) },
                { "Char", new SimpleHighlightingBrush(Colors.Red) },
                { "NumberLiteral", new SimpleHighlightingBrush(Colors.Brown) },

                {"Comment",new SimpleHighlightingBrush(Colors.Green) },
                {"Preprocessor",new SimpleHighlightingBrush(Colors.Green) },

                {"MethodCall",new SimpleHighlightingBrush(Color.FromRgb(250,0,250)) },
                {"ValueTypeKeywords", new SimpleHighlightingBrush(Colors.BlueViolet) },
                //{"Parametr", new SimpleHighlightingBrush(Color.FromRgb(255, 0, 0)) },
                {"TrueFalse", new SimpleHighlightingBrush(Colors.DarkCyan) },
            }
        },
        {".sql|Dark" ,
            new()
            {
                { "Keywords", new SimpleHighlightingBrush(Colors.LightGreen) },
                { "Char", new SimpleHighlightingBrush(Colors.OrangeRed) },
                { "NumberLiteral", new SimpleHighlightingBrush(Colors.Orange) },

                {"Comment",new SimpleHighlightingBrush(Colors.Yellow) },
                {"Preprocessor",new SimpleHighlightingBrush(Colors.Yellow) },

                {"MethodCall",new SimpleHighlightingBrush(Color.FromRgb(250,0,250)) },
                {"ValueTypeKeywords", new SimpleHighlightingBrush(Colors.BlueViolet) },
               // {"Parametr", new SimpleHighlightingBrush(Color.FromRgb(0, 255, 0)) },
                {"TrueFalse", new SimpleHighlightingBrush(Colors.DarkCyan) },
            }
        }
    };
    public static SqlCodeEditor LastFocusedEditor { get; set; }
    public static void ResetStyle(bool dark, string language = ".sql")
    {
        string keyName = $"{language}|{(dark ? "Dark" : "Light")}";

        if (SqlColors.TryGetValue(keyName, out Dictionary<string, HighlightingBrush>? tmpValue))
        {
            foreach (var (key, val) in tmpValue)
            {
                //cachedHighlightingDefinition[language].GetNamedColor(key).Foreground = val;
                var syntax = HighlightingManager.Instance.GetDefinition(ISomeEditorOptions.REGISTERED_EXTENSIONS[language].name);
                syntax.GetNamedColor(key).Foreground = val;
            }
        }
    }

    //private static readonly Dictionary<string, Dictionary<string, HighlightingBrush>> _sqlColors = new()
    //{
    //    {".sql|Light" ,
    //        new()
    //        {
    //            { "Keywords", new SimpleHighlightingBrush(Colors.Blue) },
    //            { "Char", new SimpleHighlightingBrush(Colors.Red) },
    //            { "NumberLiteral", new SimpleHighlightingBrush(Colors.Brown) },

    //            {"Comment",new SimpleHighlightingBrush(Colors.Green) },
    //            {"Preprocessor",new SimpleHighlightingBrush(Colors.Green) },

    //            {"MethodCall",new SimpleHighlightingBrush(Color.FromRgb(250,0,250)) },
    //            {"ValueTypeKeywords", new SimpleHighlightingBrush(Colors.BlueViolet) },
    //            //{"Parametr", new SimpleHighlightingBrush(Color.FromRgb(255, 0, 0)) },
    //            {"TrueFalse", new SimpleHighlightingBrush(Colors.DarkCyan) },
    //        }
    //    },
    //    {".sql|Dark" ,
    //        new()
    //        {
    //            { "Keywords", new SimpleHighlightingBrush(Colors.LightGreen) },
    //            { "Char", new SimpleHighlightingBrush(Colors.OrangeRed) },
    //            { "NumberLiteral", new SimpleHighlightingBrush(Colors.Orange) },

    //            {"Comment",new SimpleHighlightingBrush(Colors.Yellow) },
    //            {"Preprocessor",new SimpleHighlightingBrush(Colors.Yellow) },

    //            {"MethodCall",new SimpleHighlightingBrush(Color.FromRgb(250,0,250)) },
    //            {"ValueTypeKeywords", new SimpleHighlightingBrush(Colors.BlueViolet) },
    //           // {"Parametr", new SimpleHighlightingBrush(Color.FromRgb(0, 255, 0)) },
    //            {"TrueFalse", new SimpleHighlightingBrush(Colors.DarkCyan) },
    //        }
    //    }
    //};
}



//#if AVALONIA_104
//    private ElementGenerator _generator = new ElementGenerator();
//#endif
//#if AVALONIA_104
//        TextArea.TextView.ElementGenerators.Add(_generator);
//        var sp = new StackPanel();
//        sp.Orientation = Avalonia.Layout.Orientation.Horizontal;
//        var te = new TextEditor() { Text = "lineNum", Cursor = Cursor.Default };
//        sp.Children.Add(te);
//        sp.Children.Add(new Button()
//        {
//            Content = "Click me",
//            Cursor = Cursor.Default,
//            Command = new RelayCommand(() =>
//            {
//                TextArea.Caret.Offset = Document.Lines[50].Offset;
//                TextArea.Caret.BringCaretToView();
//            })
//        }) ;

//        _generator.controls.Add(new KeyValuePair<int, Control>(CaretOffset, sp));
//        TextArea.TextView.Redraw();
//#endif

//#if AVALONIA_104
//class ElementGenerator : AvaloniaEdit.Rendering.VisualLineElementGenerator, IComparer<KeyValuePair<int, Control>>
//{
//    public List<KeyValuePair<int, Control>> controls = new List<KeyValuePair<int, Control>>();

//    /// <summary>
//    /// Gets the first interested offset using binary search
//    /// </summary>
//    /// <returns>The first interested offset.</returns>
//    /// <param name="startOffset">Start offset.</param>
//    public override int GetFirstInterestedOffset(int startOffset)
//    {
//        int pos = controls.BinarySearch(new KeyValuePair<int, Control>(startOffset, null), this);
//        if (pos < 0)
//            pos = ~pos;
//        if (pos < controls.Count)
//            return controls[pos].Key;
//        else
//            return -1;
//    }

//    public override AvaloniaEdit.Rendering.VisualLineElement ConstructElement(int offset)
//    {
//        int pos = controls.BinarySearch(new KeyValuePair<int, Control>(offset, null), this);
//        if (pos >= 0)
//            return new AvaloniaEdit.Rendering.InlineObjectElement(0, controls[pos].Value);
//        else
//            return null;
//    }

//    int IComparer<KeyValuePair<int, Control>>.Compare(KeyValuePair<int, Control> x, KeyValuePair<int, Control> y)
//    {
//        return x.Key.CompareTo(y.Key);
//    }
//}
//#endif




//public class ColorizeAvalonEdit : DocumentColorizingTransformer
//{
//    protected override void ColorizeLine(DocumentLine line)
//    {
//        int lineStartOffset = line.Offset;
//        string text = CurrentContext.Document.GetText(line);
//        int start = 0;
//        int index;
//        while ((index = text.IndexOf("AvalonEdit", start)) >= 0)
//        {
//            base.ChangeLinePart(
//                lineStartOffset + index, // startOffset
//                lineStartOffset + index + 10, // endOffset
//                (VisualLineElement element) => {
//                    // This lambda gets called once for every VisualLineElement
//                    // between the specified offsets.
//                    Typeface tf = element.TextRunProperties.Typeface;
//                    // Replace the typeface with a modified version of
//                    // the same typeface
//                    element.TextRunProperties.SetTypeface(new Typeface(
//                        tf.FontFamily,
//                        FontStyles.Italic,
//                        FontWeights.Bold,
//                        tf.Stretch
//                    ));
//                });
//            start = index + 1; // search for next occurrence
//        }
//    }
//}

//public static void FillBuffer(Rope<char> rope, int startIndex, int length, char[] buffer)
//{
//    rope.CopyTo(startIndex, buffer, 0, length);
//}
//internal static object GetInstanceField(Type type, object instance, string fieldName)
//{
//    BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.NonPublic;
//    FieldInfo field = type.GetField(fieldName, bindFlags);
//    return field.GetValue(instance);
//}