using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using JustyBase.Converters;
using JustyBase.Helpers;
using JustyBase.Models;
using JustyBase.Models.Tools;
using JustyBase.PluginCommons;
using JustyBase.ViewModels;
using JustyBase.ViewModels.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JustyBase.Views.Tools;

public sealed partial class SqlResultsView : UserControl
{
    public SqlResultsView()
    {
        InitializeComponent();
        ResultDataGrid.Initialized += DataGrid_Initialized;
        ResultDataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.None;
        ResultDataGrid.KeyDown += DataGrid_KeyDown;
        ResultDataGrid.SelectionChanged += DataGrid_SelectionChanged;
        ResultDataGrid.CurrentCellChanged += DataGrid_CurrentCellChanged;
        Initialized += SqlResultsView_Initialized;
        rowDetailsDataGrid.Initialized += RowDetailsDataGrid_Initialized;

        if (columnAutoComplet is not null)
        {
            columnAutoComplet.GotFocus += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(columnAutoComplet.Text))
                {
                    columnAutoComplet.Text = " ";
                    columnAutoComplet.IsDropDownOpen = true;
                }
            };
            columnAutoComplet.ItemFilter = new AutoCompleteFilterPredicate<object>(
                (x, y) =>
                {
                    if (string.IsNullOrWhiteSpace(x))
                    {
                        return true;
                    }
                    else
                    {
                        return y.ToString().Contains(x.Trim(), StringComparison.OrdinalIgnoreCase);
                    }
                });
        }
        ResultDataGrid.LoadingRow += DataGrid_LoadingRow;
        rowDetailsDataGrid.DoubleTapped += DataGrid_DoubleTapped;
        TriggerSearchTimerCommand = new RelayCommand(TriggerSearchTimer);
        //DataGrid.RowHeaderWidth = 60;
    }

    private void SqlResultsView_Initialized(object? sender, EventArgs e)
    {
        _flyoutOnControls = new Dictionary<string, (Control, string)>()
        {
            { "CopyAsCsvClipboard|button", (copyClipboardBt, "Copied")},
            { "CopyAsExcelFileClipboard|button", (copyXlsToClipboard, "Copied")},
            { "OpenAsExcelFileClipboard|button",(openAsXlsx, "Opening started") },
            { "SaveAsExcelFile|button",(openAsXlsx, "Saved") },
            { "CopyAsHtml|button",(copyAsHtml, "Copied") },
            { "ERROR", (this, "Error") }
        };

        ShowFlyoutCommand = new RelayCommand<string>((x) =>
        {
            if (_flyoutOnControls.TryGetValue(x, out var flyout))
            {
                ShowCopiedFlyout(flyout.Item1, flyout.Item2);
            }
        });


        ChangeColumVisiblityCommand = new RelayCommand<string>((colname) =>
        {
            foreach (var item in ResultDataGrid.Columns)
            {
                if (item.Header.ToString() == colname)
                {
                    App.GetRequiredService<IMessageForUserTools>().DispatcherActionInstance(() => item.IsVisible = !item.IsVisible);
                    return;
                }
            }
        });
    }

    private Dictionary<string, (Control, string)> _flyoutOnControls;

    public static readonly StyledProperty<ICommand> ShowFlyoutCommandProperty =
    AvaloniaProperty.Register<SqlResultsView, ICommand>(nameof(ShowFlyoutCommand));

    public ICommand ShowFlyoutCommand
    {
        get => GetValue(ShowFlyoutCommandProperty);
        set => SetValue(ShowFlyoutCommandProperty, value);
    }


    public static readonly StyledProperty<ICommand> Copy1CommandProperty =
AvaloniaProperty.Register<SqlResultsView, ICommand>(nameof(Copy1Command));

    public ICommand Copy1Command
    {
        get => GetValue(Copy1CommandProperty);
        set => SetValue(Copy1CommandProperty, value);
    }


    public static readonly StyledProperty<TableOfSqlResults> CurrentResultsTableProperty =
AvaloniaProperty.Register<SqlResultsView, TableOfSqlResults>(nameof(CurrentResultsTable));

    public TableOfSqlResults CurrentResultsTable
    {
        get => GetValue(CurrentResultsTableProperty);
        set => SetValue(CurrentResultsTableProperty, value);
    }


    public static readonly StyledProperty<ICommand> ChangeColumVisiblityCommandProperty =
    AvaloniaProperty.Register<SqlResultsView, ICommand>(nameof(ChangeColumVisiblityCommand));

    public ICommand ChangeColumVisiblityCommand
    {
        get => GetValue(ChangeColumVisiblityCommandProperty);
        set => SetValue(ChangeColumVisiblityCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> TriggerSearchTimerCommandProperty =
AvaloniaProperty.Register<SqlResultsView, ICommand>(nameof(TriggerSearchTimerCommand));

    public ICommand TriggerSearchTimerCommand
    {
        get => GetValue(TriggerSearchTimerCommandProperty);
        set => SetValue(TriggerSearchTimerCommandProperty, value);
    }


    public static readonly StyledProperty<ICommand> GridDoubleClickCommandProperty =
    AvaloniaProperty.Register<SqlResultsView, ICommand>(nameof(GridDoubleClickCommand));

    public ICommand GridDoubleClickCommand
    {
        get => GetValue(GridDoubleClickCommandProperty);
        set => SetValue(GridDoubleClickCommandProperty, value);
    }

    private void DataGridDoubleClicked(object data, bool rawMode)
    {
        GridDoubleClickCommand?.Execute(new GridDoubleClickArg(data, rawMode));
    }


    public static readonly StyledProperty<ICommand> GridSelectionChangedCommandProperty =
AvaloniaProperty.Register<SqlResultsView, ICommand>(nameof(GridSelectionChangedCommand));

    public ICommand GridSelectionChangedCommand
    {
        get => GetValue(GridSelectionChangedCommandProperty);
        set => SetValue(GridSelectionChangedCommandProperty, value);
    }


    public static readonly StyledProperty<IEnumerable<object>> SelectedColumnCellsProperty =
AvaloniaProperty.Register<SqlResultsView, IEnumerable<object>>(nameof(SelectedColumnCells));

    public IEnumerable<object> SelectedColumnCells
    {
        get => GetValue(SelectedColumnCellsProperty);
        set => SetValue(SelectedColumnCellsProperty, value);
    }

    public static readonly DirectProperty<SqlResultsView, IList> SelectedItemsProperty =
AvaloniaProperty.RegisterDirect<SqlResultsView, IList>(nameof(SelectedItems), x => x.SelectedItems, defaultBindingMode: BindingMode.OneWayToSource);

    public IList SelectedItems
    {
        get => ResultDataGrid.SelectedItems;
    }


    public static readonly DirectProperty<SqlResultsView, Dictionary<int, AditionalOneFilter>> AdditionalValuesProperty =
        AvaloniaProperty.RegisterDirect<SqlResultsView, Dictionary<int, AditionalOneFilter>>(nameof(AdditionalValues),
    x => x.AdditionalValues,
    (o, v) => o.AdditionalValues = v, defaultBindingMode: BindingMode.OneTime);


    private Dictionary<int, AditionalOneFilter> _additionalValues = [];
    public Dictionary<int, AditionalOneFilter> AdditionalValues
    {
        get => _additionalValues;
        set => SetAndRaise(AdditionalValuesProperty, ref _additionalValues, value);
    }


    public static readonly StyledProperty<Queue<int>> PrevColsProperty =
    AvaloniaProperty.Register<SqlResultsView, Queue<int>>(nameof(PrevCols));

    public Queue<int> PrevCols
    {
        get => GetValue(PrevColsProperty);
        set => SetValue(PrevColsProperty, value);
    }


    public static readonly StyledProperty<string> StatsTextProperty =
AvaloniaProperty.Register<SqlResultsView, string>(nameof(StatsText));

    public string StatsText
    {
        get => GetValue(StatsTextProperty);
        set => SetValue(StatsTextProperty, value);
    }


    private long _lastStatsGeneratedTime = Stopwatch.GetTimestamp();
    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        //to avoid DataGrid_CurrentCellChanged + DataGrid_SelectionChanged combo
        if (Stopwatch.GetElapsedTime(_lastStatsGeneratedTime) > TimeSpan.FromMilliseconds(1))
        {
            GridSelectionChangedCommand?.Execute($"Selected {ResultDataGrid.SelectedItems.Count:N0} rows");
        }

        var selectedCount = SelectedItems?.Count;
        if (_prevSelectedCount != selectedCount)
        {
            _prevSelectedCount = SelectedItems.Count;
            RefreshRowView();
        }
    }
    private int _prevSelectedCount = -1;

    private Flyout _copiedNoticeFlyout;

    private void ShowCopiedFlyout(Control host, string message = "Copied!", bool fail = false)
    {
        if (_copiedNoticeFlyout == null)
        {
            _copiedNoticeFlyout = new Flyout
            {
                Content = new TextBlock
                {
                    Text = message,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                }
            };
        }
        else
        {
            var tb = _copiedNoticeFlyout.Content as TextBlock;
            tb.Text = message;
        }

        if (fail)
        {
            _copiedNoticeFlyout.FlyoutPresenterClasses.Add("Fail");
        }
        else
        {
            _copiedNoticeFlyout.FlyoutPresenterClasses.Remove("Fail");
        }

        _copiedNoticeFlyout.ShowAt(host);


        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await Task.Delay(2000);
                _copiedNoticeFlyout.Hide();
            }
            catch (Exception)
            {
            }
        }, priority: DispatcherPriority.Background);
    }

    private void DataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.Header = new TextBlock()
        {
            Text = (e.Row.Index + 1).ToString("N0"),
            Margin = new Thickness(5, 0, 5, 0),
            FontSize = 12
        };
    }

    private void DataGrid_CurrentCellChanged(object? sender, EventArgs e)
    {
        if (ResultDataGrid.CurrentColumn is null)
        {
            return;
        }
        _statsTime ??= new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Default,
            (o, e) =>
            {
                (o as DispatcherTimer).Stop();
                DoStats();
            });

        int index = CurrentResultsTable.Headers.IndexOf(ResultDataGrid.CurrentColumn.Header.ToString());
        if (ResultDataGrid.SelectedItem is TableRow tableRow && index >= 0 && index < tableRow.Fields.Length)
        {
            if (ResultDataGrid.SelectedItems.Count > 20_000)
            {
                _statsTime.Start();
            }
            else
            {
                DoStats();
            }
        }
    }

    private DispatcherTimer _statsTime;
    private void DoStats()
    {
        _statsTime.Stop();
        var columenHeader = ResultDataGrid?.CurrentColumn?.Header?.ToString();
        if (columenHeader is null)
        {
            return;
        }
        int index = CurrentResultsTable.Headers.IndexOf(columenHeader);
        int lastIndex = CurrentResultsTable.Headers.IndexOf(columenHeader);
        if (index != lastIndex)
        {
            return;
        }

        TableRowStats stats = new (CurrentResultsTable, ResultDataGrid.SelectedItems.OfType<TableRow>(), index);
        SelectedColumnCells = TableRowStats.CurrentColumnCells(ResultDataGrid.SelectedItems.OfType<TableRow>(), index);
        StatsText = $"Selected {ResultDataGrid.SelectedItems.Count:N0} rows | Sum {stats.Sum:N3} | Count {stats.NotNullCnt:N0} | Distinct {stats.DistinctCnt:N0} | Min {stats.MinOfColumn:N3} | Max {stats.MaxOfColumn:N3}";
        _lastStatsGeneratedTime = Stopwatch.GetTimestamp();
    }

    private void RowDetailsDataGrid_Initialized(object? sender, EventArgs e)
    {
        if (CurrentResultsTable is null)
            return;
        RefreshRowView();
    }
    private void RefreshRowView()
    {
        while (rowDetailsDataGrid.Columns.Count > 3)
        {
            rowDetailsDataGrid.Columns.RemoveAt(2);
        }

        if (ResultDataGrid.SelectedItems.Count >= 1)
        {
            var selectedCount = ResultDataGrid.SelectedItems.Count > 10 ? 10 : ResultDataGrid.SelectedItems.Count;
            for (int i = 0; i < selectedCount; i++)
            {
                int savedI = i;
                var valCol = new DataGridTextColumn()
                {
                    Header = $"Value {savedI + 1}",
                    MaxWidth = 600,
                    Width = DataGridLength.Auto,
                    Binding = new Binding($"{nameof(RowDetail.FieldsValues)}[{savedI}]", BindingMode.OneWay),
                    IsReadOnly = true,
                    CanUserSort = true,
                    CanUserResize = true
                };
                rowDetailsDataGrid.Columns.Insert(rowDetailsDataGrid.Columns.Count - 1, valCol);
            }
        }
    }


    private void DataGrid_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
        {
            var SelectedItems = ResultDataGrid.SelectedItems;
            StringBuilder sb = new ();

            if (SelectedItems.Count > 1)
            {
                for (int i = 0; i < ResultDataGrid.Columns.Count; i++)
                {
                    sb.Append(ResultDataGrid.Columns[i].Header);
                    if (i < ResultDataGrid.Columns.Count - 1)
                    {
                        sb.Append('\t');
                    }
                }

                sb.AppendLine();
                for (int index = 0; index < SelectedItems.Count; index++)
                {
                    if (SelectedItems[index] is TableRow tableRow)
                    {
                        sb.AppendLine(String.Join('\t', tableRow.Fields));
                    }
                }
            }
            else
            {
                if (ResultDataGrid.SelectedItem is TableRow tableRow)
                {
                    var header = ResultDataGrid.CurrentColumn.Header?.ToString();
                    if (header is not null)
                    {
                        int ind = this.CurrentResultsTable.Headers.IndexOf(header);
                        if (ind >= 0 && ind <= tableRow.Fields.Length)
                        {
                            //var obj = tableRow.Fields[ResultDataGrid.CurrentColumn.DisplayIndex];
                            var obj = tableRow.Fields[ind];
                            if (obj is string objStr)
                            {
                                sb.Append(objStr);
                            }
                            else
                            {
                                string res = StringExtension.ConvertAsSqlCompatybile(obj);
                                sb.Append(res);
                            }
                        }
                    }
                }
            }
            Copy1Command.Execute(sb.ToString());
            e.Handled = true;
            //InitializeComponent();
        }
        else if (e.Key == Avalonia.Input.Key.F5)
        {
            ResultDataGrid.CurrentColumn.Width = new DataGridLength(ResultDataGrid.CurrentColumn.ActualWidth);
        }
    }

    private DataGridCollectionView GridCollectionView => this.ResultDataGrid.ItemsSource as DataGridCollectionView;

    private const int SEARCH_DELAY_MS = 50;
    private DispatcherTimer _searchTimer;

    private void TriggerSearchTimer()
    {
        if (!SearchInProgress)
        {
            if (_searchTimer is null)
            {
                _searchTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(SEARCH_DELAY_MS)
                };
                _searchTimer.Tick += (_, _) => MakeSearch();
            }
            _searchTimer.Stop();
            _searchTimer.Start();
        }
    }

    private bool SearchInProgress => searchBox.GetValue(TextBox.IsReadOnlyProperty);//searchBox.IsReadOnly;
    private bool? ContainsGeneralSearch => generalSearchToggle.GetValue(ToggleSwitch.IsCheckedProperty);//generalSearchToggle.IsChecked;
    private string SearchText => searchBox.GetValue(TextBox.TextProperty);// searchBox.Text;
    private void MakeSearch()
    {
        _searchTimer.Stop();
        //TO DO deselect all rows - due to https://github.com/KrzysztofDusko/JustDataEvoProject/issues/101
        if (SearchInProgress || CurrentResultsTable is null || CurrentResultsTable.Rows is null || CurrentResultsTable.Rows.Count <= 0 || CurrentResultsTable.Headers.Count <= 0)
            return;
        searchBox.IsReadOnly = true;

        //Table.FilteredRows.SupressNotification = true;
        CurrentResultsTable.FilteredRows.Clear();

        SearchInRows sr = new(CurrentResultsTable, SearchText, AdditionalValues, ContainsGeneralSearch == true);
        sr.SearchAll();

        // https://github.com/KrzysztofDusko/JustDataEvoProject/issues/101
        if (SelectedItems.Count > 5_000)
        {
            SelectedItems.Clear();
        }
        CurrentResultsTable.SortFilteredRows();
        GridCollectionView.Refresh();
        //Table.FilteredRows.SupressNotification = false;

        //RowsLoadingMessage = $"{CurrentResultsTable.FilteredRows.Count:N0} rows";
        rowsLoadingMessage.Text = $"{CurrentResultsTable.FilteredRows.Count:N0} rows";
        RefreshLastColumnIndex?.Invoke();
        searchBox.IsReadOnly = false;
    }

    private void DataGrid_Initialized(object? sender, System.EventArgs e)
    {
        if (CurrentResultsTable is not null)
        {
            List<string> headersListCopy = new(CurrentResultsTable.Headers);

            headersListCopy.Sort();
            var columnAutoComplet = this.columnAutoComplet;
            columnAutoComplet.ItemsSource = headersListCopy;
            columnAutoComplet.SelectionChanged += (_, _) =>
            {
                var item = columnAutoComplet.SelectedItem;
                if (item is string stringItem)
                {
                    for (int i = 0; i < ResultDataGrid.Columns.Count; i++)
                    {
                        if ((string)(ResultDataGrid.Columns[i].Header) == stringItem)
                        {
                            ResultDataGrid.ScrollIntoView(null, ResultDataGrid.Columns[i]);
                            break;
                        }
                    }
                }
            };

            ValueConverters = [];
            for (var i = 0; i < CurrentResultsTable.Headers.Count; ++i)
            {
                DataGridBoundColumn col = GetColumnToResults(CurrentResultsTable, i);
                ResultDataGrid.Columns.Add(col);
            }
            ResultDataGrid.FrozenColumnCount = _pinnedColumns.Count;
            ResultDataGrid.DoubleTapped += DataGrid_DoubleTapped;
            ResultDataGrid.Tapped += ResultDataGrid_Tapped;
            ResultDataGrid.Sorting += ResultDataGrid_Sorting;
            ResultDataGrid.LoadingRowGroup += DataGrid_LoadingRowGroup;
        }
    }

    private readonly Dictionary<string, int> _pinnedColumns = [];

    private readonly Dictionary<int, CustomListBoxViewModel> _listBoxesDictionaryCache = [];
    private List<IValueConverter> ValueConverters = null;
    public CustomListBoxViewModel GetFilterDataContext(int index)
    {
        if (!_listBoxesDictionaryCache.TryGetValue(index, out CustomListBoxViewModel? outVal1))
        {
            outVal1 = new CustomListBoxViewModel(CurrentResultsTable, index, FilterTypeEnum.equals, ValueConverters[index]);
            _listBoxesDictionaryCache[index] = outVal1;
        }

        return outVal1;
    }

    private DataGridBoundColumn GetColumnToResults(TableOfSqlResults table, int index)
    {
        int savedI = index;
        var nullConverter = new NullValueConverter(); // each column have own one copy of NullValueConverter

        if (table.TypeCodes[index] == System.TypeCode.Decimal)
        {
            var scale = table.GetNumericScale(savedI);
            nullConverter.NumericFormat = $"N{(scale <= 0 ? 1 : scale)}";
        }
        else if (table.TypeCodes[index] == System.TypeCode.Int32 || table.TypeCodes[index] == System.TypeCode.Int64)
        {
            bool doDateFormat = true;
            foreach (TableRow item in table.Rows.Take(50)) // detect date stored as int/long 20241231 => 2024 12 31
            {
                var obj = item.Fields[savedI];
                if (!DateOnly.TryParseExact(obj?.ToString(), "yyyyMMdd", out var _))
                {
                    doDateFormat = false;
                    break;
                }
            }
            if (doDateFormat)
            {
                nullConverter.NumericIntFormat = "#### ## ##";
            }
        }
        ValueConverters.Add(nullConverter);

        var cellValueBinding = new Binding($"{nameof(TableRow.Fields)}[{index}]", BindingMode.OneWay)
        {
            Converter = nullConverter
        };

        FuncDataTemplate<object> headerTemplate = GetHeaderTemplate(table, index, savedI);

        DataGridBoundColumn col;
        //DataGridColumn col;

        if (table.TypeCodes[index] == TypeCode.Boolean)
        {
            col = new DataGridCheckBoxColumn()
            {
                Header = table.Headers[index],
                HeaderTemplate = headerTemplate,
                MaxWidth = 1_000,
                Binding = cellValueBinding,
                Width = DataGridLength.Auto,
                IsReadOnly = true,
                CanUserSort = true,
                CustomSortComparer = new CustomResultComparer(table.TypeCodes[index], index),
                IsThreeState = true,
                //CellStyleClasses = new Classes() { "pinnedStyle" },
                //Width = table.Headers.Count < 20 ? DataGridLength.Auto: new DataGridLength(120),
                //Width = DataGridLength.SizeToHeader,
            };
        }
        else
        {
            //standard column
            col = new DataGridTextColumn()
            {
                Header = table.Headers[index],
                HeaderTemplate = headerTemplate,
                MaxWidth = 1_000,
                Binding = cellValueBinding,
                Width = DataGridLength.Auto,
                //Width = table.Headers.Count < 20 ? DataGridLength.Auto: new DataGridLength(120),
                //Width = DataGridLength.,
                IsReadOnly = true,
                CanUserSort = true,
                CustomSortComparer = new CustomResultComparer(table.TypeCodes[index], index),
                //CellStyleClasses = new Classes() { "pinnedStyle" },
            };
            var t = table.TypeCodes[index];
            if (t == TypeCode.Char || t == TypeCode.SByte || t == TypeCode.Int16
                || t == TypeCode.Int32 || t == TypeCode.Int64 || t == TypeCode.Single
                || t == TypeCode.Double || t == TypeCode.Decimal)
            {
                col.CellStyleClasses.Add("pinnedStyle");// align to right
            }
        }

        if (_pinnedColumns.TryGetValue(col.Header as string, out var c))
        {
            col.DisplayIndex = c;
        }

        return col;
    }

    private readonly List<string> _groupedCols = [];
    private FuncDataTemplate<object> GetHeaderTemplate(TableOfSqlResults table, int index, int savedI)
    {
        var headerTemplate = new FuncDataTemplate<object>((x, y) =>
        {
            Grid wholeHeaderControl = new()
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto,Auto,Auto")
            };
            var tb = new TextBlock()
            {
                Text = table.Headers[index],
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Padding = new Thickness(6, 1, 7, 1),
                Background = Brushes.Transparent
                //Background = new SolidColorBrush(Colors.WhiteSmoke,0.01)
            };

            wholeHeaderControl.Children.Add(tb);
            Grid.SetColumn(tb, 0);

            //wholeHeaderControl.Children.Add(pnInner);

            var btGroupBy = new Button()
            {
                Margin = new Thickness(0, 2, 0, 0),
                Padding = new Thickness(0),
                Name = table.Headers[index],
                FontSize = 20,
                Background = Avalonia.Media.Brushes.Transparent,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
            };
            ToolTip groupByToolTip = new ToolTip
            {
                Content = new TextBlock()
                {
                    Text = "Group by column"
                }
            };

            var groupFilledSymbol = new PathIcon()
            {
                //Data = _groupFilledData
                Data = this.Resources["groupFilledData"] as StreamGeometry
            };

            var groupNormalSymbol = new PathIcon()
            {
                Data = this.Resources["groupNormalData"] as StreamGeometry
            };

            if (_groupedCols is not null && _groupedCols.Contains($"{nameof(TableRow.Fields)}[{savedI}]"))
            {
                btGroupBy.Content = groupFilledSymbol;
            }
            else
            {
                btGroupBy.Content = groupNormalSymbol;
            }

            btGroupBy.Click += (_, _) =>
            {
                if (btGroupBy.Content is PathIcon symbol && symbol == groupNormalSymbol)
                {
                    btGroupBy.Content = groupFilledSymbol;
                }
                else
                {
                    btGroupBy.Content = groupNormalSymbol;
                }
            };
            btGroupBy.Command = GroupByOneColumnCommand;
            btGroupBy.CommandParameter = btGroupBy.Name;

            wholeHeaderControl.Children.Add(btGroupBy);

            Grid.SetColumn(btGroupBy, 2);

            var btPin = new Button()
            {
                Margin = new Thickness(0, 2, 0, 0),
                Padding = new Thickness(0),
                Name = table.Headers[index] + "Pin",
                FontSize = 20,
                Background = Avalonia.Media.Brushes.Transparent,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Tag = "Pin-"
            };

            Grid.SetColumn(btPin, 3);

            if (_pinnedColumns.ContainsKey(table.Headers[index]))
            {
                btPin.Content = new PathIcon()
                {
                    Data = this.Resources["btPinData"] as StreamGeometry
                };
                if (ResultDataGrid.Columns[index] is DataGridTextColumn textColumn)
                {
                    (ResultDataGrid.Columns[index] as DataGridTextColumn).FontWeight = Avalonia.Media.FontWeight.SemiBold;
                }
            }
            else
            {
                btPin.Content = new PathIcon()
                {
                    Data = this.Resources["btPinData2"] as StreamGeometry
                };
                if (ResultDataGrid.Columns[index] is DataGridTextColumn textColumn)
                {
                    (ResultDataGrid.Columns[index] as DataGridTextColumn).FontWeight = Avalonia.Media.FontWeight.Normal;
                }
            }

            btPin.Click += (_, _) =>
            {
                if (btPin.Tag is string stringTag && stringTag == "Pin+")
                {
                    btPin.Content = new PathIcon()
                    {
                        Data = this.Resources["btPinData2"] as StreamGeometry
                    };
                    btPin.Tag = "Pin-";
                    _pinnedColumns.Remove(table.Headers[index]);
                    var c = ResultDataGrid.Columns[index];
                    c.DisplayIndex = _pinnedColumns.Count;
                    if (c is DataGridTextColumn textColumn)
                    {
                        textColumn.FontWeight = Avalonia.Media.FontWeight.Normal;
                    }
                    //c.CellStyleClasses.Remove("pinnedStyle");
                }
                else
                {
                    btPin.Content = new PathIcon()
                    {
                        Data = this.Resources["btPinData"] as StreamGeometry
                    };
                    btPin.Tag = "Pin+";
                    _pinnedColumns[table.Headers[index]] = ResultDataGrid.FrozenColumnCount;
                    var c = ResultDataGrid.Columns[index];
                    c.DisplayIndex = _pinnedColumns.Count - 1;// DataGrid.FrozenColumnCount;
                    if (c is DataGridTextColumn textColumn)
                    {
                        textColumn.FontWeight = Avalonia.Media.FontWeight.UltraBold;
                    }
                }
                ResultDataGrid.FrozenColumnCount = _pinnedColumns.Count;
                ResultDataGrid.Columns[index].IsVisible = false;
                ResultDataGrid.Columns[index].IsVisible = true;
            };

            wholeHeaderControl.Children.Add(btPin);

            var filterFiledIcon = new PathIcon()
            {
                Data = this.Resources["filterFilledData"] as StreamGeometry
            };

            var filterNormalIcon = new PathIcon()
            {
                Data = this.Resources["filterNormalData"] as StreamGeometry
            };

            var btFilter = new Button()
            {
                Content = filterNormalIcon,
                Margin = new Thickness(0, 2, 0, 0),
                Padding = new Thickness(0),
                Name = table.Headers[index] + "_" + "filter",
                FontSize = 20,
                Background = Avalonia.Media.Brushes.Transparent,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            };

            if (AdditionalValues?.ContainsKey(savedI) == true)
            {
                btFilter.Content = filterFiledIcon;
            }
            else
            {
                btFilter.Content = filterNormalIcon;
            }
            Grid.SetColumn(btFilter, 4);

            var customListBox = new CustomListBox();
            CustomListBoxViewModel customListBoxViewModel = GetFilterDataContext(index);

            customListBox.DataContext = customListBoxViewModel;

            var filterFlyout = new Flyout()
            {
                //Content = mainFlStackPanel,
                Content = customListBox,
                ShowMode = FlyoutShowMode.Standard,
            };

            filterFlyout.Opening += (_, _) =>
            {
                customListBoxViewModel.OpeningAction();
                RefreshLastColumnIndex = () => customListBoxViewModel?.RefreshList();
            };
            customListBoxViewModel.CloseAction = () =>
            {
                RefreshLastColumnIndex = null;
                filterFlyout.Hide();
            };

            void OnlineSearchAction()
            {
                var newFilter = new AditionalOneFilter(customListBoxViewModel.FilterTextForList)
                {
                    InList = customListBoxViewModel.CheckItems,
                    NotList = customListBoxViewModel.UncheckItems,
                    FilterType = customListBoxViewModel.FilterType
                };

                if (string.IsNullOrEmpty(newFilter.FilterEnteredTextPhase) && (newFilter.InList?.Count ?? 0) == 0 &&
                    (newFilter.NotList?.Count ?? 0) == 0 &&
                    newFilter.FilterType != FilterTypeEnum.isNull && newFilter.FilterType != FilterTypeEnum.isNotNull)
                {
                    AdditionalValues.Remove(index);
                }
                else
                {
                    AdditionalValues[index] = newFilter;
                }

                TriggerSearchTimerCommand?.Execute(null);
            }

            filterFlyout.Closed += (_, _) =>
            {
                if (AdditionalValues?.ContainsKey(savedI) == true)
                {
                    btFilter.Content = filterFiledIcon;
                }
                else
                {
                    btFilter.Content = filterNormalIcon;
                }

                OnlineSearchAction();
                if (AdditionalValues?.ContainsKey(savedI) == true)
                {
                    btFilter.Content = filterFiledIcon;
                }
                else
                {
                    btFilter.Content = filterNormalIcon;
                }
            };

            customListBoxViewModel.OnlineSearchAction = OnlineSearchAction;

            btFilter.ContextFlyout = filterFlyout;
            btFilter.Click += (_, _) =>
            {
                filterFlyout.ShowAt(btFilter, true);
            };

            wholeHeaderControl.Children.Add(btFilter);
            return wholeHeaderControl;
        });
        return headerTemplate;
    }
    private Action RefreshLastColumnIndex;



    [RelayCommand]
    private void GroupByOneColumn(string name)
    {
        if (GridCollectionView.Count >= 1_000_000)
        {
            App.GetRequiredService<IMessageForUserTools>().ShowSimpleMessageBoxInstance("to many items");
            return;
        }
        bool contains = false;
        var index = CurrentResultsTable.Headers.IndexOf(name);
        string propName = $"{nameof(TableRow.Fields)}[{index}]";

        foreach (var item in GridCollectionView.GroupDescriptions)
        {
            if (item.PropertyName == propName)
            {
                GridCollectionView.GroupDescriptions.Remove(item);
                contains = true;
                break;
            }
        }
        if (!contains)
        {
            string[] groupNames = [.. GridCollectionView.GroupDescriptions.Select(o => o.PropertyName), .. new string[] { propName }];
            CurrentResultsTable.ColumnsToSort.Clear();
            foreach (var nme in groupNames)
            {
                string fw = TableOfSqlResults.FIELDS_WORD; // name of array
                var m = Regex.Match(nme, @$"{fw}\[(?<number>.*)\]");
                if (m.Success && int.TryParse(m.Groups["number"].Value, out var index2))
                {
                    var cmp = new CustomResultComparer(CurrentResultsTable.TypeCodes[index2], index2);
                    CurrentResultsTable.ColumnsToSort.Add(new TableOfSqlResults.SortInfo() { ColNumber = cmp.Index, SortDirection = ListSortDirection.Ascending, Comparer = cmp });

                    var dataGridSortDescription = DataGridSortDescription.FromPath(nme, ListSortDirection.Ascending);
                    GridCollectionView.SortDescriptions.Add(dataGridSortDescription);
                }
            }
            CurrentResultsTable.SortFilteredRows();

            var group = new DataGridPathGroupDescription(propName)
            {
                ValueConverter = new ForGroupValueConverter()//https://github.com/KrzysztofDusko/JustDataEvoProject/issues/121
            };
            GridCollectionView.GroupDescriptions.Add(group);

            //doCollapseInNextCollapseAction = true;
            //CollapseAll(ResultDataGrid);

            //bool thisIsFirstGroup = (dataGridCollectionView.GroupDescriptions.Count == 0);
            //if (thisIsFirstGroup)
            //{
            //    //var customSortComparer = new JustyBase.Views.Tools.SqlResultsView.CustomLongComparer(CurrentResultsTable.TypeCodes[index], index);
            //    //var dataGridSortDescription = DataGridSortDescription.FromPath(propName, ListSortDirection.Ascending, customSortComparer);

            //    var dataGridSortDescription = DataGridSortDescription.FromPath(propName, ListSortDirection.Ascending);
            //    dataGridCollectionView.SortDescriptions.Clear();
            //    dataGridCollectionView.SortDescriptions.Add(dataGridSortDescription);

            //    //https://github.com/AvaloniaUI/Avalonia/discussions/14475
            //    //https://github.com/KrzysztofDusko/JustyBase/issues/300
            //    typeof(DataGridCollectionView).GetField("_flags", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(dataGridCollectionView, 5);
            //}
            //dataGridCollectionView.GroupDescriptions.Add(group);
            //if (thisIsFirstGroup)
            //{
            //    typeof(DataGridCollectionView).GetField("_flags", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(dataGridCollectionView, 4);
            //}
        }
        _groupedCols.Clear();
        foreach (var item in GridCollectionView.GroupDescriptions)
        {
            _groupedCols.Add(item.PropertyName);
        }
    }


    //sorting is not triggered becouse source is tuned for grouping
    private void ResultDataGrid_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        var bind = ((e.Column as DataGridBoundColumn).Binding as Avalonia.Data.Binding);
        var cmp = ((e.Column as DataGridBoundColumn).CustomSortComparer as CustomResultComparer);
        if (bind is not null && cmp is not null)
        {
            var collectionView = GridCollectionView;
            if (collectionView is null)
            {
                return;
            }
            var ct = this.CurrentResultsTable;
            // do nto sort if grouped
            if (collectionView.Groups is not null && collectionView.Groups.Count > 1)
            {
                foreach (var acualGroup in collectionView.GroupDescriptions)
                {
                    if (acualGroup.PropertyName == $"{TableOfSqlResults.FIELDS_WORD}[{cmp.Index}]")
                    {
                        TableOfSqlResults.SortInfo cs = null;

                        foreach (TableOfSqlResults.SortInfo item in ct.ColumnsToSort)
                        {
                            if (item.ColNumber == cmp.Index)
                            {
                                cs = item;
                                break;
                            }
                        }
                        if (cs is not null)
                        {
                            if (cs.SortDirection == ListSortDirection.Ascending)
                            {
                                cs.SortDirection = ListSortDirection.Descending;
                            }
                            else
                            {
                                cs.SortDirection = ListSortDirection.Ascending;
                            }

                        }
                        collectionView.SortDescriptions.Clear();
                        ct.SortFilteredRows();

                        return;
                    }
                }

                // DO NOT SORT IF GROUPED
                //e.Handled = true;
                //return;

                //SORT WHEN GROUPED - EXPIREMENTAL !
                var existingSortInfo = ct.ColumnsToSort.Find(m => m.ColNumber == cmp.Index);
                if (existingSortInfo is not null)
                {
                    ct.ColumnsToSort.Remove(existingSortInfo);
                }

                ct.ColumnsToSort.Add(new TableOfSqlResults.SortInfo()
                {
                    ColNumber = cmp.Index,
                    SortDirection = existingSortInfo?.SortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending,
                    Comparer = cmp
                });
                ct.SortFilteredRows();
                return;

            }
            //var sortDesc = collectionView.SortDescriptions.OfType<DataGridComparerSortDescription>().ToList();
            bool doDesc = false;

            bool contains = false;
            for (int i = 0; i < ct.ColumnsToSort.Count; i++)
            {
                var itm = ct.ColumnsToSort[i];
                if (itm.ColNumber == cmp.Index)
                {
                    if (itm.SortDirection == ListSortDirection.Ascending)
                    {
                        itm.SortDirection = ListSortDirection.Descending;
                    }
                    else
                    {
                        itm.SortDirection = ListSortDirection.Ascending;
                    }

                    contains = true;
                    break;
                }
            }

            if (!contains)
            {
                ct.ColumnsToSort.Clear();
                collectionView.SortDescriptions.Clear();
                //if (collectionView.Groups is null || collectionView.Groups.Count == 1)
                //{
                //    ct.ColumnsToSort.Clear();
                //}
                ct.ColumnsToSort.Add(new TableOfSqlResults.SortInfo()
                {
                    ColNumber = cmp.Index,
                    SortDirection = doDesc ? ListSortDirection.Descending : ListSortDirection.Ascending,
                    Comparer = cmp
                });
            }
            ct.SortFilteredRows();
        }
    }
    private void DataGrid_LoadingRowGroup(object? sender, DataGridRowGroupHeaderEventArgs e)
    {
        DataGridRowGroupHeader group = e.RowGroupHeader;
        group.IsItemCountVisible = true;
        //11.1###
        group.ItemCountFormat = "({0:N0} Items)";

        //var cc = group.GetTemplateChildren();
        //var itemsTxt = cc.Where(x => x.Name == "PART_ItemCountElement").FirstOrDefault() as TextBlock;
        //if (itemsTxt is not null && e.RowGroupHeader.DataContext is Avalonia.Collections.DataGridCollectionViewGroup dgcv)
        //{
        //    //decimal sum = 0;
        //    //foreach (var item in dgcv.Items.OfType<JustyBase.Models.Tools.TableRow>())
        //    //{
        //    //    sum += (decimal)item.Fields[3];
        //    //}
        //    //itemsTxt.Text = $"({dgcv.ItemCount:N0} Items, sum:{sum})";
        //    itemsTxt.Text = $"({dgcv.ItemCount:N0} Items)";
        //}
        //else
        //{
        //    group.TemplateApplied += Group_TemplateApplied;
        //}

        //group.Loaded += Group_Loaded;
        //return;

        //e.RowGroupHeader.Bounds !!  - można po tym poznać poziom

        //<PathIcon Height="12" Data="M 255 116 A 1 1 0 0 0 254 117 L 254 130 A 1 1 0 0 0 255 131 A 1 1 0 0 0 256 130 L 256 123.87109 C 256.1125 123.90694 256.2187 123.94195 256.33984 123.97852 C 257.18636 124.23404 258.19155 124.5 259 124.5 C 259.80845 124.5 260.52133 124.2168 261.17773 123.9668 C 261.83414 123.7168 262.43408 123.5 263 123.5 C 263.56592 123.5 264.5612 123.73404 265.37109 123.97852 C 266.18098 124.22299 266.82227 124.4668 266.82227 124.4668 A 0.50005 0.50005 0 0 0 267.5 124 L 267.5 118 A 0.50005 0.50005 0 0 0 267.17773 117.5332 C 267.17773 117.5332 266.50667 117.27701 265.66016 117.02148 C 264.81364 116.76596 263.80845 116.5 263 116.5 C 262.19155 116.5 261.47867 116.7832 260.82227 117.0332 C 260.16586 117.2832 259.56592 117.5 259 117.5 C 258.43408 117.5 257.4388 117.26596 256.62891 117.02148 C 256.39123 116.94974 256.17716 116.87994 255.98047 116.81445 A 1 1 0 0 0 255 116 z M 263 117.5 C 263.56592 117.5 264.5612 117.73404 265.37109 117.97852 C 266.00097 118.16865 266.29646 118.28239 266.5 118.35742 L 266.5 120.29297 C 266.25708 120.21012 265.97978 120.11797 265.66016 120.02148 C 264.81364 119.76596 263.80845 119.5 263 119.5 C 262.19155 119.5 261.47867 119.7832 260.82227 120.0332 C 260.16586 120.2832 259.56592 120.5 259 120.5 C 258.43408 120.5 257.4388 120.26596 256.62891 120.02148 C 256.39971 119.9523 256.19148 119.88388 256 119.82031 L 256 117.87109 C 256.1125 117.90694 256.2187 117.94195 256.33984 117.97852 C 257.18636 118.23404 258.19155 118.5 259 118.5 C 259.80845 118.5 260.52133 118.2168 261.17773 117.9668 C 261.83414 117.7168 262.43408 117.5 263 117.5 z M 263 120.5 C 263.56592 120.5 264.5612 120.73404 265.37109 120.97852 C 265.8714 121.12954 266.2398 121.25641 266.5 121.34961 L 266.5 123.30469 C 266.22286 123.20649 266.12863 123.1629 265.66016 123.02148 C 264.81364 122.76596 263.80845 122.5 263 122.5 C 262.19155 122.5 261.47867 122.7832 260.82227 123.0332 C 260.16586 123.2832 259.56592 123.5 259 123.5 C 258.43408 123.5 257.4388 123.26596 256.62891 123.02148 C 256.39971 122.9523 256.19148 122.88388 256 122.82031 L 256 120.87109 C 256.1125 120.90694 256.2187 120.94195 256.33984 120.97852 C 257.18636 121.23404 258.19155 121.5 259 121.5 C 259.80845 121.5 260.52133 121.2168 261.17773 120.9668 C 261.83414 120.7168 262.43408 120.5 263 120.5 z" />
        //string propName = e.RowGroupHeader.PropertyName;
        //double subLevel = e.RowGroupHeader.SublevelIndent;

        //e.RowGroupHeader.Template = new FuncControlTemplate((x, y) =>
        //{
        //    var groupInfo = (x as DataGridRowGroupHeader).DataContext as Avalonia.Collections.DataGridCollectionViewGroup;
        //    var sp = new StackPanel()
        //    {
        //        Orientation = Avalonia.Layout.Orientation.Horizontal,
        //    };

        //    var gd = this.ViewModel.dataGridCollectionView.GroupDescriptions;
        //    int index = 0;
        //    for (index = 0; index < gd.Count; index++)
        //    {
        //        if (gd[index].PropertyName == propName)
        //        {
        //            break;
        //        }
        //    }
        //    double dd = subLevel * (index + 1);

        //    int i = 0;
        //    for (i = 0; i < this.CurrentResultsTable.Headers.Count; i++)
        //    {
        //        int savedI = i;
        //        if (propName == $"{nameof(TableRow.Fields)}[{savedI}]")
        //        {
        //            break;
        //        }
        //    }
        //    string FieldName = this.CurrentResultsTable.Headers[i];

        //    //int index = this.ViewModel.GroupedCols.IndexOf(propName);
        //    //double dd = subLevel;
        //    //if (index !=-1)
        //    //{
        //    //    dd *= (index+1);
        //    //}

        //    //var bt = new Button()
        //    //{
        //    //    Background = Avalonia.Media.Brushes.Transparent,
        //    //    Padding = new Thickness(0),
        //    //    Margin = new Thickness(dd, 0, 5, 0),
        //    //    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        //    //    Height = 20,
        //    //    MinHeight = 20,
        //    //    Tag = false,
        //    //    Content = $"{FieldName}: {groupInfo.Key} ({groupInfo.ItemCount.ToString("N0")} items)"
        //    //};

        //    //bt.Click += (s, _) =>
        //    //{
        //    //    var gi = groupInfo;
        //    //    var thisBt = (s as Button);
        //    //    thisBt.Content = $"{FieldName}: {groupInfo.Key} ({groupInfo.ItemCount.ToString("N0")} items)";
        //    //    if ((bool)thisBt.Tag)
        //    //    {
        //    //        thisBt.Tag = false;
        //    //        //thisBt.Content = $"{FieldName}: {groupInfo.Key} ({groupInfo.ItemCount.ToString("N0")} items)";
        //    //        DataGrid.ExpandRowGroup(groupInfo, false);
        //    //    }
        //    //    else
        //    //    {
        //    //        thisBt.Tag = true;
        //    //        //thisBt.Content = $"{FieldName}: {groupInfo.Key} ({groupInfo.ItemCount.ToString("N0")} items)";
        //    //        DataGrid.CollapseRowGroup(groupInfo, false);
        //    //    }
        //    //};
        //    //var d1 = this.Resources["DataGridRowGroupHeaderIconClosedPath"];

        //    //expanded
        //    sp.Children.Add(new PathIcon() { Data = Avalonia.Media.PathGeometry.Parse("M109 486 19 576 1024 1581 2029 576 1939 486 1024 1401z"), Height = 10, Width = 10 });
        //    //closed
        //    sp.Children.Add(new PathIcon() { Data = Avalonia.Media.PathGeometry.Parse("M515 93l930 931l-930 931l90 90l1022 -1021l-1022 -1021z"), Height = 10, Width = 10 });
        //    //sp.Children.Add(bt);
        //    sp.Children.Add(new TextBlock() { Text = $" {FieldName}: {groupInfo.Key} ({groupInfo.ItemCount.ToString("N0")} items)" });

        //    //var b = new Border();
        //    //b.BorderThickness = new Thickness(1);
        //    //b.BorderBrush = Avalonia.Media.Brushes.Purple;
        //    //b.Child = sp;
        //    return sp;

        //    //var simplePath = this.Find<Path>("simplePath");
        //    //new TextBlock()
        //    //{
        //    //    Text = ">",
        //    //    Padding = new Thickness(0),
        //    //    Margin = new Thickness(0),
        //    //    FontSize = 12,
        //    //};
        //    //Avalonia.Media.StreamGeometry sg = Avalonia.Media.PathGeometry.Parse("M0,0L100,0 L100,100 L0,100Z M5,5 L95,5 L95,95 L5,95Z");
        //    //Path pth = new Path()
        //    //{
        //    //    Data = sg, Width=20, Height=20
        //    //};
        //    //pth.Stretch = Avalonia.Media.Stretch.UniformToFill;
        //    //bt.Content = pth;

        //    //sp.Children.Add(bt);      


        //});

        //group.FontSize = 20;
        //DataGrid.CollapseRowGroup(group.DataContext as Avalonia.Collections.DataGridCollectionViewGroup, true);
        //e.RowGroupHeader.DataTemplates.Clear();
    }

    //bool el = false;
    //void changeFilter(string val)
    //{
    //    el = !el;
    //    coll.Filter = null;
    //    coll.Filter = (o) => el && ((int)(o as TableRow).Fields[0] == 1;
    //}

    //private void DataGrid_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    //{
    //double x = e.GetPosition(DataGrid).X;
    //var ind = DataGrid.RowSelection.SelectedIndex;
    //if (ind.Count != 1)
    //{
    //    rowNum = -1;
    //    colNum = -1;
    //    return;
    //}
    //rowNum = ind[0];
    //colNum = DataGrid.Columns.GetColumnAt(x).index;

    //if (DataGrid.RowSelection.SelectedItem is TableRow tableRow)
    //{
    //    selectedRow = tableRow;
    //}
    //}
    //int rowNum = -1;
    //int colNum = -1;
    //TableRow selectedRow;

    //private void AdjustWidth(object sender, RoutedEventArgs e)
    //{
    //    double ScorllOffsetX = DataGrid.Scroll?.Offset.X??0.0;
    //    if (DataGrid.Columns is null)
    //    {
    //        return;
    //    }
    //    int startIndex = DataGrid.Columns.GetColumnAt(ScorllOffsetX).index;
    //    double limit = DataGrid.Bounds.Width;
    //    double sum = 0;
    //    for (int i = startIndex; i < DataGrid.Columns.Count; i++)
    //    {
    //        double prevWidth = DataGrid.Columns[i].ActualWidth;

    //        DataGrid.Columns.SetColumnWidth(i, GridLength.Auto);
    //        sum += DataGrid.Columns[i].ActualWidth;

    //        if (sum > limit || double.IsNaN(sum))
    //        {
    //            DataGrid.Columns.SetColumnWidth(i, new GridLength(prevWidth));
    //            break;
    //        }
    //    }
    //    return;
    //}

    private void DataGrid_DoubleTapped(object sender, RoutedEventArgs e)
    {
        DataGrid dg = (sender as DataGrid);

        bool headerClicked = e.Source is not Control control || control.DataContext is not TableRow;

        if (headerClicked && dg.Name != nameof(rowDetailsDataGrid))
        {
            if ((e.Source as Control)?.DataContext is JustyBase.ViewModels.Tools.SqlResultsViewModel)
            {
                return;
            }
            if ((e.Source as Control)?.DataContext is Avalonia.Collections.DataGridCollectionViewGroup)
            {
                return;
            }
            DataGridDoubleClicked((e.Source as Control)?.DataContext?.ToString(), true);
            return;
        }
        else if (dg?.SelectedItem is TableRow row)
        {
            //var o = row.Fields[DataGrid.CurrentColumn.DisplayIndex];
            var o = row.Fields[ResultDataGrid.Columns.IndexOf(ResultDataGrid.CurrentColumn)];
            DataGridDoubleClicked(o, false);
        }
        else if (dg?.SelectedItem is RowDetail rowDetail)
        {
            if (dg.CurrentColumn.DisplayIndex == 0)
            {
                DataGridDoubleClicked(rowDetail.Name, true);
            }
            else if (dg.CurrentColumn.DisplayIndex < dg.Columns.Count - 1)
            {
                DataGridDoubleClicked(rowDetail.FieldsValues[dg.CurrentColumn.DisplayIndex - 1], false);
            }
            else
            {
                DataGridDoubleClicked(rowDetail.TypeName, false);
            }
        }
    }

    private void ResultDataGrid_Tapped(object? sender, TappedEventArgs e)
    {
        DataGrid dg = (sender as DataGrid);

        bool headerClicked = e.Source is not Control control || control.DataContext is not TableRow;
        if (!headerClicked)
        {
            if (dg?.SelectedItem is TableRow)
            {
                if (PrevCols.Count == 2)
                {
                    PrevCols.TryDequeue(out int _);
                }

                int acualCol = ResultDataGrid.Columns.IndexOf(ResultDataGrid.CurrentColumn);
                PrevCols.Enqueue(acualCol);


                //var currentRow =ResultDataGrid.FindDescendantOfType<DataGridRowsPresenter>()
                //    .Children.OfType<DataGridRow>()
                //    .FirstOrDefault(r => r.FindDescendantOfType<DataGridCellsPresenter>()
                //        .Children.Any(p => p.Classes.Contains(":current")));
                //var item = currentRow?.DataContext;

                //var currentCell = currentRow?.FindDescendantOfType<DataGridCellsPresenter>().Children
                //    .OfType<DataGridCell>().FirstOrDefault(p => p.Classes.Contains(":current"));

                //IEnumerable<DataGridCell> currentRowCells = currentRow?.FindDescendantOfType<DataGridCellsPresenter>().Children.OfType<DataGridCell>();

                //foreach (var item2 in currentRowCells)
                //{
                //    currentCell.Background = Brushes.PaleVioletRed;
                //}

            }
        }
    }
}


//static SqlResultsView()
//{
//PointerMovedEvent.AddClassHandler<DataGridCell>(
//(sender, e) =>
//{
//    if (e is PointerEventArgs args)
//    {
//        var point = args.GetCurrentPoint(sender as Control);
//        //var x = point.Position.X;
//        //var y = point.Position.Y;
//        Debug.WriteLine("### I AM ###");
//        //if (point.Properties.IsLeftButtonPressed)
//        //{
//            var row = (sender as DataGridCell).FindAncestorOfType<DataGridRow>();
//            var grid = (sender as DataGridCell).FindAncestorOfType<DataGrid>();
//            grid.SelectedItems.Add(row.DataContext);
//            Debug.WriteLine($"### SELECT {Random.Shared.Next()} {(row.DataContext as TableRow).Fields[0]} ###");
//        //}
//        (sender as DataGridCell).Background = Brushes.Red;

//    }
//});

//PointerEnteredEvent.AddClassHandler<DataGridCell>(
//(x, e) =>
//{
//    //x.Background = Random.Shared.NextDouble() > 0.5 ? Brushes.Yellow : Brushes.Transparent;

//    if (x.Classes.Contains(":selected"))
//    {
//        //x.Classes.Clear();
//        //x.Classes.Add("class0");
//        if (x.Background.Opacity == 1)
//        {
//            x.Background = Brushes.Orange;
//        }
//    }


//    //var currentRow =ResultDataGrid.FindDescendantOfType<DataGridRowsPresenter>()
//    //    .Children.OfType<DataGridRow>()
//    //    .FirstOrDefault(r => r.FindDescendantOfType<DataGridCellsPresenter>()
//    //        .Children.Any(p => p.Classes.Contains(":current")));
//    //var item = currentRow?.DataContext;

//    //var currentCell = currentRow?.FindDescendantOfType<DataGridCellsPresenter>().Children
//    //    .OfType<DataGridCell>().FirstOrDefault(p => p.Classes.Contains(":current"));

//    //IEnumerable<DataGridCell> currentRowCells = currentRow?.FindDescendantOfType<DataGridCellsPresenter>().Children.OfType<DataGridCell>();

//    //foreach (var item2 in currentRowCells)
//    //{
//    //    currentCell.Background = Brushes.PaleVioletRed;
//    //}


//    //x.BindClass("newClass", new Binding("x"), null);
//}, handledEventsToo: true
//);

//PointerExitedEvent.AddClassHandler<DataGridCell>(
//(x, e) =>
//{
//    //x.Background = Random.Shared.NextDouble() > 0.5 ? Brushes.Yellow : Brushes.Transparent;
//    x.Classes.Clear();
//    x.Classes.Add("class1");
//    //x.BindClass("newClass", new Binding("x"), null);
//}, handledEventsToo: true
//);
//}

//private int _verticalScrollIndex = 0;
//private int _endIdx = 0;
//public void GetIndexSB()
//{
//    var sb = resultDataGrid.FindDescendantOfType<ScrollBar>(false);
//    if (sb == null)
//    {
//        return;
//    }
//    var rowHeight = 25.0;//datagrid rowheigh
//    var headerHeigh = 24;
//    var startIdx = (int)Math.Floor(sb.Value / rowHeight);
//    var offset = (int)Math.Floor((sb.ViewportSize) / rowHeight) - 1; // 24 header, 21 rows
//    var endIdx = startIdx + offset;
//    _endIdx = endIdx;
//    if (sb.Value == sb.Maximum - 1)//occurs when scrolling to the end, then reversing scroll direction
//    {
//        --startIdx;
//    }
//    _verticalScrollIndex = startIdx;
//}

//private object _itemToScrollTo = null;
//private void SqlResultsView_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
//{
//    GetIndexSB();
//    if (_verticalScrollIndex < this.ViewModel.dataGridCollectionView.Count)
//    {
//        _itemToScrollTo = this.ViewModel.dataGridCollectionView[_verticalScrollIndex];
//    }
//}

//private async void SqlResultsView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
//{
//    if (_itemToScrollTo is not null)
//    {
//        Dispatcher.UIThread.InvokeAsync((Action)(() => resultDataGrid.ScrollIntoView(this.ViewModel.dataGridCollectionView[^1], null)), DispatcherPriority.ContextIdle);
//        await Task.Delay(1000);
//        Dispatcher.UIThread.InvokeAsync((Action)(() => resultDataGrid.ScrollIntoView(_itemToScrollTo, null)), DispatcherPriority.ContextIdle);
//    }
//}

////https://github.com/AvaloniaUI/Avalonia/commit/798c7128657f47f3492366cb26a1c32ac18a5d7c
//private void SqlResultsView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
//{
//    var _lastDataConttext = this.ViewModel.SelectedItemFromDeatached;
//    var selIndexTmp = this.ViewModel.SelectedIndexFromDeatached;
//    if (_generalApplicationData.Config.UseCachedResultsView)
//    {
//        resultDataGrid.SelectedIndex = selIndexTmp;
//    }
//    else
//    {
//        Dispatcher.UIThread.InvokeAsync((Action)(() =>
//        {
//            try
//            {
//                resultDataGrid.SelectedIndex = selIndexTmp;
//            }
//            catch { }

//        }));
//    }

//    if (_lastDataConttext is not null)
//    {
//        Dispatcher.UIThread.InvokeAsync((Action)(() =>
//        {
//            if (_lastDataConttext is not null)
//            {
//                try
//                {
//                    //if (!_generalApplicationData.Config.TryToRestoreScrollPosition)
//                    //{
//                    //    resultDataGrid.SelectedIndex = selIndexTmp;
//                    //}

//                    resultDataGrid.ScrollIntoView(_lastDataConttext, null);
//                    //if (_lastSelIndex != -1)
//                    //{
//                    //    resultDataGrid.SelectedIndex = _lastSelIndex;
//                    //}
//                }
//                catch { }
//            }
//        }), DispatcherPriority.ContextIdle);

//    }
//}

//https://github.com/AvaloniaUI/Avalonia/commit/798c7128657f47f3492366cb26a1c32ac18a5d7c
//private void SqlResultsView_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
//{
//    DataGridRowsPresenter? rowPresenter = resultDataGrid.GetTemplateChildren().OfType<Avalonia.Controls.Primitives.DataGridRowsPresenter>().FirstOrDefault();
//    double bestTop = 0;
//    int rowHeight = 22;

//    if (rowPresenter is not null)
//    {
//        var rowPresenterHeigh = rowPresenter.Bounds.Height;
//        foreach (DataGridRow item in rowPresenter.Children.OfType<DataGridRow>())
//        {
//            if (item.IsVisible && item.Bounds.Top > bestTop && item.Bounds.Top + rowHeight <= rowPresenterHeigh + 0.0001)
//            {
//                bestTop = item.Bounds.Top;
//                if (this.ViewModel is not null)
//                {
//                    this.ViewModel.SelectedItemFromDeatached = item.DataContext;
//                    this.ViewModel.SelectedIndexFromDeatached = resultDataGrid.SelectedIndex;
//                }
//            }
//        }
//    }

//    //if (rowPresenter is not null)
//    //{
//    //    foreach (DataGridRow item in rowPresenter.Children.OfType<DataGridRow>())
//    //    {
//    //        if (item.IsVisible)
//    //        {
//    //            DataGridRowHeader rh = item.FindDescendantOfType<Avalonia.Controls.Primitives.DataGridRowHeader>();
//    //            if (rh.Content is TextBlock block)
//    //            {
//    //                if (int.TryParse(block.Text, out var resInt) && resInt > bestTop)
//    //                {
//    //                    bestTop = resInt;
//    //                }
//    //            }
//    //            _lastDataConttext = item.DataContext;
//    //        }
//    //    }
//    //}
//}

//private void SqlResultsView_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
//{
//    if (_lastSelectedItem == -1)
//    {
//        return;
//    }
//    resultDataGrid.SelectedIndex = _lastSelectedItem;
//    try
//    {
//        Dispatcher.UIThread.InvokeAsync((Action)(() =>
//        {
//            try
//            {
//                var col = this.ViewModel.dataGridCollectionView;
//                if (_lastSelectedItem + 5 < col.Count)
//                {
//                    resultDataGrid.ScrollIntoView(this.ViewModel.dataGridCollectionView[_lastSelectedItem + 5], null);
//                }
//                else if (_lastSelectedItem < col.Count)
//                {
//                    resultDataGrid.ScrollIntoView(this.ViewModel.dataGridCollectionView[_lastSelectedItem], null);
//                }
//            }
//            catch (Exception ex)
//            {
//                _generalApplicationData.LogToWebObject.TrackCrashMessagePlusOpenNotepad(ex, "go to last row 1");
//            }
//        }
//        ), DispatcherPriority.ContextIdle);
//    }
//    catch (Exception ex2)
//    {
//        _generalApplicationData.LogToWebObject.TrackCrashMessagePlusOpenNotepad(ex2, "go to last row 2");
//    }
//}


//private void ResultDataGrid_VerticalScroll(object? sender, ScrollEventArgs e)
//{
//    //capture index of last visible row
//    var sb = sender as ScrollBar;
//    if (sb == null)
//    {
//        return;
//    }
//    _sb = sb;
//    _sbVal = sb.Value;
//    var rowHeight = 21;//datagrid rowheigh
//    var headerHeigh = 24;
//    var startIdx = (int)Math.Ceiling(sb.Value / rowHeight);
//    var offset = (int)Math.Floor((sb.ViewportSize - headerHeigh) / rowHeight) - 1 + 1  ; // 24 header, 21 rows
//    var endIdx = startIdx + offset;

//    if (sb.Value == sb.Maximum - 1)//occurs when scrolling to the end, then reversing scroll direction
//    {
//        --startIdx;
//    }
//    _verticalScrollIndex = startIdx;
//}



//private void DataGrid_LoadingRow1(object? sender, DataGridRowEventArgs e)
//{
//    foreach (var item in DataGrid.Columns)
//    {
//        if (PinnedColumns.ContainsKey(item.Header.ToString()))
//        { 
//            item.CellStyleClasses.Add("pinnedStyle");
//        }
//    }
//}


//https://github.com/AvaloniaUI/Avalonia/pull/14465
//private void Group_TemplateApplied(object? sender, TemplateAppliedEventArgs e)
//{
//    if (sender is DataGridRowGroupHeader rowGroupHeader && rowGroupHeader.DataContext is Avalonia.Collections.DataGridCollectionViewGroup dgcv)
//    {
//        var _itemCountElement = e.NameScope.Find<TextBlock>("PART_ItemCountElement");
//        //var _propertyNameElement = e.NameScope.Find<TextBlock>("PART_PropertyNameElement");
//        var cnt = dgcv.ItemCount;

//        //decimal sum = 0;
//        //foreach (var item in dgcv.Items.OfType<JustyBase.Models.Tools.TableRow>())
//        //{
//        //    sum += (decimal)item.Fields[3];
//        //}
//        if (cnt == 1)
//        {
//            _itemCountElement.Text = $"({cnt:N0} Item)";
//        }
//        if (cnt > 0)
//        {
//            //_itemCountElement.Text = $"({cnt:N0} Items, sum:{sum})";
//            _itemCountElement.Text = $"({cnt:N0} Items)";
//        }
//    }
//}
