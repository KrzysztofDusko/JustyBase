using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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