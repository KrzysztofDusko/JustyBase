namespace JustyBase.Views.OtherDialogs;

public partial class AskForConfirm : Window
{
    public AskForConfirm()
    {
        InitializeComponent();
        this.DataContextChanged += AskForConfirm_DataContextChanged;
    }
    private void AskForConfirm_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (this.DataContext is JustyBase.ViewModels.AskForConfirmViewModel vm)
        {
            vm.CloseAction = () => this.Close();
        }
    }
}