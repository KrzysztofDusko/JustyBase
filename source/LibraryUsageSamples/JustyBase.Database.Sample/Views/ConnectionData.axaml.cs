using Avalonia.Controls;
using JustyBase.Database.Sample.ViewModels;

namespace JustyBase.Database.Sample;

public partial class ConnectionData : UserControl
{
    public ConnectionData()
    {
        InitializeComponent();
        this.DataContext = App.GetRequiredService<ConnectionDataViewModel>();
    }
}