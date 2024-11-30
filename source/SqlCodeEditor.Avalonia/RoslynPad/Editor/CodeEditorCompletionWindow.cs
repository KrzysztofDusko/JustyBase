using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.Editor;

partial class CodeEditorCompletionWindow
{
    partial void Initialize()
    {

        this.Opened += CustomCompletionWindow_Opened;
        if (CompletionList.ListBox is not null)
        {
            CompletionList.ListBox.BorderThickness = new Thickness(1);
            CompletionList.ListBox.PointerPressed += (o, e) => _isSoftSelectionActive = false;
        }
    }
    private async void CustomCompletionWindow_Opened(object? sender, EventArgs e)
    {
        await Task.Delay(10);
        CompletionList.SelectedItem = CompletionList.CompletionData.FirstOrDefault();
        //Debug.WriteLine("CustomCompletionWindow_Opened");
        //throw new NotImplementedException();
    }
}