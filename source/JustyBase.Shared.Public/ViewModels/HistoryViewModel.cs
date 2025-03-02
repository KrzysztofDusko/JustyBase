using CommunityToolkit.Mvvm.ComponentModel;
using JustyBase.Common.Models;
using JustyBase.Common.Services;
using System.Collections.Generic;

namespace JustyBase.ViewModels.Documents;

public sealed partial class HistoryViewModel
{
    private readonly HistoryService _historyService;

    [ObservableProperty]
    public partial TextDocument Doc { get; set; } = new TextDocument();

    public HistoryEntry SelectedItem
    {
        get;
        set
        {
            SetProperty(ref field, value);
            Doc.Text = field?.SQL ?? "";
            OnPropertyChanged(nameof(Doc));
        }
    }

    public List<HistoryEntry> HistoryItemsCollection => _historyService.HistoryItemsCollection;
    public bool FilterRecords(object o)
    {
        if (o is HistoryEntry historyEntry)
        {
            return historyEntry.FiltrerRow(SearchTxt);
        }
        return false;
    }
}


