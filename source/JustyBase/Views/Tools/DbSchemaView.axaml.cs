using JustyBase.Services;
using JustyBase.Services.Database;
using JustyBase.ViewModels.Tools;
using System.Threading.Tasks;

namespace JustyBase.Views.Tools;

public partial class DbSchemaView : UserControl
{
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    public DbSchemaView()
    {
        InitializeComponent();
        Initialized += DbSchemaView_Initialized;
        btAddNewConnection.Click += BtAddNewConnection_Click;
        btConnectionsSettings.Click += BtConnectionsSettings_Click;
        cmSchema.Opening += SchemaContextMenu_ContextMenuOpening;
        _avaloniaSpecificHelpers = App.GetRequiredService<IAvaloniaSpecificHelpers>();
    }

    private async void BtConnectionsSettings_Click(object? sender, RoutedEventArgs e)
    {
        var vmX = App.GetRequiredService<AddNewConnectionViewModel>();
        vmX.ShowExistings = true;

        var wn = new Window()
        {
            Content = new AddNewConnectionView()
            {
                DataContext = vmX
            },
            Width = 350,
            Height = 410,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowActivated = true,
            ShowInTaskbar = true,
            UseLayoutRounding = true,
            CornerRadius = new Avalonia.CornerRadius(5),
            ExtendClientAreaToDecorationsHint = true,
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.Default,
            CanResize = false
        };
        vmX.CloseWindowAction = () => wn.Close();
        wn.KeyDown += Wn_KeyDown;

        await wn.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
    }

    public async Task AddNewConnectionWindow()
    {
        var vmX = App.GetRequiredService<AddNewConnectionViewModel>();
        vmX.ShowExistings = false;

        var wn = new Window()
        {
            Content = new AddNewConnectionView() { DataContext = vmX },
            Width = 310,
            Height = 310,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowActivated = true,
            ShowInTaskbar = true,
            UseLayoutRounding = true,
            CornerRadius = new Avalonia.CornerRadius(5),
            ExtendClientAreaToDecorationsHint = true,
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.Default,
            CanResize = false
        };
        vmX.CloseWindowAction = () => wn.Close();
        wn.KeyDown += Wn_KeyDown;

        await wn.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
    }
    private async void BtAddNewConnection_Click(object? sender, RoutedEventArgs e)
    {
        await AddNewConnectionWindow();
    }

    private static void Wn_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.Return)
        {
            (sender as Window)?.Close();
        }
    }

    private DbSchemaViewModel ViewModel => this.DataContext as DbSchemaViewModel;
    private void DbSchemaView_Initialized(object? sender, System.EventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        ViewModel.FocusAndBringSelectionIntoView = () =>
        {
            dbSchemaTreeGrid.Focus();
            int nm = 0;
            foreach (var item in dbSchemaTreeGrid.Rows)
            {
                if (item.Model == dbSchemaTreeGrid.RowSelection.SelectedItem)
                {
                    break;
                }
                nm++;
            }
            dbSchemaTreeGrid.RowsPresenter.BringIntoView(nm);
        };
        dbSchemaTreeGrid.DoubleTapped += DbSchemaView_DoubleTapped;
    }

    private void DbSchemaView_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        dbSchemaTreeGrid.RowSelection.RangeAnchorIndex = dbSchemaTreeGrid.RowSelection.SelectedIndex;
        if (dbSchemaTreeGrid.RowSelection.SelectedItem is null)
        {
            return;
        }
        if (dbSchemaTreeGrid.RowSelection.SelectedItem is IDatabaseSchemaItem schemaModel)
        {
            IDatabaseSchemaItem.InsertDoubleClicked(schemaModel);
        }
    }
    private void SchemaContextMenu_ContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        this.ViewModel.PrepareContextMenu(null);
    }
}
