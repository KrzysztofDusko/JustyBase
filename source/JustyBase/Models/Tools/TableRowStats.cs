using System;
using System.Collections.Generic;

namespace JustyBase.Models.Tools;

public sealed class TableRowStats
{
    public decimal Sum { get; set; } = 0;
    public int NotNullCnt { get; set; } = 0;
    public int DistinctCnt { get; set; } = 0;
    public decimal? MinOfColumn { get; set; } = decimal.MaxValue;
    public decimal? MaxOfColumn { get; set; } = decimal.MinValue;
    public TableRowStats(TableOfSqlResults table, IEnumerable<TableRow> rows, int columnIndex)
    {
        var tpe = table.TypeCodes[columnIndex];

        decimal sum = 0;
        int notNullCnt = 0;
        decimal minOfColumn = decimal.MaxValue;
        decimal maxOfColumn = decimal.MinValue;
        HashSet<string> strings = [];
        var decimalCompatibile = IsToDecimalCompatibile(tpe);

        foreach (TableRow tableRow in rows)
        {
            var val = tableRow.Fields[columnIndex];
            if (val is not null && val != DBNull.Value)
            {
                notNullCnt++;
                if (decimalCompatibile)
                {
                    var declimalVal = Convert.ToDecimal(val);
                    sum += declimalVal;
                    if (declimalVal < minOfColumn)
                    {
                        minOfColumn = declimalVal;
                    }
                    if (declimalVal > maxOfColumn)
                    {
                        maxOfColumn = declimalVal;
                    }
                }
                strings.Add(val.ToString());
            }
        }
        NotNullCnt = notNullCnt;
        Sum = sum;
        DistinctCnt = strings.Count;
        if (minOfColumn == decimal.MaxValue)
        {
            MinOfColumn = null;
        }
        else
        {
            MinOfColumn = minOfColumn;
        }
        if (maxOfColumn == decimal.MinValue)
        {
            MaxOfColumn = null;
        }
        else
        {
            MaxOfColumn = maxOfColumn;
        }
    }
    public static IEnumerable<object> CurrentColumnCells(IEnumerable<TableRow> rows, int columnIndex)
    {
        foreach (TableRow tableRow in rows)
        {
            yield return tableRow.Fields[columnIndex];
        }
    }

    private readonly HashSet<TypeCode> _decimalCompatibileArray = [ TypeCode.Byte, TypeCode.SByte, TypeCode.UInt16, TypeCode.Int16, TypeCode.Int32, TypeCode.Int64,
                TypeCode.Single, TypeCode.Double, TypeCode.Decimal ];

    private bool IsToDecimalCompatibile(TypeCode tpe)
    {
        return _decimalCompatibileArray.Contains(tpe);
    }
}