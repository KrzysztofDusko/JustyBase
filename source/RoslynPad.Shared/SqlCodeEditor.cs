using System;
using System.Linq;
using JustyBase.Helpers;
using JustyBase.Editor.Folding;
using System.Threading.Tasks;
using JustyBase.Editor.CompletionProviders;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommons;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;


namespace JustyBase.Editor;
public sealed class SqlCodeEditor : CodeTextEditor
{
    private BraceMatcherHighlightRenderer? _braceMatcherHighlighter;
    private TextMarkerService _textMarkerService;
    
    public SqlCodeEditor()
    {

        this.TextArea.Caret.PositionChanged += CaretOnPositionChanged;
#if AVALONIA
        this.AddHandler(PointerWheelChangedEvent, (o, e) =>
        {
            if (e.KeyModifiers == KeyModifiers.Control)
            {
                if (e.Delta.Y > 0 && FontSize < 60)
                {
                    FontSize += 1;
                }
                else if (FontSize > 3)
                {
                    FontSize -= 1;
                }
            }
        }, RoutingStrategies.Bubble, true);
#else
        this.PreviewMouseWheel += (o, e) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0 && FontSize < 60)
                {
                    FontSize += 1;
                }
                else if (FontSize > 3)
                {
                    FontSize -= 1;
                }
                e.Handled = true;
            }
        };
#endif
#if AVALONIA
        SetupCommandBindings();
#endif
    }
#if AVALONIA
    private void SetupCommandBindings()
    {
        // 
        var handler = (TextAreaDefaultInputHandler)TextArea.ActiveInputHandler;
        handler.Detach();
        //TODO selection up/down
        var lineUp = new RoutedCommand("LineUp", new KeyGesture(Key.Up, KeyModifiers.Control));
        var lineDown = new RoutedCommand("LineDown", new KeyGesture(Key.Down, KeyModifiers.Control));

        handler.CommandBindings.Add(new RoutedCommandBinding(lineUp, (o, e) =>
        {
            var currentLine = this.TextArea.Caret.Line;
            if (currentLine > 1)
            {
                DocumentLine line0 = this.Document.Lines[currentLine - 1];
                var line1 = this.Document.Lines[currentLine - 2];
                EditorHelpers.SwapLines(this, line0, line1);
            }
        }));

        handler.CommandBindings.Add(new RoutedCommandBinding(lineDown, (o, e) =>
        {
            var currentLine = this.TextArea.Caret.Line;
            if (currentLine < this.LineCount - 1)
            {
                var line0 = this.Document.Lines[currentLine - 1];
                var line1 = this.Document.Lines[currentLine + 1];
                EditorHelpers.SwapLines(this, line0, line1);
            }
        }));

        //var duplicateLine = new RoutedCommand("DuplicateLine", new KeyGesture(Key.D, KeyModifiers.Control | KeyModifiers.Shift));
        //var goToLine = new RoutedCommand("GoToLine", new KeyGesture(Key.G, KeyModifiers.Control));

        //handler.CommandBindings.Add(new RoutedCommandBinding(duplicateLine, (o, e) =>
        //{
        //    EditorHelpers.DoubleSelectedLine(this);
        //}));
        //handler.CommandBindings.Add(new RoutedCommandBinding(goToLine, async (o, e) =>
        //{
        //    if (GoToLineAsyncAction is null)
        //    {
        //        return;
        //    }
        //    int res = await GoToLineAsyncAction();
        //    if (res > 0 && TextArea is not null)
        //    {
        //        TextArea.Caret.Line = res;
        //        TextArea.Caret.BringCaretToView();
        //    }
        //}));

        handler.Attach();
    }
#endif
    protected override async void OnKeyDown(KeyEventArgs e)
    {
        _foldingTimer?.Stop();
        _foldingTimer?.Start();

        base.OnKeyDown(e);
        // Key.Oem7 = " or '
        if (TextArea.IsFocused && e.Key == Key.Oem7 && TextArea.Selection.Length < 1024)
        {
            int selectionLength = TextArea.Selection.Length;
            if (selectionLength == 0)
            {
                if (e.HasModifiers(ModifierKeys.Shift))
                {
                    TextArea.Document.Insert(CaretOffset, "\"");
                }
                else
                {
                    TextArea.Document.Insert(CaretOffset, "'");
                }
                TextArea.Caret.Offset--;
            }
            else
            {
                if (e.HasModifiers(ModifierKeys.Shift))
                {
                    TextArea.Selection.ReplaceSelectionWithText($"\"{TextArea.Selection.GetText()}");
                }
                else
                {
                    TextArea.Selection.ReplaceSelectionWithText($"'{TextArea.Selection.GetText()}");
                }
            }
        }
        // D9 = "("
        else if (TextArea.IsFocused && e.Key == Key.D9 && e.HasModifiers(ModifierKeys.Shift) && TextArea.Selection.Length < 1024)
        {
            var selection = TextArea.Selection;
            int sellen = selection.Length;
            if (sellen > 0 || selection is not RectangleSelection)
            {
                if (sellen == 0 && TextArea.Caret.Offset > 0 && selection is not RectangleSelection)
                {
                    selection.ReplaceSelectionWithText($"()");
                    //textArea.Document.Insert(CaretOffset, ")");
                    TextArea.Caret.Offset--;
                }
                else if (sellen > 0 && selection is RectangleSelection)
                {
                    selection.ReplaceSelectionWithText($"({selection.GetText().Replace("\r\n", ")\r\n(")})");
                    //_removeLastChar = true;
                }
                else if (selection is not RectangleSelection)
                {
                    selection.ReplaceSelectionWithText($"({selection.GetText()})");
                    //_removeLastChar = true;
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Space && e.HasModifiers(ModifierKeys.Control))
        {
            e.Handled = true;
            var mode = e.HasModifiers(ModifierKeys.Shift)
                ? TriggerMode.SignatureHelp
                : TriggerMode.Completion;
            _ = ShowCompletion(mode);
        }
        else if (e.Key == Key.Space && e.HasModifiers(ModifierKeys.None))
        {
            _completionWindow?.Close();
            if (TextArea is not null)
            {
                ImmediateReplaceQuickOrTypo(e);
                //e.Handled = true;
            }
        }
        else if (e.Key == Key.C && e.HasModifiers(ModifierKeys.Alt)/*&& _completionWindow?.IsOpen() != true*/)
        {
            if (_completionWindow?.IsOpen() == true)
            {
                _completionWindow.Close();
            }
            if (TextArea is not null)
            {
                ImmediateReplaceStandard();
                //e.Handled = true;
            }
            //e.Handled = true;
        }
        else if (e.HasModifiers(ModifierKeys.Control))
        {
            switch (e.Key)
            {
                case Key.H:
                    if (ForcedContolhtAction is not null)
                    {
                        ForcedContolhtAction.Invoke();
                        e.Handled = true;
                    }
                    break;
                case Key.F:
                    if (ForcedContolftAction is not null)
                    {
                        ForcedContolftAction.Invoke();
                        e.Handled = true;
                    }
                    break;
                case Key.D:
                    if (e.HasModifiers(ModifierKeys.Shift))
                    {
                        EditorHelpers.DoubleSelectedLine(this);
                    }
                    break;
                case Key.G:
                    int res = await GoToLineAsyncAction();
                    if (res > 0 && TextArea is not null)
                    {
                        TextArea.Caret.Line = res;
                        TextArea.Caret.BringCaretToView();
                    }
                    break;
                case Key.E:
                    ExpandFoldings();
                    break;
                case Key.R:
                    CollapseFoldings();
                    break;
                case Key.U:
                    if (e.HasModifiers(ModifierKeys.Shift))
                    {
                        SelectedText = SelectedText.ToLower();
                    }
                    else
                    {
                        SelectedText = SelectedText.ToUpper();
                        //    var HighlightDefinition = this.SyntaxHighlighting;
                        //    var Highlighter = new AvaloniaEdit.Highlighting.DocumentHighlighter(Document, HighlightDefinition);

                        //    AvaloniaEdit.Highlighting.HighlightedLine result = Highlighter.HighlightLine(0);

                        //    int off = 0;
                        //    bool isInComment = result.Sections.Any(
                        //s => s.Offset <= off && s.Offset + s.Length >= off
                        //     && s.Color.Name == "Comment");
                        //http://avalonedit.net/documentation/html/4d4ceb51-154d-43f0-b876-ad9640c5d2d8.htm ?
                        //var documentHighlighter = new AvaloniaEdit.Highlighting.DocumentHighlighter(this.Document, cachedHighlightingDefinition);
                        //var colorizer = TextArea.TextView.LineTransformers[0];
                        //var sp = documentHighlighter.GetSpanStack(5);
                        //bool isInComment = result.Sections.Any(
                        //    s => s.Offset <= off && s.Offset + s.Length >= off
                        //         && s.Color.Name == "Comment");
                    }
                    break;
                case Key.J:
                    if (e.HasModifiers(ModifierKeys.Shift))
                    {
                        SelectedText = SelectedText.ChangeCaseRespectingSqlRules(false);
                    }
                    else
                    {
                        SelectedText = SelectedText.ChangeCaseRespectingSqlRules(true);
                    }
                    break;
                case Key.V:
                    if (e.HasModifiers(ModifierKeys.Shift) && ContolShiftvAction is not null)
                    {
                        await ContolShiftvAction.Invoke();
                    }
                    break;
            }
        }

        ///replace if typo or "quick snippet"
        void ImmediateReplaceQuickOrTypo(KeyEventArgs e)
        {
            int offset = TextArea.Caret.Offset;
            Span<char> chars = stackalloc char[LastWordLenLimit];
            int lastWordLength = EditorHelpers.GetLastWord(TextArea, chars);

            if (lastWordLength <= 8 && _someEditorOptions?.FastReplaceDictionary is not null)
            {
                string tmp = chars[..lastWordLength].ToString();
                if (_someEditorOptions.FastReplaceDictionary.TryGetValue(tmp, out var res))
                {
                    int ind = res.IndexOf("${Caret}");
                    if (ind > 0)
                    {
                        ind = res.Length - ind - "${Caret}".Length;
                        res = res.Replace("${Caret}", "");
                    }
                    TextArea.Document.Replace(offset - lastWordLength, lastWordLength, res);
                    if (ind > 0 && ind < TextArea.Caret.Offset)
                    {
                        TextArea.Caret.Offset -= ind;
                        e.Handled = true;
                    }
                }
            }
            if (lastWordLength >= 3 && lastWordLength < LastWordLenLimit && _someEditorOptions is not null)
            {
                var typoCandidate = chars[..lastWordLength];
                foreach (var correctWord in _someEditorOptions.TypoPatternList)
                {
                    int dist = typoCandidate.DamerauLevenshteinDistance(correctWord);
                    if (dist <= SqlCodeEditorHelpers.TypoLimit && dist >= 1)
                    {
                        TextArea.Document.Replace(offset - lastWordLength, lastWordLength, correctWord);
                    }
                }
            }
        }

        void ImmediateReplaceStandard()
        {
            int offset = TextArea.Caret.Offset;
            Span<char> chars = stackalloc char[LastWordLenLimit + 1];
            int lastWordLength = EditorHelpers.GetLastWord(TextArea, chars[1..]);

            if (lastWordLength > 0 && lastWordLength < LastWordLenLimit - 1)
            {
                chars[0] = '@';
                if (_someEditorOptions.GetAllSnippets.TryGetValue(new string(chars[..(lastWordLength + 1)]), out var res))
                {
                    TextArea.Document.Replace(offset - lastWordLength, lastWordLength, res.Text);
                }
            }
        }
    }
    private const int LastWordLenLimit = 32;

    private string LanguageFileExtension => this.Document.FileName is not null ? System.IO.Path.GetExtension(this.Document.FileName).ToLower() : "";

    private ISomeEditorOptions _someEditorOptions ;
    public void Initialize(ISqlAutocompleteData sqlAutocompleteData, ISomeEditorOptions someEditorOptions)
    {

        _someEditorOptions = someEditorOptions;
        _braceMatcherHighlighter = new BraceMatcherHighlightRenderer(TextArea.TextView);
        AsyncToolTipRequest = OnAsyncToolTipRequest;
        var completionProvider = new SqlCompletionProvider(this, sqlAutocompleteData, _someEditorOptions);
        CompletionProvider = completionProvider;
        _textMarkerService = new TextMarkerService(this);
        TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
        TextArea.TextView.LineTransformers.Add(_textMarkerService);
        var truncateLongLines = new TruncateLongLines();
        TextArea.TextView.ElementGenerators.Insert(0, truncateLongLines);
        //TextArea.TextView.ElementGenerators.Add(truncateLongLines);

        this.TextArea.SelectionChanged += TextArea_SelectionChanged;
        if (_someEditorOptions.CollapseFoldingOnStartup && ForceUpdateFoldings())
        {
            CollapseFoldings();
        }
    }
    private async Task OnAsyncToolTipRequest(ToolTipRequestEventArgs arg)
    {
        await Task.Delay(1);
        if (arg.Position < Document.TextLength - 5 && arg.Position > 5)
        {
            string txt = Document.GetText(arg.Position - 3, 4);
            if (txt == "FROM")
            {
                arg.SetToolTip("DEMO tooltip: " + Document.GetText(arg.Position - 3, 4));
                //arg.SetToolTip(new JustyBase.Views.ToolTipViews.ToolTipTable());
            }
        }
    }

    public bool SelectError(int startOffset, int length)
    {
        var marker = _textMarkerService.TryCreate(startOffset, length);
        if (marker is not null)
        {
            marker.MarkerColor = Colors.Red;
            marker.ToolTip = "Error (test): info about.. ";
            return true;
        }
        return false;
    }
    public bool SelectWarning(int startOffset, int length)
    {
        var marker = _textMarkerService.TryCreate(startOffset, length);
        if (marker is not null)
        {
            marker.MarkerColor = Colors.DarkOrange;
            marker.ToolTip = "Error/Warning (test): info about.. ";
            return true;
        }
        return false;
    }    
    public void RemoveAllErrorsWarnings()
    {
        _textMarkerService.RemoveAll(marker => true);
    }

    private void CaretOnPositionChanged(object? sender, EventArgs eventArgs)
    {
        SqlCodeEditorHelpers.LastFocusedEditor = this;
        if (_braceMatcherHighlighter is null || this.Document is null)
        {
            return;
        }

        int position = this.TextArea.Caret.Offset;
        if (position == 0)
        {
            return;
        }
        (int left, int right) = FindBrackets(position);

        if (left != -1 && right != -1)
        {
            _braceMatcherHighlighter.SetHighlight(new BraceMatchingResult(left, right), null);
        }
        else
        {
            _braceMatcherHighlighter.SetHighlight(null, null);
        }
    }

    private (int, int) FindBrackets(int caretOffset)
    {
        if (CleanSqlCode.Length != Document.TextLength)
        {
            CleanSqlCreator();
        }
        int left = FindLeftBracket(caretOffset);
        int right = FindRightBracket(caretOffset);

        if (left == -1 || right == -1)
        {
            return (-1, -1);
        }

        return (left, right);
    }

    private const int LEFT_BRACKET_BUFFER_LEN = 256;
    private int FindLeftBracket(int caretOffset)
    {
        int counter = 0;
        int maxIterations = SqlCodeEditorHelpers.BRACKET_SEARCH_LEN;

        if (caretOffset == 0)
        {
            return -1;
        }

        char c = default;

        do
        {
            ReadOnlySpan<char> spn2;
            int start = 0;
            if (caretOffset >= LEFT_BRACKET_BUFFER_LEN)
            {
                start = caretOffset - (LEFT_BRACKET_BUFFER_LEN - 1);
            }
            else
            {
                start = 0;
            }
            spn2 = CleanSqlCode.AsSpan(start, caretOffset - start);

            int index = spn2.LastIndexOfAny(SqlCodeEditorHelpers.leftBracket, SqlCodeEditorHelpers.rightBracket, ';');
            if (index < 0 && start == 0)
            {
                break;
            }
            else if (index < 0)
            {
                caretOffset -= (LEFT_BRACKET_BUFFER_LEN - 1);
                continue;
            }
            caretOffset = index + start;

            c = spn2[index];
            if (c == ';')
            {
                return -1;
            }

            if (c == SqlCodeEditorHelpers.leftBracket) counter++;
            if (c == SqlCodeEditorHelpers.rightBracket) counter--;
            if (counter == 1)
            {
                //found
                break;
            }
            //
            maxIterations--;
            if (maxIterations <= 0) break;
        } while (caretOffset > 1);

        if (c != SqlCodeEditorHelpers.leftBracket)
        {
            return -1;
        }

        return caretOffset;
    }
    private int FindRightBracket(int caretOffset)
    {
        int counter = 0;
        int maxIterations = SqlCodeEditorHelpers.BRACKET_SEARCH_LEN;
        //string characters = null;
        int docLen = Document.TextLength;

        if (caretOffset == docLen)
        {
            return -1;
        }
        --caretOffset;

        char c = default;
        do
        {
            var spn = CleanSqlCode.AsSpan(++caretOffset);
            int index = spn.IndexOfAny(SqlCodeEditorHelpers.leftBracket, SqlCodeEditorHelpers.rightBracket, ';');
            if (index < 0)
                break;
            maxIterations -= index;
            caretOffset += index;
            c = spn[index];

            if (c == ';')
            {
                return -1;
            }

            if (c == SqlCodeEditorHelpers.leftBracket) counter++;
            if (c == SqlCodeEditorHelpers.rightBracket) counter--;
            if (counter == -1)
            {
                //found
                break;
            }
            //

            if (maxIterations <= 0) break;
        } while (caretOffset < docLen - 1);

        if (c != SqlCodeEditorHelpers.rightBracket)
        {
            return -1;
        }

        return caretOffset;
    }

    public void ExpandFoldings()
    {
        if (_foldingManager is not null)
        {
            foreach (var fold in _foldingManager.AllFoldings)
            {
                fold.IsFolded = false;
            }
        }
    }
    public void CollapseFoldings()
    {
        if (_foldingManager is not null)
        {
            foreach (var fold in _foldingManager.AllFoldings)
            {
                fold.IsFolded = true;
            }
        }
    }
    public bool ForceUpdateFoldings()
    {
        if (_xmlFoldingStrategy is not null)
        {
            _xmlFoldingStrategy.UpdateFoldings(_foldingManager, Document);
            return true;
        }
        else if (_foldingStrategy is not null)
        {
            _foldingStrategy.UpdateFoldings(_foldingManager, Document);
            return true;
        }
        return false;
    }

    private FoldingManager _foldingManager;
    private SqlFoldingStrategy _foldingStrategy;
    private XmlFoldingStrategy _xmlFoldingStrategy;
    private DispatcherTimer _foldingTimer;
    public void FoldingSetup()
    {
        if (_foldingManager != null)
        {
            _foldingManager.Clear();
            FoldingManager.Uninstall(_foldingManager);
        }
        _foldingManager = FoldingManager.Install(TextArea);

        if (ISomeEditorOptions.REGISTERED_EXTENSIONS.TryGetValue(LanguageFileExtension, out var res) && res.isXml)
        {
            _xmlFoldingStrategy = new XmlFoldingStrategy();
            _xmlFoldingStrategy.UpdateFoldings(_foldingManager, Document);
            CollapseFoldings();

            _foldingTimer = new();
            _foldingTimer.Tick += new EventHandler((s, e) =>
            {
                _foldingTimer.Stop();
                _xmlFoldingStrategy?.UpdateFoldings(_foldingManager, Document);
            });
            _foldingTimer.Interval = TimeSpan.FromSeconds(0.5);
        }
        else if (this.SyntaxHighlighting?.Name == "GeneralSql")
        {
            _foldingStrategy = new SqlFoldingStrategy();
            _foldingStrategy.UpdateFoldings(_foldingManager, Document);
            CollapseFoldings();

            _foldingTimer = new();
            _foldingTimer.Tick += new EventHandler((s, e) =>
            {
                _foldingTimer.Stop();
                _foldingStrategy?.UpdateFoldings(_foldingManager, Document);
            });
            _foldingTimer.Interval = TimeSpan.FromSeconds(0.5);
        }
    }

    private void TextArea_SelectionChanged(object? sender, EventArgs e)
    {
        foreach (var markSameWord in this.TextArea.TextView.LineTransformers.OfType<MarkSameWord>().ToList())
        {
            this.TextArea.TextView.LineTransformers.Remove(markSameWord);
        }

        if (!string.IsNullOrWhiteSpace(this.SelectedText) && this.SelectedText.Length < 512)
        {
            this.TextArea.TextView.LineTransformers.Add(new MarkSameWord(this.SelectedText));
        }
    }

    //public Func<Task> ControlHaction;
    public Func<Task<int>> GoToLineAsyncAction;

    public Func<Task> ContolShiftvAction;

    public Action ForcedContolftAction;
    public Action ForcedContolhtAction;

}

