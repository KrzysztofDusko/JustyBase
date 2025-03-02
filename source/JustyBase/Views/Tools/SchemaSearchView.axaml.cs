using JustyBase.Common.Models;
using JustyBase.PluginCommon.Models;
using JustyBase.ViewModels.Tools;

namespace JustyBase.Views.Tools;

public partial class SchemaSearchView : UserControl
{
    public SchemaSearchView()
    {
        InitializeComponent();
        SchemaSearchDataGrid.LoadingRowGroup += Dg_LoadingRowGroup;
        SchemaSearchDataGrid.DoubleTapped += Dg_DoubleTapped;
        this.Initialized += SchemaSearchView_Initialized;
    }

    private SchemaSearchViewModel? ViewModel => this.DataContext as SchemaSearchViewModel;
    private async void Dg_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SchemaSearchDataGrid.SelectedItem is SchemaSearchItem searchItem)
        {
            await this.ViewModel?.DoubleTappedAction(searchItem);
        }
    }

    private async void SchemaSearchView_Initialized(object? sender, System.EventArgs e)
    {
        if (ViewModel is not null)
        {
            bool firstTime = false;
            if (ViewModel?.DG is null)
            {
                firstTime = true;
            }
            ViewModel.DG = SchemaSearchDataGrid;
            if (firstTime && ViewModel.RefreshStartup)
            {
                await ViewModel.RefreshDbCmd.ExecuteAsync(null);
            }
            else
            {
                ViewModel.TryGoupResults(SchemaSearchViewModel.GIANT_GROUP_LIMIT);
            }
        }
    }

    private void Dg_LoadingRowGroup(object? sender, DataGridRowGroupHeaderEventArgs e)
    {
        if (e.RowGroupHeader is DataGridRowGroupHeader group)
        {
            group.IsItemCountVisible = true;
            group.ItemCountFormat = "({0:N0} Items)";
        }
    }
}
