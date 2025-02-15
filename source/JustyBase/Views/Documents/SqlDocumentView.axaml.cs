using JustyBase.Common.Contracts;
using JustyBase.Helpers;
using JustyBase.ViewModels;
using JustyBase.ViewModels.Documents;
using JustyBase.Views.ToolTipViews;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JustyBase.Views.Documents;
public partial class SqlDocumentView : UserControl
{
    public SqlDocumentView()
    {
        InitializeComponent();
        SqlEditor.TextArea.RightClickMovesCaret = true;
        SqlEditor.DataContextChanged += TextEditor_DataContextChanged;
        SqlEditor.KeyDown += TextEditor_KeyDownAsync;
        SetupDnd();
    }
    private readonly Flyout _quickMenuFlyout = new()
    {
        Content = new DbObjectQuickMenu(),
        ShowMode = FlyoutShowMode.Standard
    };

    private async void TextEditor_KeyDownAsync(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F4)
        {
            DbObjectQuickMenu quickMenu = _quickMenuFlyout.Content as DbObjectQuickMenu;
            quickMenu.DataContext = new DbObjectQuickMenuViewModel()
            {
                ObjectTitle = this.SqlEditor.GetTappedWord(),
                SqlDocVM = ViewModel,
                CloseAction = _quickMenuFlyout.Hide
            };
            _quickMenuFlyout.ShowAt(this, true);
        }
        else if (e.Key is Key.RightCtrl or Key.F6)
        {
            await ViewModel.JumpToSelectedItem();
        }
        else if (e.Key == Key.F7)
        {
            await ViewModel.SelectSelectedItem();
        }
    }

    private SqlDocumentViewModel ViewModel => this.DataContext as SqlDocumentViewModel;

    private void SetupDnd()
    {
        void DragOver(object sender, DragEventArgs e)
        {
            //files = special care
            if (e.Data.Contains(DataFormats.Files) || e.Data.Contains("FileContents"))
            {
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
                return;
            }
            return;
        }

        async void Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
            {
                //SqlEditor.AppendText(string.Join(Environment.NewLine, e.Data.GetFileNames()));
                if (ViewModel is not null)
                {
                    var filenameX = e.Data.GetFiles();
                    if (filenameX is not null)
                    {
                        List<string> filenamesToOpen = filenameX.Select(o => o.Path.LocalPath)
                            .Where(o => IGeneralApplicationData.REGISTERED_EXTENSIONS.ContainsKey(Path.GetExtension(o)))
                            .ToList();
                        ViewModel.ActualDockFactory.AddNewDocumentFromFile(filenamesToOpen);

                        foreach (var item in filenameX.Select(o => o.Path.LocalPath).Where(p => !IGeneralApplicationData.REGISTERED_EXTENSIONS.ContainsKey(Path.GetExtension(p))))
                        {
                            if (ViewModel is null)
                            {
                                return;
                            }
                            int index = ViewModel.SelectedConnectionIndex;
                            var doc = ViewModel.ActualDockFactory.AddNewDocument("IMPORT IN PROGRESS");
                            doc.SelectedConnectionIndex = index;
                            await doc.ImportFromFilePath(item);
                        }
                    }
                }
            }
            else if (e.Data.Contains("FileContents"))
            {
                try
                {
                    var fileContents = e.Data.Get("FileContents");
                    //var t2 = e.Data.Get("FileGroupDescriptor");
                    //var t3 = e.Data.Get("FileGroupDescriptorW");
                    //var t4 = e.Data.Get("Text");
                    //var formats = e.Data.GetDataFormats();
                    if (fileContents is MemoryStream memoryStream)
                    {
                        var streamreader = new StreamReader(memoryStream);
                        ViewModel.ActualDockFactory.AddNewDocument(streamreader.ReadToEnd());
                        streamreader.Close();
                        memoryStream.Close();
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private bool _initialized = false;
    private void TextEditor_DataContextChanged(object sender, EventArgs e)
    {
        if (!_initialized && ViewModel is not null)
        {
            _initialized = true;
            if (ViewModel.SqlEditor is null)
            {
                ViewModel.SqlEditor = SqlEditor;
            }

            var currentOptions = new MenuItem() { Header = "Current options", IsEnabled = true };
            foreach (var item in ViewModel.CurrentOptionsList)
            {
                currentOptions.Items.Add(new MenuItem() { Header = item.OptionHeader, Command = item.OptionCommand, CommandParameter = item.OptionHeader });
            }
            rightMenu.Items.Insert(0, currentOptions);

            //ViewModel.ResetFontStyle = () =>
            //{
            //    App.GetRequiredService<IMessageForUserTools>().DispatcherActionInstance(()=> this.ResetFontInView());
            //};
        }
    }
}

//private int GetOffsetFromMousePosition(Point positionRelativeToTextView, out int visualColumn, out bool isAtEndOfLine)
//{
//    visualColumn = 0;
//    var textView = SqlEditor.TextArea.TextView;
//    var pos = positionRelativeToTextView;
//    if (pos.Y < 0)
//        pos = pos.WithY(0);
//    if (pos.Y > textView.Bounds.Height)
//        pos = pos.WithY(textView.Bounds.Height);
//    pos += textView.ScrollOffset;
//    if (pos.Y >= textView.DocumentHeight)
//        pos = pos.WithY(textView.DocumentHeight - ExtensionMethods.Epsilon);
//    var line = textView.GetVisualLineFromVisualTop(pos.Y);
//    if (line != null && line.TextLines != null)
//    {
//        isAtEndOfLine = false;
//        visualColumn = line.GetVisualColumn(pos, false);
//        return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
//    }
//    isAtEndOfLine = false;
//    return -1;
//}


//string fileExtension = "none";
//if (ViewModel.FilePath is null && !ViewModel.TxtPreview)
//{
//    fileExtension = ".sql";
//}

//if (ViewModel.FilePath is not null)
//{
//    fileExtension = Path.GetExtension(ViewModel.FilePath);
//}
//if (ViewModel.SqlEditor is null)
//{
//    ViewModel.SqlEditor = SqlEditor;
//}
//if (ViewModel.SqlEditor is null)
//{
//    ViewModel.SqlEditor = SqlEditor;
//    if (_generalApplicationData.TryGetDocumentById(ViewModel.Id, out var offlineTabData) && offlineTabData.SqlText is not null)
//    {
//        SqlEditor.Document.Text = offlineTabData.SqlText;
//        //SqlEditor.AppendText(offlineTabData.SqlText);
//    }
//}
//else // failed recycle mode, ???????
//{
//    Debug.Assert(false);
//    Debug.Assert(false);
//    ViewModel.SqlEditor = SqlEditor;
//    SqlEditor.Initialize(ViewModel, _generalApplicationData);
//    SqlEditor.TextArea.GotFocus += (_, _) => SqlCodeEditorHelpers.LastFocusedEditor = SqlEditor;
//    SqlEditor.GotFocus += (_, _) => SqlCodeEditorHelpers.LastFocusedEditor = SqlEditor;
//    SqlEditor.ForceUpdateFoldings();
//    SqlEditor.CollapseFoldings();
//}