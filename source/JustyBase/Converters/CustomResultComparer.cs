using JustyBase.Models;
using System;
using System.Collections;

namespace JustyBase.Helpers;

internal sealed class CustomResultComparer : IComparer
{
    private readonly int _index;
    public int Index => _index;
    public CustomResultComparer(TypeCode typeCode, int index)
    {
        _index = index;
        _chosedComparer = typeCode switch
        {
            TypeCode.Boolean => CompareGeneric<bool>,
            TypeCode.Char => CompareGeneric<Char>,
            TypeCode.SByte or TypeCode.Byte => CompareGeneric<byte>,
            TypeCode.Int16 or TypeCode.UInt16 => CompareGeneric<Int16>,
            TypeCode.UInt32 => CompareGeneric<UInt32>,
            TypeCode.Int32 => CompareGeneric<Int32>,
            TypeCode.UInt64 => CompareGeneric<UInt64>,
            TypeCode.Int64 => CompareGeneric<Int64>,
            TypeCode.Single => CompareGeneric<Single>,
            TypeCode.Double => CompareGeneric<double>,
            TypeCode.Decimal => CompareGeneric<decimal>,
            TypeCode.DateTime => CompareGeneric<DateTime>,
            TypeCode.String => CompareGeneric<string>,
            TypeCode.Empty => CompareDef,
            TypeCode.Object => CompareDef,
            TypeCode.DBNull => CompareDef,
            _ => CompareDef,
        };
    }

    private readonly Func<TableRow, TableRow, int> _chosedComparer;

    public int Compare(object? x, object? y)
    {
        if (_index != -1 && x is TableRow tableRow1 && y is TableRow tableRow2)
        {
            var val1 = tableRow1.Fields[_index];
            var val2 = tableRow2.Fields[_index];
            if (val1 == val2)
            {
                return 0;
            }
            if (val1 is null || val1 == DBNull.Value)
            {
                return -1;
            }
            if (val2 is null || val2 == DBNull.Value)
            {
                return 1;
            }

            return _chosedComparer.Invoke(tableRow1, tableRow2);
        }
        return 0;
    }

    public int CompareGeneric<T>(TableRow x, TableRow y) where T : IComparable<T>
    {
        T val1 = (T)x.Fields[_index];
        T val2 = (T)y.Fields[_index];
        return (val1).CompareTo(val2);
    }

    public int CompareDef(TableRow x, TableRow y)
    {
        var val1 = x.Fields[_index]?.ToString();
        var val2 = y.Fields[_index]?.ToString();
        return (val1).CompareTo(val2);
    }
}
