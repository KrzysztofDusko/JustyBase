using JustyBase.ViewModels;

namespace JustyBase.Views.ToolTipViews;

public partial class DbObjectQuickMenu : UserControl
{
    public DbObjectQuickMenu()
    {
        InitializeComponent();
    }
    public void ClickHandler(object sender, RoutedEventArgs args)
    {
        (this.DataContext as DbObjectQuickMenuViewModel)?.CloseAction();
    }
}