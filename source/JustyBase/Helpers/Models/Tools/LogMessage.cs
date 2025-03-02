using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using JustyBase.Common.Models;
using JustyBase.Helpers.Interactions;
using System;
using System.Collections.ObjectModel;

namespace JustyBase.Models.Tools;

public partial class LogMessage : ObservableObject
{
    public DateTime Timestamp { get; set; }

    [ObservableProperty]
    public partial LogMessageType MessageType { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }
    public string Source { get; set; }
    [ObservableProperty]
    public partial string Message { get; set; }
    public ObservableCollection<StringPair> InnerMessages { get; set; } = [];

    public DataGridCollectionView InnerMessagesCollectionView { get; set; }
    public LogMessage()
    {
        InnerMessagesCollectionView = new DataGridCollectionView(InnerMessages);
    }

    public void AddInnerMessageInUiThread(string message, DateTime titleTime)
    {
        MessageForUserTools.DispatcherAction(() =>
        {
            InnerMessages.Insert(0, new StringPair() { PairTitle = titleTime, PairMessage = message });
        });
    }
}

