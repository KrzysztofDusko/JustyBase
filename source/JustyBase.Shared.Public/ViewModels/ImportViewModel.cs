//TODO
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using JustyBase.Common.Models;
using JustyBase.Common.Tools.ImportHelpers;
using JustyBase.Helpers.Interactions;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommon.Enums;
using JustyBase.PluginCommon.Models;
using JustyBase.PluginCommons;
using JustyBase.PluginDatabaseBase.Database;
using JustyBase.Shared.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Documents;

public sealed partial class ImportViewModel
{
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly IMessageForUserTools _messageForUserTools;

    public ObservableCollection<ColumnInGrid> ColumnsInGrid { get; set; } = [];
    public ObservableCollection<string[]> PreviewRows { get; set; } = [];

    private readonly Dictionary<string, ImportFromExcelFile> _importFromExcelFilesClasses = [];
    public ICommand OpenFileForImportCommand { get; set; }
    public ObservableCollection<TabItem> ExcelTabsNames { get; set; } = [];
    public ObservableCollection<ConnectionItem> ConnectionsList => SqlDocumentViewModelHelper.ConnectionsList;

    [ObservableProperty]
    public partial TabItem SelectedTab { get; set; }

    private readonly string _createNewTxt = "[CREATE NEW TABLE]";

    [ObservableProperty]
    public partial bool AllColumnsAsText { get; set; }

    private readonly ConcurrentBag<ImportItem> _importsInProgress = [];

    [ObservableProperty]
    public partial string TabsWarningMessage { get; set; }

    [ObservableProperty]
    public partial bool StartEnabled { get; set; }

    [ObservableProperty]
    public partial string ImportFilepath { get; set; }
    [ObservableProperty]
    public partial bool ContinueEnabled { get; set; }

    public object SelectedConnection
    {
        get;
        set
        {
            SetProperty(ref field, value);
            DatabaseItems.Clear();
            foreach (var item in DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionTyped.Name).GetDatabases(""))
            {
                DatabaseItems.Add(item);
            }
        }
    }

    public ObservableCollection<string> DatabaseItems { get; set; }
    public ConnectionItem SelectedConnectionTyped => (SelectedConnection as ConnectionItem);

    public string SelectedDatabase
    {
        get;
        set
        {
            SetProperty(ref field, value);
            SchemaItems.Clear();
            foreach (var item in DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionTyped.Name).GetSchemas(SelectedDatabase, ""))
            {
                SchemaItems.Add(item);
            }
        }
    }

    public ObservableCollection<string> SchemaItems { get; set; }

    public string SelectedSchema
    {
        get;
        set
        {
            SetProperty(ref field, value);
            TableItems.Clear();
            TableItems.Add(_createNewTxt);
            foreach (var item in DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionTyped.Name).GetDbObjects(SelectedDatabase, SelectedSchema, "", TypeInDatabaseEnum.Table))
            {
                TableItems.Add(item.Name);
            }
        }
    }
    public ObservableCollection<string> TableItems { get; set; }

    public ObservableCollection<ImportItem> ImportItemCollections { get; set; }

    [ObservableProperty]
    public partial string SelectedTableText { get; set; }

    private async Task OpenMethod(string filePath)
    {
        ImportFilepath = filePath;

        var curentImportFromFile = new ImportFromExcelFile(x => MessageForUserTools.ShowSimpleMessageBox(x), _generalApplicationData.GlobalLoggerObject)
        {
            FilePath = filePath,
            TreatAllColumnsAsText = this.AllColumnsAsText
        };
        _importFromExcelFilesClasses[filePath] = curentImportFromFile;

        if (!string.IsNullOrWhiteSpace(curentImportFromFile.FilePath))
        {
            var initSuccess = await Task.Run(() =>
            {
                if (!curentImportFromFile.InitImport(encoding: Encoding.UTF8))
                {
                    curentImportFromFile.DoFileDispose();
                    return false;
                }
                return true;
            });
            if (!initSuccess)
            {
                return;
            }

            ExcelTabsNames.Clear();
            for (int i = 0; i < curentImportFromFile.SheetNamesToImport.Count; i++)
            {
                string item = curentImportFromFile.SheetNamesToImport[i];
                ExcelTabsNames.Add(new TabItem() { TabName = item, TabOk = (i == 0) });
                SelectedTab = ExcelTabsNames[0];
            }
            TabsWarningMessage = "";
        }
    }

    [ObservableProperty]
    public partial int SelIndexOpt { get; set; }

    public Action<string[]> ActionFromView;
    private readonly Lock _lock = new();

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ImportStart(string option)
    {
        if (option == "Continue")
        {
            ContinueEnabled = false;
            return;
        }

        string importFilePath = ImportFilepath;
        if (File.Exists(importFilePath))
        {
            if (!_importFromExcelFilesClasses.ContainsKey(importFilePath))
            {
                await OpenMethod(importFilePath);
            }
            var curentImportFromFile = _importFromExcelFilesClasses[importFilePath];
            //StartEnabled = false;

            if (SelectedTableText == _createNewTxt || string.IsNullOrWhiteSpace(SelectedTableText))
            {
                string nme = StringExtension.RandomSuffix("IMPORTED_");
                if (TableItems.Count >= 2)
                {
                    TableItems.Insert(1, nme);
                }
                else
                {
                    TableItems.Add(nme);
                }

                SelectedTableText = nme;
            }

            var importItem = new ImportItem(_messageForUserTools)
            {
                SourcePath = Path.GetDirectoryName(importFilePath),
                SourceName = Path.GetFileName(importFilePath),
                StartTime = DateTime.Now,
                Elapsed = "",
                Estimated = " - ",
                Info = "started",
                Connection = SelectedConnectionTyped?.Name,
                Destination = $"{SelectedDatabase}.{SelectedSchema}.{SelectedTableText}"
            };
            lock (_lock)
            {
                _importsInProgress.Add(importItem);
            }

            ImportItemCollections.Insert(0, importItem);
#if AVALONIA
            ImportItems.Refresh();
#endif
            await Task.Delay(20);
            importItem.Bck = "Yellow";
            IDatabaseService service = DatabaseServiceHelpers.GetDatabaseService(_generalApplicationData, SelectedConnectionTyped.Name);

            if (!_dispatcherTimer.IsEnabled)
            {
                _dispatcherTimer.Start();
            }
            if (curentImportFromFile is null)
            {
                _messageForUserTools.ShowSimpleMessageBoxInstance("_currentImport is null");
                return;
            }

            curentImportFromFile.StandardMessageAction = (s) => MessageForUserTools.DispatcherAction(
                () =>
                {
                    importItem.Info = s;
                }
            );

            List<string> excelNames = [];
            foreach (var item in ExcelTabsNames)
            {
                if (item.TabOk)
                {
                    excelNames.Add(item.TabName);
                }
            }

            for (int i = 0; i < curentImportFromFile.SheetNamesToImport.Count; i++)
            {
                var item = curentImportFromFile.SheetNamesToImport[i];
                if (!excelNames.Contains(item))
                {
                    curentImportFromFile.SheetNamesToImport.Remove(item);
                }
            }

            string tableNameMask = SelectedTableText;
            var sheets = curentImportFromFile.SheetNamesToImport;

            try
            {
                if (option == "Fast")
                {
                    var fastImportTask = curentImportFromFile.ImportFromFileAllSteps(service.DatabaseType, service, SelectedSchema, tableNameMask);
                    _importFromExcelFilesClasses.Remove(importFilePath);
                    StartEnabled = true;
                    await fastImportTask;
                }
                else if (option == "WithSteps")
                {
                    ColumnsInGrid.Clear();
                    var importTaskWithSteps = curentImportFromFile.ImportFromFileStepByStep(service.DatabaseType, service, SelectedSchema, tableNameMask,
                        (x, y) =>
                        {
                            ColumnsInGrid.Add(new ColumnInGrid()
                            {
                                ColumnName = x,
                                DetectedType = y,
                                DoForceText = false,
                            });
                            OnPropertyChanged(nameof(ColumnsInGrid));
                        }
                            , x =>
                            {
                                x.ForEach(x => PreviewRows.Add(x));
                                _messageForUserTools.DispatcherActionInstance(() => ActionFromView(ColumnsInGrid.Select(o => o.ColumnName).ToArray()));
                            }
                        );
                    _importFromExcelFilesClasses.Remove(importFilePath);
                    await foreach (var item in importTaskWithSteps)
                    {
                        ContinueEnabled = true;
                        StartEnabled = false;
                        var func = item?.Func;
                        var imp = item?.ImportJob;

                        SelIndexOpt = 1;
                        while (ContinueEnabled)
                        {
                            await Task.Delay(50);
                        }
                        for (int l = 0; l < ColumnsInGrid.Count; l++)
                        {
                            if (ColumnsInGrid[l].DoForceText)
                            {
                                imp.ColumnTypesBestMatch[l] = new DbTypeWithSize(DbSimpleType.Nvarchar) { TextLength = DatabaseTypeChooser.DEFAULT_NVARCHAR_LENGTH };
                            }
                        }
                        await func?.Invoke();
                        SelIndexOpt = 0;
                    }
                    StartEnabled = true;
                }
                else
                {
                    _messageForUserTools.ShowSimpleMessageBoxInstance("wrong option");
                }

                if (sheets.Count > 1)
                {
                    StringBuilder sb = new();
                    foreach (var item in sheets)
                    {
                        sb.AppendLine($"{SelectedDatabase}.{SelectedSchema}.{tableNameMask}_{item}");
                    }

                    TabsWarningMessage = sb.ToString();
                }
                else
                {
                    TabsWarningMessage = $"{SelectedDatabase}.{SelectedSchema}.{tableNameMask}";
                }

                importItem.Info = "completed !";
                importItem.Elapsed = (DateTime.Now - importItem.StartTime).ToString(@"hh\:mm\:ss");
                importItem.Bck = "LightGreen";
#if AVALONIA
                ImportItems.Refresh();
#endif
                lock (_lock)
                {
                    _importsInProgress.TryTake(out importItem);
                    _dispatcherTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                TabsWarningMessage = ex.Message;
                _generalApplicationData.GlobalLoggerObject.TrackError(ex, isCrash: false);
                curentImportFromFile?.DoFileDispose();
                _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                return;
            }
        }
    }
}

public sealed partial class ImportItem : ObservableObject
{
    [ObservableProperty]
    public partial string Info { get; set; }
    public string SourceName { get; set; }
    public string SourcePath { get; set; }
    public string Connection { get; set; }
    public string Destination { get; set; }
    public DateTime StartTime { get; set; }

    [ObservableProperty]
    public partial string Elapsed { get; set; }

    [ObservableProperty]
    public partial string Estimated { get; set; }

    [ObservableProperty]
    public partial string Bck { get; set; }
    public ICommand StopCommand { get; set; }
    public ImportItem(IMessageForUserTools messageForUserTools)
    {
        StopCommand = new RelayCommand(() =>
        {
            messageForUserTools.ShowSimpleMessageBoxInstance($"to do {Info} {StartTime}");
        });
        Bck = "Transparent";
    }
}

public sealed partial class TabItem : ObservableObject
{
    [ObservableProperty]
    public partial string TabName { get; set; }

    [ObservableProperty]
    public partial bool TabOk { get; set; }
}

public sealed partial class ColumnInGrid : ObservableObject
{
    [ObservableProperty]
    public partial string ColumnName { get; set; }

    [ObservableProperty]
    public partial string DetectedType { get; set; }

    public bool DoForceText
    {
        get;
        set
        {
            if (value)
            {
                DetectedType = $"NVARCHAR({DatabaseTypeChooser.DEFAULT_NVARCHAR_LENGTH})";
            }
            SetProperty(ref field, value);
        }
    }
}
