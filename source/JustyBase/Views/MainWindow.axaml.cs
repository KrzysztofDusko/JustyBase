using JustyBase.Common.Contracts;
using JustyBase.ViewModels;
using System;
using System.Threading.Tasks;

namespace JustyBase.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;
        this.Loaded += MainWindow_Loaded;
        Program.SetUpDispatcherExceptionHandling();
    }
    private async void MainWindow_Loaded(object? sender, EventArgs e)
    {
        await DoUpdate();
    }

    private async Task DoUpdate()
    {
        try
        {
            AboutViewModel aboutViewModel = App.GetRequiredService<AboutViewModel>();
            var updateInfo = await AboutViewModel.GetUpdateInfo();
            if (updateInfo is not null && (DataContext as MainWindowViewModel)?.AutoDownloadUpdate == true)
            {
                var updateTask = aboutViewModel.Update();
                await updateTask;
            }
            else if (updateInfo is not null)
            {
                aboutViewModel.IsUpdateAvaiable = true;
                var ab = new About() { DataContext = aboutViewModel };
                await ab.ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            if (ex.Message != "Cannot perform this operation in an application which is not installed.")
            {
                App.GetRequiredService<IMessageForUserTools>().ShowSimpleMessageBoxInstance(ex);
            }
        }
    }
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        (this.DataContext as MainWindowViewModel)?.WindowClosingCommand?.Execute(this);
    }
}


