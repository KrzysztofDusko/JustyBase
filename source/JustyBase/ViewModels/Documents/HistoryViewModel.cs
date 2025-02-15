using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Models;
using JustyBase.Common.Services;
using JustyBase.PluginCommon.Contracts;
using System.ComponentModel;

namespace JustyBase.ViewModels.Documents;

public sealed partial class HistoryViewModel : DocumentBaseVM
{
    public string SearchTxt
    {
        get;
        set
        {
            SetProperty(ref field, value);
            HistoryItems.Refresh();
        }
    }
    public ICommand RefreshCmd { get; set; }
    public DataGridCollectionView HistoryItems { get; set; }
    private HistoryViewModel() { }

    private readonly IClipboardService _clipboardService;
    public IClipboardService Clipboard => _clipboardService;
    public HistoryViewModel(HistoryService historyService, IClipboardService clipboardService)
    {
        _historyService = historyService;
        _clipboardService = clipboardService;
        Title = "History";

        HistoryItems = new DataGridCollectionView(HistoryItemsCollection)
        {
            GroupDescriptions =
            {
                    new DataGridPathGroupDescription(nameof(HistoryEntry.Connection))
            },
            Filter = FilterRecords
        };

        var dataGridSortDescription = DataGridSortDescription.FromPath(nameof(HistoryEntry.Date), ListSortDirection.Descending/*, new DateComparer()*/);
        HistoryItems.SortDescriptions.Add(dataGridSortDescription);

        SearchTxt = "";
        Doc = new TextDocument();
        RefreshCmd = new RelayCommand(() => HistoryItems.Refresh());
    }

}
