using JustyBase.Common.Models;
using JustyBase.ViewModels.Documents;
using System.Text;

namespace JustyBase.Views.Documents;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
        hisotryGrid.KeyDown += HisotryGrid_KeyDown;
        textEditor.SyntaxHighlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance.GetDefinition("SQL");
    }
    private HistoryViewModel ViewModel => this.DataContext as HistoryViewModel;
    private async void HisotryGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
        {
            var SelectedItems = hisotryGrid.SelectedItems;
            StringBuilder sb = new();

            for (int i = 0; i < hisotryGrid.Columns.Count; i++)
            {
                sb.Append(hisotryGrid.Columns[i].Header);
                if (i < hisotryGrid.Columns.Count - 1)
                {
                    sb.Append('\t');
                }
            }
            if (SelectedItems.Count > 0)
            {
                sb.AppendLine();
                for (int index = 0; index < SelectedItems.Count; index++)
                {
                    if (SelectedItems[index] is HistoryEntry historyEntry)
                    {
                        sb.Append(historyEntry.Date.ToString());
                        sb.Append('\t');
                        sb.Append(historyEntry.Connection);
                        sb.Append('\t');
                        sb.Append(historyEntry.Database);
                        sb.Append('\t');
                        sb.AppendLine(historyEntry.SQL);
                    }
                }
            }
            await (ViewModel?.Clipboard)?.SetTextAsync(sb.ToString());
            e.Handled = true;
        }
    }
}
