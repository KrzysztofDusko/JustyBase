using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.Common.Helpers;
using JustyBase.Common.Models;
using JustyBase.PluginCommon.Contracts;
using JustyBase.ViewModels.Docks;
using JustyBase.ViewModels.Documents;
using JustyBase.ViewModels.Tools;
using JustyBase.ViewModels.Views;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Orientation = Dock.Model.Core.Orientation;

namespace JustyBase.ViewModels;

public sealed class DockFactory(IGeneralApplicationData generalApplicationData, IOtherHelpers otherHelpers, ISimpleLogger simpleLogger, IEncryptionHelper encryptionHelper,
    IMessageForUserTools messageForUserTools) : Factory
{
    private readonly IGeneralApplicationData _generalApplicationData = generalApplicationData;
    private readonly ISimpleLogger _simpleLogger = simpleLogger;
    private readonly IOtherHelpers _otherHelpers = otherHelpers;
    private readonly IEncryptionHelper _encryptionHelper = encryptionHelper;
    private readonly IMessageForUserTools _messageForUserTools = messageForUserTools;
    private IRootDock? _rootDock;

    private IDocumentDock? _mainDocumentDockTmp;
    private IDocumentDock? MainDocumentDock => _mainDocumentDockTmp ??= (DocumentDock)this.FindDockable(_rootDock, a => a is DocumentDock);

    public bool IsLastDocument()
    {
        return MainDocumentDock?.VisibleDockables?.Count == 1;
    }

    public void ResetMainDocumentDockTmp()
    {
        _mainDocumentDockTmp = (DocumentDock)this.FindDockable(_rootDock, a => a is DocumentDock);
    }

    public override IDocumentDock CreateDocumentDock() => new CustomDocumentDock();

    public override IRootDock CreateLayout()
    {
        _rootDock = CreateFreshLayout();
        return _rootDock;
    }

    public void CloseOldAddNewConnection()
    {
        try
        {
            var addNewOldTool = this.FindDockable(_rootDock, x => x.Id == "newConnectionTab");
            if (addNewOldTool is not null)
            {
                addNewOldTool.CanClose = true;
                (addNewOldTool.Owner as ToolDock)?.VisibleDockables.Remove(addNewOldTool);
            }
        }
        catch (Exception)
        {
        }
    }

    private SqlResultsFastViewModel _sqlResultsFastViewModel;
    public SqlResultsFastViewModel SqlResultsFastViewModel => _sqlResultsFastViewModel;

    public int LayoutCount { get; set; } = 3;
    public IRootDock CreateFreshLayout()
    {
        int layoutNum = _generalApplicationData.Config.LayoutNum;

        IList<IDockable> documentsList = CreateList<IDockable>();
        foreach (var (tabId, offlineTabData) in _generalApplicationData.GetDocumentsKeyValueCollection())
        {
            if (offlineTabData.HotDocumentViewModel is not null)
            {
                var doc = offlineTabData.HotDocumentViewModelAsT<SqlDocumentViewModel>();
                doc.SelectedConnectionIndex = offlineTabData.ConnectionIndex;
                documentsList.Add(doc);
                ActiveSqlDocumentViewModel = doc;
            }
            else
            {
                var doc = App.GetRequiredService<SqlDocumentViewModel>();
                doc.Id = tabId;
                doc.Title = offlineTabData.Title;
                doc.FontSize = offlineTabData.FontSize;

                doc.Id = tabId;
                doc.SelectedConnectionIndex = offlineTabData.ConnectionIndex;
                if (offlineTabData.SqlFilePath is not null)
                {
                    doc.FilePath = offlineTabData.SqlFilePath;
                }

                offlineTabData.HotDocumentViewModel = doc;
                documentsList.Add(doc);
                ActiveSqlDocumentViewModel = doc;
            }
        }
        if (documentsList.Count == 0)
        {
            string title = "your first sql";
            string docId = _generalApplicationData.AddNewDocument(title);

            var newDockable = App.GetRequiredService<SqlDocumentViewModel>();
            newDockable.Id = docId;
            newDockable.Title = "your first sql";
            newDockable.FontSize = _generalApplicationData.Config.DefaultFontSizeForDocuments;

            documentsList.Add(newDockable);
            _generalApplicationData.GetDocumentVmById(docId).HotDocumentViewModel = newDockable;
        }

        var dbSchemaVM = App.GetRequiredService<DbSchemaViewModel>();
        dbSchemaVM.Id = "DbSchema";
        dbSchemaVM.Title = "Schema";
        dbSchemaVM.CanClose = false;
        dbSchemaVM.CanPin = true;
        dbSchemaVM.CanFloat = false;

        //var addNewConnectionViewModel = new AddNewConnectionViewModel (){ Id = "newConnectionTab", Title = "Add New", CanClose = false, CanPin = true, CanFloat = false };

        var tool2VariablesVM = App.GetRequiredService<VariablesViewModel>();
        tool2VariablesVM.Id = "Variables";
        tool2VariablesVM.Title = "Variables";
        tool2VariablesVM.CanClose = false;
        tool2VariablesVM.CanPin = true;
        tool2VariablesVM.CanFloat = false;

        var ltvm = App.GetRequiredService<LogToolViewModel>();
        var schemaSearch = new SchemaSearchViewModel(this, _generalApplicationData, _messageForUserTools, ltvm) { Id = "schemaSearch", Title = "Schema search", CanClose = false, CanPin = true, CanFloat = false };

        var fileExloprer = App.GetRequiredService<FileExplorerViewModel>();
        fileExloprer.Id = "File explorer";
        fileExloprer.Title = "Files";
        fileExloprer.CanClose = false;
        fileExloprer.CanPin = true;
        fileExloprer.CanFloat = false;

        var _logViewModel = App.GetRequiredService<LogToolViewModel>();
        _logViewModel.Id = "LogTool";
        _logViewModel.Title = "Log";
        _logViewModel.CanClose = false;
        _logViewModel.CanPin = true;
        _logViewModel.CanFloat = false;

        _sqlResultsFastViewModel = App.GetRequiredService<SqlResultsFastViewModel>();
        _sqlResultsFastViewModel.Id = "FastViewModel";
        _sqlResultsFastViewModel.Title = "Results";
        _sqlResultsFastViewModel.CanClose = false;
        _sqlResultsFastViewModel.CanPin = true;
        _sqlResultsFastViewModel.CanFloat = false;

        var resDockInstance = new ToolDock
        {
            Title = "ResultsDock",
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>(_sqlResultsFastViewModel),
            Alignment = Alignment.Bottom,
            GripMode = GripMode.Hidden,
            CanClose = false,
            AutoHide = false,
            IsCollapsable = false,
            CanPin = false,
            CanFloat = false,
            Proportion = 0.25
        };

        var documentDock = new CustomDocumentDock
        {
            IsCollapsable = false,
            ActiveDockable = documentsList[0],
            VisibleDockables = documentsList,
            //ActiveDockable = document1,
            //VisibleDockables = CreateList<IDockable>(document1, document2, document3),
            CanCreateDocument = true,
            CanPin = true
        };

        var middleDock = new ProportionalDock
        {
            Proportion = 0.75,
            Title = "MiddleDock",
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = CreateList<IDockable>
            (
                documentDock,
                new ProportionalDockSplitter(),
                //MainResultDock
                //_mainResultDockTmp
                //SqlResultsFastViewModelInstance
                resDockInstance
            )
        };


        ProportionalDock? mainLayout = null;
        if (layoutNum == 0)
        {
            var leftDock = new ProportionalDock
            {
                Proportion = 0.25,
                Orientation = Orientation.Vertical,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    new ToolDock
                    {
                        ActiveDockable = dbSchemaVM,
                        VisibleDockables = CreateList<IDockable>(dbSchemaVM/*, addNewConnectionViewModel*/),
                        Alignment = Alignment.Left
                    },
                    new ProportionalDockSplitter(),
                    new ToolDock
                    {
                        ActiveDockable = schemaSearch,
                        VisibleDockables = CreateList<IDockable>(schemaSearch, _logViewModel),
                        Alignment = Alignment.Left
                    }
                )
            };

            var rightDock = new ProportionalDock
            {
                Proportion = 0.25,
                Orientation = Orientation.Vertical,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    new ToolDock
                    {
                        ActiveDockable = tool2VariablesVM,
                        VisibleDockables = CreateList<IDockable>(tool2VariablesVM),
                        Alignment = Alignment.Right,
                        //GripMode = GripMode.Visible
                    },
                    new ProportionalDockSplitter(),
                    new ToolDock
                    {
                        ActiveDockable = fileExloprer,
                        VisibleDockables = CreateList<IDockable>(fileExloprer),
                        Alignment = Alignment.Right,
                        //GripMode = GripMode.Visible
                    }
                )
            };

            mainLayout = new ProportionalDock
            {
                Orientation = Orientation.Horizontal,
                VisibleDockables = CreateList<IDockable>
               (
                   leftDock,
                   new ProportionalDockSplitter(),
                   middleDock,
                   // documentDock,
                   new ProportionalDockSplitter(),
                   rightDock
               )
            };
        }
        else if (layoutNum > 0)
        {
            var sideDock = new ProportionalDock
            {
                Proportion = 0.25,
                Orientation = Orientation.Vertical,
                ActiveDockable = null,
                VisibleDockables = CreateList<IDockable>
                (
                    new ToolDock
                    {
                        ActiveDockable = dbSchemaVM,
                        VisibleDockables = CreateList<IDockable>(dbSchemaVM, tool2VariablesVM),
                        Alignment = Alignment.Left
                    },
                    new ProportionalDockSplitter(),
                    new ToolDock
                    {
                        ActiveDockable = schemaSearch,
                        VisibleDockables = CreateList<IDockable>(schemaSearch, fileExloprer, _logViewModel),
                        Alignment = Alignment.Left
                    }
                )
            };


            if (layoutNum == 2)
            {
                mainLayout = new ProportionalDock
                {
                    Orientation = Orientation.Horizontal,
                    VisibleDockables = CreateList<IDockable>(new ProportionalDockSplitter(), middleDock, new ProportionalDockSplitter(), sideDock)
                };
            }
            else //1
            {
                mainLayout = new ProportionalDock
                {
                    Orientation = Orientation.Horizontal,
                    VisibleDockables = CreateList<IDockable>(sideDock, new ProportionalDockSplitter(), middleDock)
                };
            }
        }

        var mainViewModel = new MainViewModel
        {
            Id = "Home",
            Title = "Home",
            ActiveDockable = mainLayout,
            VisibleDockables = CreateList<IDockable>(mainLayout)
        };

        var rootDock = CreateRootDock();
        rootDock.IsCollapsable = false;
        //rootDock.Id = "Root";
        //rootDock.Title = "Root";
        rootDock.ActiveDockable = mainViewModel;
        rootDock.DefaultDockable = mainViewModel;
        rootDock.VisibleDockables = CreateList<IDockable>(mainViewModel);
        _rootDock = rootDock;

        return rootDock;
    }

    private readonly List<IDockable> _hidenDockables = [];
    private ProportionalDock? _middleDock;
    public void HideOrShowSideElements()
    {
        _middleDock ??= this.FindDockable(_rootDock, x => x.Title == "MiddleDock") as ProportionalDock;
        if (_middleDock is null)
        {
            _messageForUserTools.ShowSimpleMessageBoxInstance("middleDock is null");
            return;
        }

        if (_rootDock?.ActiveDockable is MainViewModel mainViewModel && mainViewModel.ActiveDockable is ProportionalDock pd)
        {
            if (_hidenDockables.Count > 0)
            {
                if (_generalApplicationData.Config.LayoutNum == 2)
                {
                    ProportionalDock tmpPD = null;
                    var proportionalDocks = _hidenDockables.Where(x => x is ProportionalDock).OfType<ProportionalDock>().ToList();
                    var splitters = _hidenDockables.Where(x => x is IProportionalDockSplitter).ToList();

                    if (proportionalDocks.Count == 1 && splitters.Count == 2)
                    {
                        if (_middleDock is not null)
                        {
                            _middleDock.Proportion = 1.0 - proportionalDocks[0].Proportion;
                        }
                        pd.VisibleDockables.Clear();
                        pd.VisibleDockables = CreateList<IDockable>(splitters[0], _middleDock, splitters[1], proportionalDocks[0]);
                    }
                    else
                    {
                        foreach (var item in _hidenDockables)
                        {
                            if (item is ProportionalDock proportional1 && proportional1 != _middleDock)
                            {
                                tmpPD = proportional1;
                                pd.VisibleDockables.Add(item); // on right
                            }
                            else
                            {
                                pd.VisibleDockables.Add(item);
                            }
                        }
                        if (tmpPD is not null)
                        {
                            tmpPD.Proportion = 0.25;
                        }
                        if (_middleDock is not null)
                        {
                            _middleDock.Proportion = 0.75;
                        }
                    }

                    _hidenDockables.Clear();
                }
                else if (_generalApplicationData.Config.LayoutNum == 1) //legacyLike
                {
                    _hidenDockables.Reverse(); // for most common case
                    foreach (var item in _hidenDockables)
                    {
                        pd.VisibleDockables.Insert(0, item);
                    }
                    if (pd.VisibleDockables.Count >= 1)
                    {
                        if (pd.VisibleDockables[0] is ProportionalDock proportional)
                        {
                            proportional.Proportion = proportional.Proportion;
                        }
                        if (_middleDock is not null)
                        {
                            _middleDock.Proportion = 1.0 - ((pd.VisibleDockables[0] as ProportionalDock)?.Proportion ?? 0.5);
                        }
                        _hidenDockables.Clear();
                        var tmp = pd.VisibleDockables[0];
                        pd.VisibleDockables.Clear();

                        pd.VisibleDockables = CreateList<IDockable>(tmp, new ProportionalDockSplitter(), _middleDock);
                    }
                }
                else if (_generalApplicationData.Config.LayoutNum == 0)
                {
                    ProportionalDock tmpPD = null;
                    var proportionalDocks = _hidenDockables.Where(x => x is ProportionalDock).OfType<ProportionalDock>().ToList();
                    var splitters = _hidenDockables.Where(x => x is IProportionalDockSplitter).ToList();

                    if (proportionalDocks.Count == 2 && splitters.Count == 2)
                    {
                        if (_middleDock is not null)
                        {
                            _middleDock.Proportion = 0.5;
                        }
                        pd.VisibleDockables.Clear();
                        pd.VisibleDockables = CreateList<IDockable>(proportionalDocks[0], splitters[0], _middleDock, splitters[1], proportionalDocks[1]);
                    }
                    else
                    {
                        foreach (var item in _hidenDockables)
                        {
                            if (item is ProportionalDock proportional1 && proportional1 != _middleDock)
                            {
                                tmpPD = proportional1;
                                pd.VisibleDockables.Add(item); // on right
                            }
                            else
                            {
                                pd.VisibleDockables.Add(item);
                            }
                        }
                        if (tmpPD is not null)
                        {
                            tmpPD.Proportion = 0.25;
                        }
                        if (_middleDock is not null)
                        {
                            _middleDock.Proportion = 0.75;
                        }
                    }

                    _hidenDockables.Clear();
                }
                else
                {
                    _hidenDockables.Reverse(); // for most common case
                    ProportionalDock tmpPD = null;
                    foreach (var item in _hidenDockables)
                    {
                        pd.VisibleDockables.Insert(0, item);
                        if (item is ProportionalDock proportional1 && proportional1 != _middleDock)
                        {
                            tmpPD = proportional1;
                        }
                    }
                    if (tmpPD is not null)
                    {
                        tmpPD.Proportion = 0.25;
                    }
                    if (_middleDock is not null)
                    {
                        _middleDock.Proportion = 0.75;
                    }
                    _hidenDockables.Clear();
                }
            }
            else
            {
                foreach (var item in pd.VisibleDockables)
                {
                    if (item != _middleDock && item.Title != "MiddleDock")
                    {
                        _hidenDockables.Add(item);
                    }
                }
                foreach (var item in _hidenDockables)
                {
                    pd.VisibleDockables.Remove(item);
                }
            }
        }
    }
    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object>>
        {
            ["DbSchema"] = () => new object(),
            ["Variables"] = () => new object(),
            ["SchemaSearch"] = () => new object(),
            ["FileExplorer"] = () => new object(),
            ["LogTool"] = () => new object(),
            ["Dashboard"] = () => layout,
            ["Home"] = () => () => new object()
        };

        //foreach (var item in _generalApplicationData.GetDocumentsKeyValueCollection())
        //{
        //    ContextLocator[item.Key] = (() => new object());
        //}

        DockableLocator = new Dictionary<string, Func<IDockable?>>()
        {
            ["Root"] = () => _rootDock,
            ["Documents"] = () => MainDocumentDock,
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
        if (_generalApplicationData.TryGetDocumentById(_generalApplicationData.SelectedTabIdFromStart, out var savedTabData) && savedTabData.HotDocumentViewModel is IDockable dockable)
        {
            MainDocumentDock.ActiveDockable = dockable;
        }

    }

    public void ClosePrevResults(string id)
    {
        _sqlResultsFastViewModel.ClearFromDocument(id, false);
    }

    public void AddNewResult((IDatabaseService? dbService, DbDataReader rdr, string errorMessage) res, string id, int queryNum, ref int abortUbound, string sql, DbCommand command, string title)
    {
        if (!_generalApplicationData.TryGetDocumentById(id, out var result))
        {
            return;
        }

        SqlResultsViewModel tool = App.GetRequiredService<SqlResultsViewModel>();
        tool.Id = $"ID_RESULT_{Guid.NewGuid()}_{id}";
        tool.RelatedSqlDocumentId = id;
        tool.Title = title ?? $"{result.HotDocumentViewModel.TitleFromDocumentVm}";
        tool.CanPin = false;
        tool.CanFloat = false;
        tool.CanClose = true;
        tool.SQL = sql;

        tool.LoadData(res);

        if (_generalApplicationData.TryGetDocumentById(id, out var savedX))
        {
            _sqlResultsFastViewModel.Add(tool, savedX.HotDocumentViewModelAsT<SqlDocumentViewModel>(), IsActiveDockable(savedX.HotDocumentViewModelAsT<SqlDocumentViewModel>()));
        }
        else
        {
            _sqlResultsFastViewModel.Add(tool, savedX.HotDocumentViewModelAsT<SqlDocumentViewModel>(), true);
        }

        tool.GridEnabled = false;
        if (res.rdr is not null && res.rdr.HasRows && res.rdr.FieldCount > 0)
        {
            tool.LoadRest(res.dbService, res.rdr, queryNum, ref abortUbound, command);
        }
        else
        {
            _messageForUserTools.DispatcherActionInstance(() =>
            {
                tool.GridEnabled = true;
            });
        }
    }

    public void ResultsFromActiveTab(SqlDocumentViewModel viewModel)
    {
        Debug.Assert(_sqlResultsFastViewModel is not null);
        _sqlResultsFastViewModel?.ShowDocumentResult(viewModel);
        App.GetRequiredService<LogToolViewModel>().SwitchLogs(viewModel.Id);
    }

    public List<SqlResultsViewModel> GetDocumentResults(SqlDocumentViewModel viewModel)
    {
        List<SqlResultsViewModel> results = [];
        var collection = _sqlResultsFastViewModel.GetDocumentResults(viewModel);
        foreach (var result in collection)
        {
            results.Add(result);
        }
        return results;
    }

    public void SaveStartupSqlAndFiles(string? selectedTabId = null)
    {
        selectedTabId ??= MainDocumentDock.ActiveDockable.Id;
        OfflineDocumentContainer mn = _generalApplicationData.GetOfflineDocumentContainer(selectedTabId);

        SortOfflineTabs(mn);

        string txt = JsonSerializer.Serialize(mn, MyJsonContextOfflineDocumentContainer.Default.OfflineDocumentContainer);
        mn.SelectedTabId = selectedTabId;
        _encryptionHelper.SaveTextFileEncoded(IGeneralApplicationData.StartupPath, txt);
    }

    private void SortOfflineTabs(OfflineDocumentContainer mn)
    {
        Dictionary<string, int> nums = [];
        int index = 0;
        foreach (var item1 in MainDocumentDock.VisibleDockables)
        {
            nums[item1.Id] = index++;
        }
        foreach (var item in mn.SqlOfflineDocumentDictionary)
        {
            nums.TryAdd(item.Key, Int32.MaxValue);
        }

        Dictionary<string, OfflineTabData> sortedDict = mn.SqlOfflineDocumentDictionary.OrderBy(pair => nums[pair.Key]).ToDictionary(pair => pair.Key, pair => pair.Value);
        mn.SqlOfflineDocumentDictionary = sortedDict;
    }

    public void MakeAllResultsHidden()
    {
        _sqlResultsFastViewModel.HideAllResult();
    }

    public T GetViewModelOfType<T>()
    {
        T obj = default;
        foreach (var dock in MainDocumentDock.VisibleDockables)
        {
            if (dock is T tObject)
            {
                obj = tObject;
            }
        }
        return obj;
    }

    public void AddHistoryDocument()
    {
        HistoryViewModel his = GetViewModelOfType<HistoryViewModel>();

        if (his is not null)
        {
            MainDocumentDock.VisibleDockables.Remove(his);
        }
        his = App.GetRequiredService<HistoryViewModel>();
        MainDocumentDock.VisibleDockables.Add(his);
        MainDocumentDock.ActiveDockable = his;
    }

    public void AddSettingsDocument()
    {
        SettingsViewModel set = GetViewModelOfType<SettingsViewModel>();
        if (set is null)
        {
            set = App.GetRequiredService<SettingsViewModel>();
            MainDocumentDock.VisibleDockables.Add(set);
        }
        MainDocumentDock.ActiveDockable = set;
    }
    public void AddImportDocument()
    {
        ImportViewModel imp = GetViewModelOfType<ImportViewModel>();
        if (imp is null)
        {
            imp = App.GetRequiredService<ImportViewModel>();
            MainDocumentDock.VisibleDockables.Add(imp);
        }
        MainDocumentDock.ActiveDockable = imp;
    }
    public void AddEtlDocument()
    {
        EtlViewModel etlVM = GetViewModelOfType<EtlViewModel>();
        if (etlVM is null)
        {
            etlVM = EtlViewModel.Instance;
            MainDocumentDock.VisibleDockables.Add(etlVM);
        }
        MainDocumentDock.ActiveDockable = etlVM;
    }

    public SqlDocumentViewModel? ActiveSqlDocumentViewModel;
    public void InsertTextToActiveDocument(object data, bool rawMode)
    {
        ActiveSqlDocumentViewModel?.InserTextToEditor(data, rawMode);
    }

    public void InsertSnippetTextToActiveDocument(string text, string connectionName)
    {
        ActiveSqlDocumentViewModel?.InserSnippet(text);
        ActiveSqlDocumentViewModel?.TrySetConnection(connectionName);
        try
        {
            ActiveSqlDocumentViewModel?.SqlEditor.ForceUpdateFoldings();
            ActiveSqlDocumentViewModel?.SqlEditor.CollapseFoldings();
        }
        catch (Exception ex)
        {
            _simpleLogger.TrackError(ex, isCrash: false);
            _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
        }
    }

    public Action<string> AtCharAction;
    public Action<string> SelectedDataGridAction;

    public void AddNewDocumentFromFile(IEnumerable<string> files)
    {
        foreach (var fullFileName in files)
        {
            if (_generalApplicationData.TryGetOpenedDocumentVmByFilePath(fullFileName, out var vm) && vm is SqlDocumentViewModel sqlVm)
            {
                MainDocumentDock.ActiveDockable = sqlVm;
                continue;
            }
            var fileInfo = new FileInfo(fullFileName);
            if (fileInfo.Length >= 20 * 1024 * 1024) // 20MB+ is to big to open in standard mode
            {
                var res = _otherHelpers.CsvTxtPreviewer(fullFileName);
                this.AddNewDocument(res, true);
                continue;
            }

            string title = Path.GetFileName(fullFileName);
            string docId = _generalApplicationData.AddNewDocument(title);

            var newDockable = App.GetRequiredService<SqlDocumentViewModel>();
            newDockable.Id = docId;
            newDockable.Title = title;
            newDockable.FilePath = fullFileName;
            newDockable.FontSize = ISomeEditorOptions.DEFAULT_DOCUMENT_FONT_SIZE;

            _generalApplicationData.GetDocumentVmById(docId).HotDocumentViewModel = newDockable;

            if (MainDocumentDock is not null)
            {
                MainDocumentDock.VisibleDockables.Add(newDockable);
                MainDocumentDock.ActiveDockable = newDockable;
            }
        }
    }

    public bool IsActiveDockable(IDockable dockable)
    {
        return MainDocumentDock?.ActiveDockable == dockable;
    }

    public SqlDocumentViewModel AddNewDocumentFromTxtPreview(string path)
    {
        var res = _otherHelpers.CsvTxtPreviewer(path);
        return AddNewDocument(res, true);
    }

    public SqlDocumentViewModel AddNewDocument(string? initText = null, bool txtPreview = false, string? forcedTitle = null)
    {
        string title = forcedTitle ?? "Document" + (MainDocumentDock.VisibleDockables.Count + 1);
        string docId = _generalApplicationData.AddNewDocument(title, initText);
        SqlDocumentViewModel newDockable = App.GetRequiredService<SqlDocumentViewModel>();
        newDockable.TxtPreview = txtPreview;
        newDockable.Id = docId;
        newDockable.Title = title;

        newDockable.FontSize = _generalApplicationData.Config.DefaultFontSizeForDocuments;
        _generalApplicationData.GetDocumentVmById(docId).HotDocumentViewModel = newDockable;
        MainDocumentDock.VisibleDockables.Add(newDockable);
        MainDocumentDock.ActiveDockable = newDockable;
        return newDockable;
    }

    public void NextActiveDocument(object data)
    {
        int cnt = MainDocumentDock.VisibleDockables.Count;
        int activeIndex = MainDocumentDock.VisibleDockables.IndexOf(MainDocumentDock.ActiveDockable);
        int sign = 1;
        if (data.ToString() == "-")
        {
            sign = -1;
        }

        MainDocumentDock.ActiveDockable = MainDocumentDock.VisibleDockables[(cnt + activeIndex + sign) % cnt];
    }
}
