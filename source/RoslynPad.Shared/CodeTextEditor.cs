using JustyBase.Editor.CompletionProviders;
using JustyBase.PluginCommons;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JustyBase.Editor;

public partial class CodeTextEditor : TextEditor
{
    protected CompletionWindow? _completionWindow;
    private OverloadInsightWindow? _insightWindow;
    private ToolTip? _toolTip;

    public CodeTextEditor()
    {
        ShowLineNumbers = true;

        Options = new TextEditorOptions
        {
            ConvertTabsToSpaces = true,
            AllowScrollBelowDocument = true,
            IndentationSize = 4,
            EnableHyperlinks = false,
            EnableEmailHyperlinks = false
        };

        TextArea.TextView.VisualLinesChanged += OnVisualLinesChanged;
        TextArea.TextEntering += OnTextEntering;
        TextArea.TextEntered += OnTextEntered;

        //TextArea.CommandBindings.Add(new RoutedCommandBinding(new RoutedCommand("XXX", new KeyGesture(Key.J, KeyModifiers.Control)),
        //    (sender, e) => ABC()));

        //var commandBindings = TextArea.CommandBindings;
        //var deleteLineCommand = commandBindings.OfType<CommandBinding>().FirstOrDefault(x =>
        //    x.Command == AvalonEditCommands.DeleteLine);
        //if (deleteLineCommand != null)
        //{
        //    commandBindings.Remove(deleteLineCommand);
        //}
        Initialize();
    }



    protected enum TriggerMode
    {
        Text,
        Completion,
        SignatureHelp
    }

    public static readonly RoutedEvent ToolTipRequestEvent = CommonEvent.Register<CodeTextEditor, ToolTipRequestEventArgs>(
        nameof(ToolTipRequest), RoutingStrategy.Bubble);

    public Func<ToolTipRequestEventArgs, Task>? AsyncToolTipRequest { get; set; }

    public event EventHandler<ToolTipRequestEventArgs> ToolTipRequest
    {
        add => AddHandler(ToolTipRequestEvent, value);
        remove => RemoveHandler(ToolTipRequestEvent, value);
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e)
    {
        _toolTip?.Close(this);
    }

#if !AVALONIA
    private void OnMouseHoverStopped(object? sender, MouseEventArgs e)
    {
        if (_toolTip != null)
        {
            _toolTip.Close(this);
            e.Handled = true;
        }
    }
#endif

#if !AVALONIA
    private async void OnMouseHover(object? sender, MouseEventArgs e)
    {
        TextViewPosition? position;
        try
        {
            position = TextArea.TextView.GetPositionFloor(e.GetPosition(TextArea.TextView) + TextArea.TextView.ScrollOffset);
        }
        catch (ArgumentOutOfRangeException)
        {
            // TODO: check why this happens
            e.Handled = true;
            return;
        }
        var args = new ToolTipRequestEventArgs { InDocument = position.HasValue };
        if (!position.HasValue || position.Value.Location.IsEmpty || position.Value.IsAtEndOfLine)
        {
            return;
        }

        args.LogicalPosition = position.Value.Location;
        args.Position = Document.GetOffset(position.Value.Line, position.Value.Column);

        RaiseEvent(args);

        if (args.ContentToShow == null)
        {
            var asyncRequest = AsyncToolTipRequest?.Invoke(args);
            if (asyncRequest != null)
            {
                await asyncRequest.ConfigureAwait(true);
            }
        }

        if (args.ContentToShow == null)
        {
            return;
        }

        if (_toolTip == null)
        {
            _toolTip = new ToolTip { MaxWidth = 400 };
            InitializeToolTip();
        }

        if (args.ContentToShow is string stringContent)
        {
            _toolTip.SetContent(this, new TextBlock
            {
                Text = stringContent,
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            _toolTip.SetContent(this, new ContentPresenter
            {
                Content = args.ContentToShow,
                MaxWidth = 400
            });
        }

        e.Handled = true;
        _toolTip.Open(this);

        AfterToolTipOpen();
    }
#endif
    partial void InitializeToolTip();
    partial void AfterToolTipOpen();

    partial void Initialize();

    public bool IsCompletionWindowOpen => _completionWindow?.IsVisible == true;

    public void CloseCompletionWindow()
    {
        if (_completionWindow != null)
        {
            _completionWindow.Close();
            _completionWindow = null;
        }
    }

    public bool IsInsightWindowOpen => _insightWindow?.IsVisible == true;

    public void CloseInsightWindow()
    {
        if (_insightWindow != null)
        {
            _insightWindow.Close();
            _insightWindow = null;
        }
    }

    #region Code Completion

    public ICodeEditorCompletionProvider? CompletionProvider { get; set; }
    partial void InitializeInsightWindow();

    protected async Task ShowCompletion(TriggerMode triggerMode)
    {
        if (CompletionProvider == null)
        {
            return;
        }

        GetCompletionDocument(out var offset);
        if (offset == 0)
        {
            return;
        }
        var completionChar = triggerMode == TriggerMode.Text ? Document.GetCharAt(offset - 1) : (char?)null;

        CompletionResult results = await CompletionProvider.GetCompletionData(offset, completionChar).ConfigureAwait(true);

        if (results?.CompletionData?.Count == 0)
        {
            _completionWindow?.Close();
        }

        if (results?.OverloadProvider != null)
        {
            results.OverloadProvider.Refresh();

            if (_insightWindow != null && _insightWindow.IsOpen())
            {
                _insightWindow.Provider = results.OverloadProvider;
            }
            else
            {
                _insightWindow = new OverloadInsightWindow(TextArea)
                {
                    Provider = results.OverloadProvider,
                    //Background = CompletionBackground,
                };

                InitializeInsightWindow();

                _insightWindow.Closed += (o, args) => _insightWindow = null;
                _insightWindow.Show();
            }
            return;
        }

        if (_completionWindow?.IsOpen() != true && results?.CompletionData is not null && results.CompletionData.Any())
        {
            _insightWindow?.Close();

//#if AVALONIA
            _completionWindow = new CompletionWindow(TextArea)
            {
                CloseWhenCaretAtBeginning = triggerMode == TriggerMode.Completion || triggerMode == TriggerMode.Text
            };

            _completionWindow.Initialized += (o, args) => 
            {
                var cw = (o as CompletionWindow);
                if (cw is not null)
                {
                    cw.CompletionList.BorderThickness = new Thickness(1);
                }
            };
            _completionWindow.Loaded += async (o, args) =>
            {
                await Task.Delay(10);
                var cw = (o as CompletionWindow);
                if (cw is not null)
                {
                    cw.CompletionList.SelectedItem = cw.CompletionList.CompletionData.FirstOrDefault();
                }
            };
            InitializeCompletionWindow();

            if (completionChar is not null && IsLetterDigitOrAt(completionChar.Value))
            {
                _completionWindow.CloseWhenCaretAtBeginning = true;
            }

            if (triggerMode == TriggerMode.Completion || completionChar is not null && IsLetterDigitOrAt(completionChar.Value)) // https://github.com/KrzysztofDusko/JustyBase/issues/268
            {
                int maxToGoBack = 32;
                var tmpStartOffset = _completionWindow.StartOffset;
                do
                {
                    maxToGoBack--;
                    tmpStartOffset -= 1;
                } while (maxToGoBack > 0 && tmpStartOffset > 0 && IsLetterDigitOrAt(Document.GetCharAt(tmpStartOffset)));
                if (tmpStartOffset > 0)
                {
                    tmpStartOffset++;
                }
                _completionWindow.StartOffset = tmpStartOffset;
            }

            var data = _completionWindow.CompletionList.CompletionData;
            ICompletionDataEx? selected = null;
            foreach (var completion in results.CompletionData)
            {
                if (completion.IsSelected)
                {
                    selected = completion;
                }

                data.Add(completion);
            }

            try
            {
                _completionWindow.CompletionList.SelectedItem = selected;
            }
            catch (Exception)
            {
                // TODO-AV: Fix this in AvaloniaEdit
            }

            _completionWindow.Closed += (o, args) => _completionWindow = null;
            _completionWindow.Show();
        }
    }

    /// <summary>
    /// Checks if a provided char is a well-known identifier
    /// </summary>
    /// <param name="c">The charcater to check</param>
    /// <returns><c>true</c> if <paramref name="c"/> is a well-known identifier.</returns>
    private static bool IsCharIdentifier(char c)
    {
        return c == '_' || c == '(' || char.IsLetterOrDigit(c);
    }

    private static bool IsLetterDigitOrAt(char c)
    {
        return c == '_' || c == '@' || char.IsLetterOrDigit(c);
    }


    private string _cleanSqlCode = "";
    public string CleanSqlCode => _cleanSqlCode;

    public void CleanSqlCreator()
    {
        _cleanSqlCode = Document.Text.CreateCleanSql();
    }    

    private void OnTextEntering(object? sender, TextCompositionEventArgs args)
    {
        if (this.SyntaxHighlighting?.Name == "GeneralSql" && args.Text?.Length > 0 && _completionWindow != null)
        {
            char c = args.Text[0];
            if (!IsCharIdentifier(c))
            //https://github.com/KrzysztofDusko/JustDataEvoProject/issues/112
            {
                // Whenever no identifier letter is typed while the completion window is open,
                // insert the currently selected element.
                _completionWindow.CompletionList.RequestInsertion(args);
            }
        }
        // Do not set e.Handled=true.
        // We still want to insert the character that was typed.
    }

    private DispatcherTimer? _sqlCompletionTimer = null;

    private void OnTextEntered(object? sender, TextCompositionEventArgs e)
    {
        if (this.SyntaxHighlighting?.Name == "GeneralSql")
        {
            CleanSqlCreator();
        }
        //_ = ShowCompletion(TriggerMode.Text);
        InitCompletitionIfNeeded();
        //if (!_completionInProgress)
        //{
        if (this.SyntaxHighlighting?.Name == "GeneralSql")
        {
            _sqlCompletionTimer?.Stop();
            _sqlCompletionTimer?.Start();
        }
        //}
    }

    private void InitCompletitionIfNeeded()
    {
        if (this.SyntaxHighlighting?.Name == "GeneralSql" && _sqlCompletionTimer is null)
        {
            _sqlCompletionTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _sqlCompletionTimer.Tick += (_, _) =>
            {
                _sqlCompletionTimer.Stop();
                _ = ShowCompletion(TriggerMode.Text);
            };
        }
    }

    /// <summary>
    /// Gets the document used for code completion, can be overridden to provide a custom document
    /// </summary>
    /// <param name="offset"></param>
    /// <returns>The document of this text editor.</returns>
    protected virtual IDocument GetCompletionDocument(out int offset)
    {
        offset = CaretOffset;
        return Document;
    }

    partial void InitializeCompletionWindow();

#endregion


}


