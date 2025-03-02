using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using JustyBase.Database.Sample.ViewModels;
using JustyBase.Database.Sample.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JustyBase.Database.Sample;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var uri = new Uri($"avares://JustyBase.Database.Sample/Assets/SQL-Mode.xshd");
        using var stream = AssetLoader.Open(uri);
        using var reader = new System.Xml.XmlTextReader(stream);
        AvaloniaEdit.Highlighting.HighlightingManager.Instance.RegisterHighlighting("SQL", [],
            AvaloniaEdit.Highlighting.Xshd.HighlightingLoader.Load(reader,
                AvaloniaEdit.Highlighting.HighlightingManager.Instance));

    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);


            // Register all the services needed for the application to run
            var collection = new ServiceCollection();
            collection.AddCommonServices();
            // Creates a ServiceProvider containing services from the provided IServiceCollection
            var services = collection.BuildServiceProvider();
            _services = services;
            var vm = services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider _services;
    public static T GetRequiredService<T>()
    {
        return _services.GetRequiredService<T>();
    }


}