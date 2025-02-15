using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.Services;
using JustyBase.Views.OtherDialogs;

namespace JustyBase.ViewModels.Documents;


public partial class DocumentBaseVM : Document
{
    [ObservableProperty]
    public partial bool IsRecentlyFinished { get; set; } = false;

    public bool IsHistory => this is HistoryViewModel;
    public bool IsSettings => this is SettingsViewModel;
    public bool IsSql => this is SqlDocumentViewModel;
    public bool IsImport => this is ImportViewModel;
    public bool IsEtl => this is EtlViewModel;
    private bool _skipCloseQuestion = false;
    private readonly bool _confirmDocumentClosing = false;

    public DocumentBaseVM()
    {
        _confirmDocumentClosing = App.GetRequiredService<IGeneralApplicationData>().Config.ConfirmDocumentClosing;
    }

    public override bool OnClose()
    {
        if (_confirmDocumentClosing && Title?.EndsWith('*') == true && !_skipCloseQuestion
            || (Factory as DockFactory)?.IsLastDocument() == true && !_skipCloseQuestion
            )
        {
            var d = new AskForConfirm();
            var vm = new AskForConfirmViewModel
            {
                Title = "Close ?",
                TextMessage = $"Do you really want to close the {Title} document ?",
                AdditionalYesAction = () =>
                {
                    _skipCloseQuestion = true;
                    Factory.CloseDockable(this);
                }
            };
            d.DataContext = vm;
            d.ShowDialog(App.GetRequiredService<IAvaloniaSpecificHelpers>().GetMainWindow());
            return false;
        }
        return base.OnClose();
    }
}

