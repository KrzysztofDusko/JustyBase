using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using JustyBase.Common.Contracts;
using JustyBase.Editor;
using JustyBase.Themes;
using JustyBase.ViewModels;
using JustyBase.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

namespace JustyBase;

public class App : Application
{
    private static IThemeManager? _themeManager;
    private static IGeneralApplicationData _generalApplicationData;
    public override void Initialize()
    {
        // Register all the services needed for the application to run
        var collection = new ServiceCollection();
        collection.AddCommonServices();
        // Creates a ServiceProvider containing services from the provided IServiceCollection
        _services = collection.BuildServiceProvider();
        _generalApplicationData = _services.GetRequiredService<IGeneralApplicationData>();

        _themeManager = _services.GetRequiredService<IThemeManager>();
        _themeManager.Initialize(this);
        AvaloniaXamlLoader.Load(this);

        foreach (var item in IGeneralApplicationData.REGISTERED_EXTENSIONS)
        {
            var (name, assetName, isXml) = item.Value;
            var uri = new Uri($"avares://JustyBase/Assets/{assetName}");
            using (var stream = AssetLoader.Open(uri))
            {
                using (var reader = new System.Xml.XmlTextReader(stream))
                {
                    AvaloniaEdit.Highlighting.HighlightingManager.Instance.RegisterHighlighting(item.Value.name, [],
                        AvaloniaEdit.Highlighting.Xshd.HighlightingLoader.Load(reader,
                            AvaloniaEdit.Highlighting.HighlightingManager.Instance));
                }
            }
        }

        if (_generalApplicationData.Config.ThemeNum == 1) // dark, default style in SQL is light
        {
            SqlCodeEditorHelpers.ResetStyle(dark: true);
        }
    }

    private static ServiceProvider _services;
    public static T GetRequiredService<T>()
    {
        return _services.GetRequiredService<T>();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // DockManager.s_enableSplitToWindow = true;
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktopLifetime:
                {
                    var mainWindowViewModel = _services.GetRequiredService<MainWindowViewModel>();
                    if (Debugger.IsAttached || !_generalApplicationData.Config.UseSplashScreen)
                    {
                        var mainWindow = new MainWindow
                        {
                            DataContext = mainWindowViewModel
                        };
                        //mainWindow.Closing += (_, _) =>
                        //{
                        //    mainWindowViewModel.CloseLayout();
                        //};

                        mainWindow.Show();
                        mainWindow.Focus();

                        desktopLifetime.MainWindow = mainWindow;
                    }
                    else
                    {
                        desktopLifetime.MainWindow = new SplashWindow(() =>
                        {
                            var mainWindow = new MainWindow
                            {
                                DataContext = mainWindowViewModel
                            };
                            //mainWindow.Closing += (_, _) =>
                            //{
                            //    mainWindowViewModel.CloseLayout();
                            //};

                            mainWindow.Show();
                            mainWindow.Focus();

                            desktopLifetime.MainWindow = mainWindow;
                            //desktopLifetime.Exit += (_, _) =>
                            //{
                            //    mainWindowViewModel.CloseLayout();
                            //};

                        });
                    }
                    break;
                }
        }

        base.OnFrameworkInitializationCompleted();
    }
}