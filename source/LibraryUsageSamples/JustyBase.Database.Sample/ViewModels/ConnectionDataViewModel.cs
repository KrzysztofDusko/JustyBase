using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using System;

namespace JustyBase.Database.Sample.ViewModels;

public sealed partial class ConnectionDataViewModel : ViewModelBase
{
    private readonly IEncryptionHelper _encryptionHelper;
    public ConnectionDataViewModel(IEncryptionHelper encryptionHelper)
    {
        Servername = "localhost";
        Database = "test";
        Username = "root";
        Password = "root";
        Port = "5480";
        _encryptionHelper = encryptionHelper;
    }
    [ObservableProperty]
    public partial string Servername { get; set; }
    [ObservableProperty]
    public partial string Database { get; set; }
    [ObservableProperty]
    public partial string Username { get; set; }
    [ObservableProperty]
    public partial string Password { get; set; }
    [ObservableProperty]
    public partial string Port { get; set; }

    [RelayCommand]
    private void Save()
    {
        string env = $"servername={Servername};port={Port};database={Database};username={Username};password={Password};";
        Environment.SetEnvironmentVariable("NetezzaTest", _encryptionHelper.Encrypt(env), EnvironmentVariableTarget.User);
        // Shutdown current instance
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

}
