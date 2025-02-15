using JustyBase.ViewModels.Tools;

namespace JustyBase.Views.Tools;

public partial class VariablesView : UserControl
{
    public VariablesView()
    {
        InitializeComponent();
    }

    private VariablesViewModel? ViewModel => DataContext as VariablesViewModel;

    //referenced in xaml
    private void VariablesDataGrid_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Delete:
                ViewModel?.RemoveSelectedVariable();
                break;
            case Key.OemPlus or Key.Add:
                ViewModel?.AddVariableFromEditorOrByPlus("newVar", "0");
                break;
            case Key.F5:
                ViewModel?.UpdateVariablesCompletition();
                break;
        }
    }

    //referenced in xaml
    private void VariablesDataGrid_DoubleTapped(object sender, RoutedEventArgs e)
    {
        ViewModel?.DataGridDoubleClicked();
    }
}
