using System;
using System.Collections.Generic;
using System.Numerics;

namespace JustyBase.Models;

public sealed class AditionalOneFilter
{
    public readonly string FilterEnteredTextPhase;
    public readonly Int16? FilterEnteredPhaseAsInt16 = null;
    public readonly int? FilterEnteredPhaseAsInt = null;
    public readonly long? filterEnteredPhaseAsLong = null;
    public readonly double? FilterEnteredPhaseAsDouble = null;
    public readonly decimal? FilterEnteredPhaseAsDecimal = null;
    public HashSet<object> NotList;
    public HashSet<object> InList;
    public AditionalOneFilter(string likePhase)
    {
        FilterEnteredTextPhase = likePhase;
        if (Int16.TryParse(likePhase, out var resInt16))
        {
            FilterEnteredPhaseAsInt16 = resInt16;
        }
        if (int.TryParse(likePhase, out var resInt))
        {
            FilterEnteredPhaseAsInt = resInt;
        }
        if (long.TryParse(likePhase, out var resLong))
        {
            filterEnteredPhaseAsLong = resLong;
        }
        if (double.TryParse(likePhase, out var resDoulee))
        {
            FilterEnteredPhaseAsDouble = resDoulee;
        }
        if (decimal.TryParse(likePhase, out var resDecimal))
        {
            FilterEnteredPhaseAsDecimal = resDecimal;
        }
    }
    public FilterTypeEnum FilterType = FilterTypeEnum.contains;


    public bool GetComparisionResultGeneral(TypeCode typeCode, object columnValue)
    {
        if (columnValue is null)
        {
            if (FilterType == FilterTypeEnum.isNull)
            {
                return true;
            }
            if (FilterType == FilterTypeEnum.isNull)
            {
                return true;
            }
            if (FilterType != FilterTypeEnum.isNotNull && string.IsNullOrEmpty(FilterEnteredTextPhase) && columnValue is null)
            {
                return true;
            }
            return false;
        }
        if (FilterType == FilterTypeEnum.isNull && columnValue is not null)
        {
            return false;
        }
        if (FilterType == FilterTypeEnum.isNotNull && columnValue is not null)
        {
            return true;
        }
        if (string.IsNullOrEmpty(FilterEnteredTextPhase) && FilterType != FilterTypeEnum.isNull && FilterType != FilterTypeEnum.isNotNull)
        {
            return true;
        }

        if (typeCode == TypeCode.Int16)
        {
            return FilterEnteredPhaseAsInt16 is not null && GetNumberComparisionResult<Int16>((Int16)columnValue, (Int16)FilterEnteredPhaseAsInt16);
        }
        if (typeCode == TypeCode.Int32)
        {
            return FilterEnteredPhaseAsInt is not null && GetNumberComparisionResult<int>((int)columnValue, (int)FilterEnteredPhaseAsInt);
        }
        if (typeCode == TypeCode.Int64)
        {
            return filterEnteredPhaseAsLong is not null && GetNumberComparisionResult<long>((long)columnValue, (long)filterEnteredPhaseAsLong);
        }
        if (typeCode == TypeCode.Double)
        {
            return FilterEnteredPhaseAsDouble is not null && GetNumberComparisionResult<double>((double)columnValue, (double)FilterEnteredPhaseAsDouble);
        }
        if (typeCode == TypeCode.Decimal)
        {
            return FilterEnteredPhaseAsDecimal is not null && GetNumberComparisionResult<decimal>((decimal)columnValue, (decimal)FilterEnteredPhaseAsDecimal);
        }

        return CompareInFilterText(columnValue.ToString());
    }


    private bool GetNumberComparisionResult<T>(T num1, T num2) where T : INumber<T>
    {
        return FilterType switch
        {
            FilterTypeEnum.equals => num1.Equals(num2),
            FilterTypeEnum.notEquals => !num1.Equals(num2),
            FilterTypeEnum.greaterThan => num1.CompareTo(num2) > 0,
            FilterTypeEnum.greaterOrEqualThan => num1.CompareTo(num2) >= 0,
            FilterTypeEnum.lowerThan => num1.CompareTo(num2) < 0,
            FilterTypeEnum.lowerOrEqualThan => num1.CompareTo(num2) <= 0,
            _ => false,
        };
    }

    private bool CompareInFilterText(string x)
    {
        return CompareInFilterTextStatic(x, FilterEnteredTextPhase, FilterType);
    }

    public static bool CompareInFilterTextStatic(string x, string y, FilterTypeEnum filterType)
    {
        return filterType switch
        {
            FilterTypeEnum.contains => x.Contains(y, StringComparison.OrdinalIgnoreCase),
            FilterTypeEnum.notContains => !x.Contains(y, StringComparison.OrdinalIgnoreCase),
            FilterTypeEnum.equals => x.Equals(y, StringComparison.OrdinalIgnoreCase),
            FilterTypeEnum.notEquals => !x.Equals(y, StringComparison.OrdinalIgnoreCase),
            FilterTypeEnum.startsWith => x.StartsWith(y, StringComparison.OrdinalIgnoreCase),
            FilterTypeEnum.endsWith => x.EndsWith(y, StringComparison.OrdinalIgnoreCase),
            FilterTypeEnum.greaterThan => throw new InvalidCastException(),
            FilterTypeEnum.greaterOrEqualThan => throw new InvalidCastException(),
            FilterTypeEnum.lowerThan => throw new InvalidCastException(),
            FilterTypeEnum.lowerOrEqualThan => throw new InvalidCastException(),
            FilterTypeEnum.isNull => throw new InvalidCastException(),
            FilterTypeEnum.isNotNull => throw new InvalidCastException(),
            _ => throw new InvalidCastException(),
        };
    }

}
