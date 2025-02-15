using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Helpers;
using JustyBase.Models;
using JustyBase.Models.Tools;
using JustyBase.Public.Lib.Helpers;
using System;
using System.Collections.Generic;

namespace JustyBase.ViewModels;

public partial class CustomListBoxViewModel : ObservableObject
{
    public TableOfSqlResults RefDataTable { get; set; }

    public string FilterTextForList
    {
        get => field ??= "";
        set
        {
            UncheckItems.Clear();
            CheckItems.Clear();
            SetProperty(ref field, value);
            OnlineSearchAction?.Invoke(); // RefreshList included
        }
    }

    public void RefreshList()
    {
        LoadItems();
    }

    private void DoPreviewList()
    {
        Items.SupressNotification = true;
        Items.Clear();

        AditionalOneFilter aditionalOneFilter = new AditionalOneFilter(FilterTextForList) { FilterType = FilterType };

        TypeCode tc = RefDataTable.TypeCodes[ColumnIndex];
        foreach (var item in _actualValuesList)
        {
            if (string.IsNullOrEmpty(FilterTextForList) || aditionalOneFilter.GetComparisionResultGeneral(tc, item))
            {
                Items.Add(new FilterItem(item, true, _valueConverter));
            }
        }
        Items.SupressNotification = false;
        OnPropertyChanged(nameof(Items));

        var stats = new TableRowStats(RefDataTable, RefDataTable.FilteredRows, ColumnIndex);

        string txt = $"Count {RefDataTable.FilteredRows.Count:N0} | Distinct {stats.DistinctCnt:N0} | Sum {stats.Sum:N3}";
        if (stats.MinOfColumn != decimal.MaxValue)
        {
            txt += $"\nMin {stats.MinOfColumn:N3} ";
        }
        if (stats.MaxOfColumn != decimal.MinValue)
        {
            txt += $" | Max {stats.MaxOfColumn:N3} ";
        }
        if (stats.NotNullCnt > 0)
        {
            txt += $" | Avg {(stats.Sum / stats.NotNullCnt):N3}";
        }
        FilteredItemsCount = txt;
    }

    [ObservableProperty]
    public partial string FilteredItemsCount { get; set; }

    [ObservableProperty]
    public partial bool NotInMode { get; set; } = true;
    public ICommand InModeChangedCommand { get; init; }

    [ObservableProperty]
    public partial bool Warning20k { get; set; }
    public HashSet<object> UncheckItems { get; set; } = [];
    public HashSet<object> CheckItems { get; set; } = [];
    public ICommand ClearCommand { get; init; }
    public ICommand OkCommand { get; init; }
    public ICommand CancelCommand { get; init; }
    public ObservableCollectionEx<FilterItem> Items { get; set; }
    public FilterTypeEnum FilterType { get; set; }

    public readonly int ColumnIndex = 0;
    private readonly IValueConverter _valueConverter;
    public CustomListBoxViewModel(TableOfSqlResults table, int columnIndex, FilterTypeEnum filterType, IValueConverter valueConverter)
    {
        ColumnIndex = columnIndex;
        RefDataTable = table;
        _valueConverter = valueConverter;
        Items = [];
        ClearCommand = new RelayCommand(Clear);
        OkCommand = new RelayCommand(OkBtAction);
        CancelCommand = new RelayCommand(CancelBtAction);
        FilterType = filterType;
        SelectedTextFilterType = filterType.StringRepresentation();

        if (table.TypeCodes[ColumnIndex] == TypeCode.Byte
            || table.TypeCodes[ColumnIndex] == TypeCode.SByte
            || table.TypeCodes[ColumnIndex] == TypeCode.Int16
            || table.TypeCodes[ColumnIndex] == TypeCode.Int32
            || table.TypeCodes[ColumnIndex] == TypeCode.Int64
            || table.TypeCodes[ColumnIndex] == TypeCode.Single
            || table.TypeCodes[ColumnIndex] == TypeCode.Double
            || table.TypeCodes[ColumnIndex] == TypeCode.Decimal)
        {
            FilterTypeTextList = _numberFilterList;
        }
        if (table.TypeCodes[ColumnIndex] == TypeCode.Object
            || table.TypeCodes[ColumnIndex] == TypeCode.String
            || table.TypeCodes[ColumnIndex] == TypeCode.DateTime)
        {
            FilterTypeTextList = _stringAndDateFilterList;
        }

        InModeChangedCommand = new RelayCommand(() =>
        {
            if (!NotInMode)
            {
                foreach (FilterItem item in Items)
                {
                    item.IsChecked = false;
                }
            }
            else
            {
                foreach (FilterItem item in Items)
                {
                    item.IsChecked = true;
                }
            }
            CheckItems.Clear();
            UncheckItems.Clear();
        });
    }

    private static readonly List<string> _allFitlerTypes =
    [
        FilterTypeEnum.equals.StringRepresentation(),
        FilterTypeEnum.notEquals.StringRepresentation(),
        FilterTypeEnum.isNull.StringRepresentation(),
        FilterTypeEnum.isNotNull.StringRepresentation(),
        FilterTypeEnum.startsWith.StringRepresentation(),
        FilterTypeEnum.endsWith.StringRepresentation(),
        FilterTypeEnum.contains.StringRepresentation(),
        FilterTypeEnum.notContains.StringRepresentation(),
        FilterTypeEnum.greaterThan.StringRepresentation(),
        FilterTypeEnum.greaterOrEqualThan.StringRepresentation(),
        FilterTypeEnum.lowerThan.StringRepresentation(),
        FilterTypeEnum.lowerOrEqualThan.StringRepresentation(),
    ];

    private static readonly List<string> _numberFilterList =
    [
        FilterTypeEnum.equals.StringRepresentation(),
        FilterTypeEnum.notEquals.StringRepresentation(),
        FilterTypeEnum.isNull.StringRepresentation(),
        FilterTypeEnum.isNotNull.StringRepresentation(),
        FilterTypeEnum.greaterThan.StringRepresentation(),
        FilterTypeEnum.greaterOrEqualThan.StringRepresentation(),
        FilterTypeEnum.lowerThan.StringRepresentation(),
        FilterTypeEnum.lowerOrEqualThan.StringRepresentation(),
    ];
    private static readonly List<string> _stringAndDateFilterList =
    [
        FilterTypeEnum.equals.StringRepresentation(),
        FilterTypeEnum.notEquals.StringRepresentation(),
        FilterTypeEnum.isNull.StringRepresentation(),
        FilterTypeEnum.isNotNull.StringRepresentation(),
        FilterTypeEnum.startsWith.StringRepresentation(),
        FilterTypeEnum.endsWith.StringRepresentation(),
        FilterTypeEnum.contains.StringRepresentation(),
        FilterTypeEnum.notContains.StringRepresentation(),
    ];

    public List<string> FilterTypeTextList { get; set; } = _allFitlerTypes;

    public string SelectedTextFilterType
    {
        get;
        set
        {
            SetProperty(ref field, value);
            FilterType = SelectedTextFilterType.FilterTypeEnumFromStringRepresentation();
            if (_actualValuesList is not null) // started
            {
                string tmp = FilterTextForList;
                FilterTextForList = "";
                FilterTextForList = tmp;
            }
        }
    } = FilterTypeEnum.equals.StringRepresentation();

    public Action CloseAction;
    public Action OnlineSearchAction;

    private object[] _actualValuesList = [];

    public void OpeningAction()
    {
        LoadItems();
    }

    private void LoadItems()
    {
        _actualValuesList = RefDataTable.GetAcualPopularValues(ColumnIndex);
        if (_actualValuesList.Length >= TableOfSqlResults.FILTER_ITEMS_LIMIT)
        {
            Warning20k = true;
        }
        DoPreviewList();
    }

    //private void ClearAllChecked()
    //{
    //    foreach (var item in Items)
    //    {
    //        if (item.IsChecked)
    //        {
    //            item.IsChecked = false;
    //        }
    //    }
    //}

    private void Clear()
    {
        FilterTextForList = "";
        SelectedTextFilterType = FilterTypeEnum.equals.StringRepresentation();
        NotInMode = true;
        //LoadItems();
        OnPropertyChanged(nameof(Items));
    }

    private void OkBtAction()
    {
        CheckItems.Clear();
        UncheckItems.Clear();
        if (NotInMode)
        {
            foreach (var item in Items)
            {
                if (!item.IsChecked)
                {
                    UncheckItems.Add(item._filterValue);
                }
            }
        }
        else
        {
            foreach (var item in Items)
            {
                if (item.IsChecked)
                {
                    CheckItems.Add(item._filterValue);
                }
            }
        }
        CloseAction?.Invoke();
    }
    private void CancelBtAction()
    {
        FilterTextForList = "";
        CloseAction?.Invoke();
    }
}


