using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace JustyBase.Models;
public sealed class TableOfSqlResults
{
    public List<string> Headers { get; set; }
    public List<string> DataTypeNames { get; set; }
    public List<TypeCode> TypeCodes { get; set; }

    public byte[] NumericScales = Array.Empty<byte>();

    public byte GetNumericScale(int index)
    {
        if (index < NumericScales.Length)
        {
            return NumericScales[index];
        }
        return 6;
    }

    public List<TableRow> Rows { get; set; }
    public List<TableRow> FilteredRows { get; set; }

    public sealed class SortInfo
    {
        public int ColNumber { get; set; }
        public ListSortDirection SortDirection { get; set; }
        public IComparer Comparer { get; set; }
    }
    public List<SortInfo> ColumnsToSort { get; set; } = [];
    public void SortFilteredRows()
    {
        FilteredRows.Sort((x, y) =>
        {
            foreach (var cs in ColumnsToSort)
            {
                var resTmp = (cs.SortDirection == ListSortDirection.Descending ? -1 : 1) * cs.Comparer.Compare(x, y);
                if (resTmp != 0)
                {
                    return resTmp;
                }
            }
            return 0;
        });
    }

    public TableOfSqlResults()
    {
        Headers = [];
        Rows = [];
        FilteredRows = [];
    }

    public const int FILTER_ITEMS_LIMIT = 20_000;
    public const string FIELDS_WORD = "Fields";
    public object[] GetAcualPopularValues(int columnIndex)
    {
        HashSet<object> values = [];
        int cnt = FilteredRows.Count;

        for (int i = 0; i < cnt; i++)
        {
            object colVal = FilteredRows[i].Fields[columnIndex];
            values.Add(colVal);
            if (i >= 20_000 && values.Count >= FILTER_ITEMS_LIMIT)
            {
                break;
            }
        }
        var arr = values.ToArray();
        Array.Sort(arr);
        return arr;
    }
    public void DoClear()
    {
        Rows.Clear();
        FilteredRows.Clear();
    }
}
