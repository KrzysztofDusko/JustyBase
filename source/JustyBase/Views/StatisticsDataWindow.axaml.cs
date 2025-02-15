using JustyBase.Common.Contracts;

namespace JustyBase.Views;

public partial class StatisticsDataWindow : Window
{
    private readonly IGeneralApplicationData _generalApplicationData;
    public StatisticsDataWindow()
    {
        _generalApplicationData = App.GetRequiredService<IGeneralApplicationData>();
        InitializeComponent();
        btStatsOk.Click += BtStatsOk_Click;
        btStatsNotOk.Click += BtStatsNotOk_Click;
    }

    private void BtStatsNotOk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _generalApplicationData.Config.AcceptDiagData = false;
        _generalApplicationData.Config.AcceptCrashData = false;
        this.Close();
    }

    private void BtStatsOk_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _generalApplicationData.Config.AcceptDiagData = true;
        _generalApplicationData.Config.AcceptCrashData = true;
        this.Close();
    }
}
