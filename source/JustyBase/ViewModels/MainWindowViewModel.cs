using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Core.Events;
using JustyBase.Common.Contracts;
using JustyBase.Public.Lib.Servces;
using JustyBase.Services;
using JustyBase.ViewModels.Documents;
using JustyBase.Views;
using System;
using System.Diagnostics;

namespace JustyBase.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFactory? _factory;
    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly IMessageForUserTools _messageForUserTools;

    [ObservableProperty]
    public partial IRootDock? Layout { get; set; }

    private DockFactory? CurrentDockFactory => _factory as DockFactory;

    public bool? AutoDownloadUpdate => _generalApplicationData?.Config?.AutoDownloadUpdate;

    [ObservableProperty]
    public partial string CharAtMessage { get; set; }

    [ObservableProperty]
    public partial string SelectedRowsCount { get; set; }

    [RelayCommand]
    private void ShowAbout()
    {
        var ab = new About();
        _ = ab.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
    }

    [RelayCommand]
    private void ShowHistory()
    {
        CurrentDockFactory?.AddHistoryDocument();
    }

    [RelayCommand]
    private void ShowSettings()
    {
        CurrentDockFactory?.AddSettingsDocument();
    }

    [RelayCommand]
    private void Import()
    {
        CurrentDockFactory?.AddImportDocument();
    }

    [RelayCommand]
    private void ShowEtl()
    {
        CurrentDockFactory?.AddEtlDocument();
    }

    [RelayCommand]
    private void WindowClosing()
    {
        CurrentDockFactory.SaveStartupSqlAndFiles();
        _generalApplicationData.SaveConfig();
    }

    [RelayCommand]
    private void OpenNewTab()
    {
        CurrentDockFactory?.AddNewDocument(null);
    }

    [RelayCommand]
    private void ChangeActiveTab(string param)
    {
        CurrentDockFactory?.NextActiveDocument(param);
    }

    [RelayCommand]
    private void ConcentrateMode()
    {
        CurrentDockFactory?.HideOrShowSideElements();
    }

    public MainWindowViewModel(IFactory factory, IGeneralApplicationData generalApplicationData, IAvaloniaSpecificHelpers avaloniaSpecificHelpers,
        IMessageForUserTools messageForUserTools)
    {
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        _generalApplicationData = generalApplicationData;
        _factory = factory;
        _messageForUserTools = messageForUserTools;

        CharAtMessage = "";
        if (CurrentDockFactory is not null)
        {
            CurrentDockFactory.AtCharAction = s => CharAtMessage = s;
            CurrentDockFactory.SelectedDataGridAction = s => SelectedRowsCount = s;
        }

#if DEBUG
        DebugFactoryEvents(_factory);
#endif

        _factory.FocusedDockableChanged += (_, args) =>
        {
            if (args.Dockable is SqlDocumentViewModel viewModel && _factory is DockFactory dockFactory)
            {
                dockFactory.ActiveSqlDocumentViewModel = viewModel;
                //dockFactory.ResultsFromActiveTab();
            }
        };
        _factory.ActiveDockableChanged += (_, args) =>
        {
            if (args.Dockable is SqlDocumentViewModel viewModel && viewModel.IsRecentlyFinished)
            {
                viewModel.IsRecentlyFinished = false;
            }
        };

        _factory.DockableClosed += Factory_DockableClosed;

        Layout = _factory?.CreateLayout();
        if (Layout is { })
        {
            _factory?.InitLayout(Layout);
            //if (Layout is { } root)
            //{
            //    root.Navigate.Execute("Home");
            //}
        }

        var args = Environment.GetCommandLineArgs();

        if (args.Length > 0 && args[^1].EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
        {
            CurrentDockFactory?.AddNewDocumentFromFile([args[1]]);
        }

        SetupPipeCommunication();

        CurrentDockFactory?.CloseOldAddNewConnection();
    }

    private void SetupPipeCommunication()
    {
        PipeComunicationService pipeComunicationService = new(Program.JbMessagePipeName)
        {
            ActivateOpenedFileAction = (x) => _messageForUserTools.DispatcherActionInstance(() =>
            {
                CurrentDockFactory?.AddNewDocumentFromFile([x]);
            }),
            RestoreAction = () => _messageForUserTools.DispatcherActionInstance(() =>
            {
                var mv = _avaloniaSpecificHelpers.GetMainWindow();
                var windowState = mv.WindowState;
                if (windowState != Avalonia.Controls.WindowState.Maximized)
                {
                    mv.WindowState = Avalonia.Controls.WindowState.Normal;
                }
            }),
            ExceptionAction = ex => _messageForUserTools.ShowSimpleMessageBoxInstance(ex)
        };

        pipeComunicationService.Start();
    }


    //for designer
    public MainWindowViewModel() : this(App.GetRequiredService<IFactory>(), App.GetRequiredService<IGeneralApplicationData>(), App.GetRequiredService<IAvaloniaSpecificHelpers>()
        , App.GetRequiredService<IMessageForUserTools>())
    {

    }

    private void Factory_DockableClosed(object sender, DockableClosedEventArgs e)
    {
        CleanUpDockable(e.Dockable);
    }
    private void CleanUpDockable(IDockable d)
    {
        ViewLocator.RemoveFromCache(d);
        if (d is ICleanableViewModel res)
        {
            res.DoCleanup();
        }
        if (d is SqlDocumentViewModel sqlVM)
        {
            CurrentDockFactory.SqlResultsFastViewModel.ClearFromDocument(sqlVM.Id, true);
        }
    }
#if DEBUG
    private void DebugFactoryEvents(IFactory factory)
    {
        factory.ActiveDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[ActiveDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.FocusedDockableChanged += (_, args) =>
        {
            Debug.WriteLine($"[FocusedDockableChanged] Title='{args.Dockable?.Title}'");
        };

        factory.DockableAdded += (_, args) =>
        {
            Debug.WriteLine($"[DockableAdded] Title='{args.Dockable?.Title}'");
        };

        factory.DockableRemoved += (_, args) =>
        {
            Debug.WriteLine($"[DockableRemoved] Title='{args.Dockable?.Title}'");
        };

        factory.DockableClosed += (_, args) =>
        {
            Debug.WriteLine($"[DockableClosed] Title='{args.Dockable?.Title}'");
        };

        factory.DockableMoved += (_, args) =>
        {
            Debug.WriteLine($"[DockableMoved] Title='{args.Dockable?.Title}'");
        };

        factory.DockableSwapped += (_, args) =>
        {
            Debug.WriteLine($"[DockableSwapped] Title='{args.Dockable?.Title}'");
        };

        factory.DockablePinned += (_, args) =>
        {
            Debug.WriteLine($"[DockablePinned] Title='{args.Dockable?.Title}'");
        };

        factory.DockableUnpinned += (_, args) =>
        {
            Debug.WriteLine($"[DockableUnpinned] Title='{args.Dockable?.Title}'");
        };

        factory.WindowOpened += (_, args) =>
        {
            Debug.WriteLine($"[WindowOpened] Title='{args.Window?.Title}'");
        };

        factory.WindowClosed += (_, args) =>
        {
            Debug.WriteLine($"[WindowClosed] Title='{args.Window?.Title}'");
        };

        factory.WindowClosing += (_, args) =>
        {
            // NOTE: Set to True to cancel window closing.
#if false
                args.Cancel = true;
#endif      
        };

        factory.WindowAdded += (_, args) =>
        {
            Debug.WriteLine($"[WindowAdded] Title='{args.Window?.Title}'");
            //factory.InsertDockable((_factory as DockFactory)._rootDock, args.Window, 0);
        };

        factory.WindowRemoved += (_, args) =>
        {
            Debug.WriteLine($"[WindowRemoved] Title='{args.Window?.Title}'");
        };

        factory.WindowMoveDragBegin += (_, args) =>
        {
            // NOTE: Set to True to cancel window dragging.
#if false
                args.Cancel = true;
#endif
            Debug.WriteLine($"[WindowMoveDragBegin] Title='{args.Window?.Title}', Cancel={args.Cancel}, X='{args.Window?.X}', Y='{args.Window?.Y}'");
        };

        factory.WindowMoveDrag += (_, args) =>
        {
            Debug.WriteLine($"[WindowMoveDrag] Title='{args.Window?.Title}', X='{args.Window?.X}', Y='{args.Window?.Y}");
        };

        factory.WindowMoveDragEnd += (_, args) =>
        {
            Debug.WriteLine($"[WindowMoveDragEnd] Title='{args.Window?.Title}', X='{args.Window?.X}', Y='{args.Window?.Y}");
        };
    }
#endif
    //public void CloseLayout()
    //{
    //    if (Layout is IDock)
    //    {
    //        CurrentDockFactory.SaveStartupSqlAndFiles();
    //    }
    //}

    [RelayCommand]
    private void NewLayout()
    {
        _generalApplicationData.Config.LayoutNum = (_generalApplicationData.Config.LayoutNum + 1) % CurrentDockFactory.LayoutCount;
        CurrentDockFactory?.SaveStartupSqlAndFiles();

        SettingsViewModel? sVm = null;
        ImportViewModel? iVm = null;
        EtlViewModel? eVm = null;
        HistoryViewModel? hVm = null;

        if (_factory is DockFactory dock)
        {
            dock.MakeAllResultsHidden();
            sVm = dock.GetViewModelOfType<SettingsViewModel>();
            iVm = dock.GetViewModelOfType<ImportViewModel>();
            eVm = dock.GetViewModelOfType<EtlViewModel>();
            hVm = dock.GetViewModelOfType<HistoryViewModel>();
        }

        IRootDock layout = _factory?.CreateLayout();
        if (layout is not null)
        {
            Layout = layout;
            _factory?.InitLayout(layout);
        }
        if (_factory is DockFactory dock1)
        {
            dock1.ResetMainDocumentDockTmp();
            if (sVm is not null)
            {
                dock1.AddSettingsDocument();
            }
            if (iVm is not null)
            {
                dock1.AddImportDocument();
            }
            if (eVm is not null)
            {
                dock1.AddEtlDocument();
            }
            if (hVm is not null)
            {
                dock1.AddHistoryDocument();
            }
        }
    }
}
