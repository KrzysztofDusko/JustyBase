using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using JustyBase.Common.Services;
using JustyBase.Helpers;
using JustyBase.Helpers.Interactions;
using JustyBase.Models;
using JustyBase.PluginCommon.Contracts;
using JustyBase.PluginCommons;
using JustyBase.Services;
using JustyBase.ViewModels.Documents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustyBase.ViewModels.Tools;

public sealed partial class SqlResultsViewModel : Tool, ICleanableViewModel
{

    [RelayCommand]
    private void GridDoubleClick(GridDoubleClickArg gridDoubleClickArg)
    {
        (this?.Factory as DockFactory)?.InsertTextToActiveDocument(gridDoubleClickArg.Data, gridDoubleClickArg.RawMode);
    }

    [RelayCommand]
    private void GridSelectionChanged(string text)
    {
        (Factory as DockFactory)?.SelectedDataGridAction?.Invoke(text);
    }

    private readonly ResultHelper _resultHelperService;

    [ObservableProperty]
    public partial bool VisibleExpand { get; set; } = false;

    [ObservableProperty]
    public partial string DpWidth { get; set; } = "10";

    partial void OnDpWidthChanged(string value)
    {
        if (int.TryParse(value, out int refWidth))
        {
            if (refWidth <= 30)
            {
                VisibleExpand = false;
            }
            else
            {
                VisibleExpand = true;
            }
        }
    }

    public string RelatedSqlDocumentId { get; set; }

    [ObservableProperty]
    public partial TableOfSqlResults CurrentResultsTable { get; set; }
    public ObservableCollection<RowDetail> RowDetailCollection { get; set; }

    private readonly IAvaloniaSpecificHelpers _avaloniaSpecificHelpers;
    private readonly IClipboardService _clipboardService;
    private readonly IGeneralApplicationData _generalApplicationData;
    private readonly IMessageForUserTools _messageForUserTools;
    private readonly ISimpleLogger _simpleLogger;

    public IClipboardService Clipboard => _clipboardService;
    public SqlResultsViewModel(IFactory factory, IAvaloniaSpecificHelpers avaloniaSpecificHelpers, IClipboardService clipboardService,
        IGeneralApplicationData generalApplicationData, IMessageForUserTools messageForUserTools,
        ISimpleLogger simpleLogger
        )
    {
        factory ??= App.GetRequiredService<IFactory>();
        Factory = factory;
        _avaloniaSpecificHelpers = avaloniaSpecificHelpers;
        _clipboardService = clipboardService;
        _generalApplicationData = generalApplicationData;
        _messageForUserTools = messageForUserTools;
        _simpleLogger = simpleLogger;
        _resultHelperService = new(generalApplicationData, _messageForUserTools, _simpleLogger);

        DpWidth = "10";
        RowDetailCollection = [];

        CurrentResultsTable = new TableOfSqlResults();
        GridCollectionView = new DataGridCollectionView(CurrentResultsTable.FilteredRows, isDataSorted: true, isDataInGroupOrder: true);
    }

    [RelayCommand]
    private void ExpandCollapseRowView()
    {
        VisibleExpand = !VisibleExpand;
        if (VisibleExpand)
        {
            DpWidth = "200";
        }
        else
        {
            DpWidth = "10";
        }
    }

    [RelayCommand]
    private void ScreenShot()
    {
        _messageForUserTools.ScreenShot();
    }

    public DataGridCollectionView GridCollectionView { get; set; }

    public string SQL { get; set; }


    [RelayCommand]
    private async Task ExportAllResults()
    {
        var ask = new JustyBase.Views.OtherDialogs.AskForFileName();
        await ask.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
        string randomName = ask.ReturnedName;

        var filePathToExport = Path.Combine(IGeneralApplicationData.DataDirectory, $"{randomName}{_resultHelperService.DefaultExcelExtension}");
        List<(DbDataReader, string)> listOfResults = [];

        if (!_generalApplicationData.TryGetDocumentById(this.RelatedSqlDocumentId, out var docRes))
        {
            MessageForUserTools.ShowSimpleMessageBox("ExportAllResults - error", "Warning");
            return;
        }

        List<SqlResultsViewModel> results = (this.Factory as DockFactory).GetDocumentResults(docRes.HotDocumentViewModelAsT<SqlDocumentViewModel>());
        if (results is null || results.Count == 0)
        {
            return;
        }
        foreach (var item in results)
        {
            listOfResults.Add((new DBReaderWithMessagesTable(item.CurrentResultsTable, null), item.SQL));
        }

        if (listOfResults.Count > 0)
        {
            await _resultHelperService.CreateXlsbOrXlsxFile(filePathToExport, listOfResults);
            try
            {
                await _avaloniaSpecificHelpers.CopyFileToClipboard(filePathToExport);
            }
            catch (Exception ex)
            {
                _simpleLogger.TrackError(ex, isCrash: true);
            }
        }
    }

    private bool _doCollapseInNextCollapseAction = true;

    [RelayCommand]
    private void CollapseAll(object o)
    {
        if (o is DataGrid dg && GridCollectionView.Groups is not null)
        {
            Stopwatch st = Stopwatch.StartNew();
            //var d = dataGridCollectionView.GroupDescriptions[0];
            foreach (var item in GridCollectionView.Groups.OfType<DataGridCollectionViewGroup>()/*(DataGrid.DataContext as Avalonia.Collections.DataGridCollectionView).Groups*/)
            {
                if (st.ElapsedMilliseconds > 2_000)
                {
                    MessageForUserTools.ShowSimpleMessageBox("operation takes too long");
                    break;
                }
                if (_doCollapseInNextCollapseAction)
                {
                    try
                    {
                        dg.ScrollIntoView(CurrentResultsTable.Rows.FirstOrDefault(), null);
                        dg.CollapseRowGroup(item, true);
                    }
                    catch (Exception ex)
                    {
                        _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                    }
                }
                else
                {
                    try
                    {
                        dg.ExpandRowGroup(item, true);
                    }
                    catch (Exception ex)
                    {
                        _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                    }
                }
            }
            _doCollapseInNextCollapseAction = !_doCollapseInNextCollapseAction;
        }
    }

    [RelayCommand]
    private async Task ActionFromButton(string whatAction)
    {
        bool canceled = false;
        if (whatAction == "CopyAsCsvClipboard|button"
            || whatAction == "CopyAsCsvClipboard|menu"
            || whatAction == "CopyAsCsvClipboardHeaders|menu"
            || whatAction == "CopyRowValues|menu"
            || whatAction == "CopyAsExcelFileClipboard|button"
            || whatAction == "CopyAsExcelFileClipboard|menu"
            || whatAction == "OpenAsExcelFileClipboard|button"
            || whatAction == "OpenAsExcelFileClipboard|menu"
            || whatAction == "SaveAsExcelFile|button"
            || whatAction == "SaveAsExcelFile|menu"
            || whatAction == "CopyAsHtml|button"
            || whatAction == "CopySelectecCellsCurrentColumn|menu"
            || whatAction == "CopySelectecCellsCurrentColumn2|menu"
            )
        {
            var rdr = new DBReaderWithMessagesTable(CurrentResultsTable, null);
            if (CurrentResultsTable.TypeCodes is null)
            {
                ShowFlyoutCommand?.Execute("ERROR");
                return;
            }

            string randomName = StringExtension.RandomSuffix();

            string filePathToExport = Path.Combine(IGeneralApplicationData.DataDirectory, $"{randomName}{_resultHelperService.DefaultExcelExtension}");
            if (whatAction.StartsWith("CopyAsCsvClipboard"))
            {
                using StringWriter stringWriter = new StringWriter();

                bool headers = false;
                if (whatAction == "CopyAsCsvClipboardHeaders|menu")
                {
                    headers = true;
                }
                try
                {
                    _resultHelperService.CreateCsvFile(stringWriter, rdr, headers);
                }
                catch (Exception ex)
                {
                    _messageForUserTools.ShowSimpleMessageBoxInstance(ex);
                }

                await _clipboardService.SetTextAsync(stringWriter.ToString());
            }
            else if (
                whatAction.StartsWith("CopyAsExcelFileClipboard")
                || whatAction.StartsWith("OpenAsExcelFileClipboard")
                || whatAction.StartsWith("SaveAsExcelFile"))
            {
                if (whatAction.StartsWith("CopyAsExcelFileClipboard"))
                {
                    var fileNameWindow = new JustyBase.Views.OtherDialogs.AskForFileName
                    {
                        ShowInTaskbar = false
                    };
                    await fileNameWindow.ShowDialog(_avaloniaSpecificHelpers.GetMainWindow());
                    randomName = fileNameWindow.ReturnedName;
                    filePathToExport = Path.Combine(IGeneralApplicationData.DataDirectory, $"{randomName}{_resultHelperService.DefaultExcelExtension}");
                    if (String.IsNullOrWhiteSpace(randomName))
                    {
                        canceled = true;
                    }
                }
                else if (whatAction.StartsWith("SaveAsExcelFile"))
                {
                    var saveFile = await _avaloniaSpecificHelpers.GetStorageProvider().SaveFilePickerAsync(
                        new FilePickerSaveOptions()
                        {
                            FileTypeChoices =
                            [
                                new("excel file") { Patterns = [".xlsb"] },
                                new("excel file") { Patterns = [".xlsx"] },
                                new("csv file") { Patterns = [".csv"] },
                                new("zstd csv file") { Patterns = [".csv.zst"] },
                                new("parquet file") { Patterns = [".parquet"] },
                                new("zipped csv file") { Patterns = [".csv.zip"] },
                                new("brotli csv file") { Patterns = [".csv.br"] },
                                new("gzip csv file") { Patterns = [".csv.gz"] },
                            ],
                            DefaultExtension = ".xlsb",
                            ShowOverwritePrompt = true
                        }
                    );

                    if (saveFile is null)
                    {
                        return;
                    }
                    filePathToExport = saveFile.Path.LocalPath;
                }

                if (string.IsNullOrWhiteSpace(filePathToExport))
                {
                    return;
                }

                if (!canceled)
                {
                    await _resultHelperService.CreateExcelFileAsync(filePathToExport, rdr, SQL);

                    if (whatAction.StartsWith("CopyAsExcelFileClipboard"))
                    {
                        try
                        {
                            await _avaloniaSpecificHelpers.CopyFileToClipboard(filePathToExport);
                        }
                        catch (Exception ex)
                        {
                            _simpleLogger.TrackError(ex, isCrash: false);
                        }
                    }
                    else if (whatAction.StartsWith("OpenAsExcelFileClipboard"))
                    {
                        _messageForUserTools.OpenInExplorerHelper(filePathToExport.Replace("/", "\\").Replace("\\\\", "\\"));
                    }
                }
            }
            else if (whatAction == "CopyAsHtml|button")
            {
                await _avaloniaSpecificHelpers.GetClipboard().SetDataObjectAsync(new CopyHtmlOrTextClipboard(CurrentResultsTable));
            }
            else if (whatAction == "CopySelectecCellsCurrentColumn|menu")
            {
                StringBuilder sb = new();
                foreach (var item in SelectedColumnCells)
                {
                    sb.AppendLine(item?.ToString());
                }
                await App.GetRequiredService<IClipboardService>().SetTextAsync(sb.ToString());
            }
            else if (whatAction == "CopySelectecCellsCurrentColumn2|menu")
            {
                StringBuilder sb = new();

                if (PrevCols.TryDequeue(out int prev1) && PrevCols.TryDequeue(out int prev2))
                {
                    for (int i = Math.Min(prev1, prev2); i <= Math.Max(prev1, prev2); i++)
                    {
                        sb.Append(CurrentResultsTable.Headers[i]);
                        if (i <= Math.Max(prev1, prev2))
                        {
                            sb.Append('\t');
                        }
                    }
                    sb.AppendLine();
                    foreach (var row in SelectedItems.OfType<TableRow>())
                    {
                        object[] fileds = row?.Fields;
                        for (int i = Math.Min(prev1, prev2); i <= Math.Max(prev1, prev2); i++)
                        {
                            object o = fileds[i];
                            sb.Append(o);
                            if (i < Math.Max(prev1, prev2))
                            {
                                sb.Append('\t');
                            }
                        }
                        sb.AppendLine();
                    }

                    await _clipboardService.SetTextAsync(sb.ToString());
                }
            }
            else if (whatAction.StartsWith("CopyRowValues"))
            {
                IList selectedRows = SelectedItems;
                if (selectedRows.Count == 1)
                {
                    StringBuilder sb = new();

                    sb.Append("VALUES (");
                    var row = (selectedRows[0] as TableRow);
                    object[] fileds = row?.Fields;
                    for (int i = 0; i < fileds.Length; i++)
                    {
                        object o = fileds[i];
                        var item = StringExtension.ConvertAsSqlCompatybile(o);
                        sb.Append(item);
                        if (i < fileds.Length - 1)
                        {
                            sb.Append(',');
                        }
                    }
                    sb.Append(')');
                    await _clipboardService.SetTextAsync(sb.ToString());
                }
            }
        }

        if (canceled)
        {
            return;
        }


        ShowFlyoutCommand?.Execute(whatAction);
    }
    public ICommand ShowFlyoutCommand { get; set; } // !!! Mode=OneWayToSource
    public ICommand ChangeColumVisiblityCommand { get; set; } // !!! Mode=OneWayToSource


    [ObservableProperty]
    public partial string ExportMessage { get; set; }

    [ObservableProperty]
    public partial string RowsLoadingMessage { get; set; }

    [ObservableProperty]
    public partial IList SelectedItems { get; set; }

    private int _selInd = 0;
    public int SelInd
    {
        get => _selInd;
        set
        {
            if (value >= 0 && value < CurrentResultsTable.FilteredRows.Count && SelectedItems is not null)
            {
                RowDetailCollection.Clear();
                int? selectedCount = -1;
                for (int i = 0; i < CurrentResultsTable.Headers.Count; i++)
                {
                    var headerName = CurrentResultsTable.Headers[i];
                    var tpe = CurrentResultsTable.DataTypeNames[i];
                    var rd = new RowDetail()
                    {
                        Name = headerName, /*ColumnValue = val,*/
                        TypeName = tpe,
                        ChangeColVisiblity = () => ChangeColumVisiblityCommand?.Execute(headerName)
                    };

                    var selectedRows = SelectedItems;
                    selectedCount = selectedRows?.Count;
                    int? cntLimited = selectedCount > 10 ? 10 : selectedCount;
                    if (selectedCount >= 1 && cntLimited is int)
                    {
                        rd.FieldsValues = new List<string>((int)cntLimited);
                        for (int i1 = 0; i1 < cntLimited; i1++)
                        {
                            object item = selectedRows[i1];
                            rd.FieldsValues.Add((item as TableRow)?.Fields[i]?.ToString());
                        }
                    }
                    RowDetailCollection.Add(rd);
                }
            }


            SetProperty(ref _selInd, value);
        }
    }

    [ObservableProperty]
    public partial IEnumerable<object> SelectedColumnCells { get; set; } = null;

    [ObservableProperty]
    public partial string StatsText { get; set; }

    partial void OnStatsTextChanged(string value)
    {
        (Factory as DockFactory)?.SelectedDataGridAction?.Invoke(StatsText);
    }


    [ObservableProperty]
    public partial bool GridEnabled { get; set; } = true;
    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = "";
    [ObservableProperty]
    public partial bool GridVisible { get; set; } = false;
    [ObservableProperty]
    public partial bool SearchInProgress { get; set; } = false;
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    partial void OnSearchTextChanged(string value)
    {
        TriggerSearchTimerCommand?.Execute(null);
    }

    public ICommand TriggerSearchTimerCommand { get; set; } // 

    public void DoCleanup()
    {
        if (CurrentResultsTable is not null)
        {
            long n = CurrentResultsTable.FilteredRows.Count * CurrentResultsTable.Headers.Count;
            CurrentResultsTable.DoClear();
            if (n > 5_000_000 && _generalApplicationData.Config.DoGcCollect)
            {
                GC.Collect();
                //GC.WaitForFullGCComplete();
                //GC.WaitForPendingFinalizers();
            }
        }
    }

    public bool ContainsGeneralSearch
    {
        get;
        set
        {
            SetProperty(ref field, value);
            TriggerSearchTimerCommand.Execute(null);
        }
    } = true;


    [ObservableProperty]
    public partial Dictionary<int, AditionalOneFilter> AdditionalValues { get; set; } = [];

    [ObservableProperty]
    public partial bool DataLoadingInProgress { get; set; } = false;
    [ObservableProperty]
    public partial bool IsResultVisible { get; set; } = true;

    public bool IsDocked
    {
        get;
        set
        {
            field = value;
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged();
            }
            if (IsDocked)
            {
                this.Title += " [DOCKED]";
            }
            else
            {
                this.Title = this.Title.Replace(" [DOCKED]", "");
            }
            OnPropertyChanged(nameof(this.Title));

        }
    } = false;


    private const int INITIAL_ROWS_LIMIT = 50;
    public void LoadData((IDatabaseService dbService, DbDataReader rdr, string errorMessage) res)
    {
        if (!string.IsNullOrWhiteSpace(res.errorMessage))
        {
            _messageForUserTools.DispatcherActionInstance(() =>
            {
                ErrorMessage = res.errorMessage;
            });
            return;
        }

        var reader = res.rdr;
        if (reader == null || !reader.HasRows && reader.FieldCount <= 0)
        {
            return;
        }


        var headers = new List<string>(reader.FieldCount);
        var dtnames = new List<string>(reader.FieldCount);
        var typeCodes = new List<TypeCode>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; ++i)
        {
            headers.Add(reader.GetName(i));
            string typeName = reader.GetDataTypeName(i);
            typeName ??= reader.GetFieldType(i).Name;
            dtnames.Add(typeName);
            if (typeName == "int1")
            {
                typeCodes.Add(TypeCode.Byte);
            }
            else
            {
                typeCodes.Add(Type.GetTypeCode(reader.GetFieldType(i)));
            }
        }
        var st = reader.GetSchemaTable();

        if (st is not null && st.Columns.Contains("NumericScale"))
        {
            CurrentResultsTable.NumericScales = new byte[reader.FieldCount];
            int nm = 0;
            foreach (var item in st.Rows.OfType<DataRow>())
            {
                var scale = item["NumericScale"];
                if (scale is not null && scale != DBNull.Value)
                {
                    try
                    {
                        byte byteScale = (byte)Math.Clamp(Convert.ToInt32(scale), 0, 127);
                        CurrentResultsTable.NumericScales[nm] = (byteScale == 127) ? (byte)8 : byteScale;
                    }
                    catch (Exception)
                    {
                        CurrentResultsTable.NumericScales[nm] = 0;
                    }
                }
                nm++;
            }
        }
        for (int i = headers.Count - 1; i >= 0; i--)
        {
            var ch = headers[i];
            int cnt = headers.Count(o => o == ch);
            if (cnt > 1)
            {
                headers[i] = ch + $"_{cnt}";
            }
        }

        var rows = new List<TableRow>();
        CurrentResultsTable.Headers = headers;
        CurrentResultsTable.DataTypeNames = dtnames;
        CurrentResultsTable.TypeCodes = typeCodes;
        CurrentResultsTable.Rows = rows;

        _messageForUserTools.DispatcherActionInstance(() =>
        {
            GridVisible = false;
            //Table.FilteredRows.SupressNotification = true;
        });

        try
        {
            int a = 0;
            lock (_lock)
            {
                var drr = res.dbService.GetDatabaseRowReader(reader);
                while (a++ < INITIAL_ROWS_LIMIT && reader.Read())
                {
                    var row = new TableRow
                    {
                        Fields = drr.ReadOneRow(),
                    };
                    rows.Add(row);
                    CurrentResultsTable.FilteredRows.Add(row);
                }
            }
        }
        finally
        {
            _messageForUserTools.DispatcherActionInstance(/*async*/ async () =>
            {
                // await Task.Delay(10);
                //Table.FilteredRows.SupressNotification = false;
                try
                {
                    GridCollectionView.Refresh();
                }
                catch (Exception)
                {
                    await Task.Delay(10);
                    try
                    {
                        GridCollectionView.Refresh();
                    }
                    catch (Exception)
                    {
                    }
                }

                GridVisible = true;
                RowsLoadingMessage = $"{CurrentResultsTable.FilteredRows.Count:N0} rows";
            });
        }
    }

    private readonly Lock _lock = new();

    public /*async Task*/ void LoadRest(IDatabaseService? dbService, DbDataReader reader, int queryNum, ref int abortUbound, DbCommand command)
    {
        _messageForUserTools.DispatcherActionInstance(() =>
        {
            DataLoadingInProgress = true;
        });

        List<TableRow> rowsTemp = [];
        int i1 = CurrentResultsTable.Rows.Count;
        try
        {
            var drr = dbService.GetDatabaseRowReader(reader);
            long startTime = Stopwatch.GetTimestamp();

            while (reader.Read() && queryNum >= abortUbound)
            {
                var row = new TableRow
                {
                    Fields = drr.ReadOneRow(),
                };
                rowsTemp.Add(row);
                i1++;
                if (i1 == _generalApplicationData.Config.ResultRowsLimit)
                {
                    command.Cancel();
                    abortUbound = queryNum + 1;
                    //reader.Close();
                    break;
                }
                if (i1 % 10_000 == 0 && Stopwatch.GetElapsedTime(startTime).TotalSeconds >= 1)
                {
                    startTime = Stopwatch.GetTimestamp();
                    int localI = i1;
                    _messageForUserTools.DispatcherActionInstance(() =>
                    {
                        RowsLoadingMessage = $"{localI:N0} rows";
                    });
                }
                //Table.Rows.Add(row);
                //Table.FilteredRows.Add(row);
            }
            //Table.PopularValues.Clear();
            //foreach (var item in Table.Headers)
            //{
            //    Table.DoPopulatePopularValues(item);
            //}
        }
        finally
        {
            CurrentResultsTable.Rows.AddRange(rowsTemp);
            //Table.FilteredRows.SupressNotification = true;
            lock (_lock)
            {
                foreach (var item in rowsTemp)
                {
                    CurrentResultsTable.FilteredRows.Add(item);
                }
            }

            _messageForUserTools.DispatcherActionInstance(() =>
            {
                RowsLoadingMessage = $"{CurrentResultsTable.FilteredRows.Count:N0} rows";
                DataLoadingInProgress = false;

                try
                {
                    GridCollectionView.Refresh();
                }
                catch (Exception)
                {
                }
                GridEnabled = true;
                RowsLoadingMessage = $"{CurrentResultsTable.FilteredRows.Count:N0} rows";
            });
        }
    }

    [ObservableProperty]
    public partial Queue<int> PrevCols { get; set; } = new Queue<int>();

    [RelayCommand]
    private async Task Copy1(string text)
    {
        await Clipboard?.SetTextAsync(text);
    }
}

