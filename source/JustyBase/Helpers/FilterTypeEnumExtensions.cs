using JustyBase.Models;
using System;

namespace JustyBase.Helpers;

public static class FilterTypeEnumExtensions
{
    public static string StringRepresentation(this FilterTypeEnum filterType)
    {
        return filterType switch
        {
            FilterTypeEnum.contains => "contains",
            FilterTypeEnum.notContains => "not contains",
            FilterTypeEnum.equals => "equals",
            FilterTypeEnum.notEquals => "not equals",
            FilterTypeEnum.startsWith => "starts with",
            FilterTypeEnum.endsWith => "ends with",
            FilterTypeEnum.greaterThan => "greater than",
            FilterTypeEnum.greaterOrEqualThan => "greater or equal than",
            FilterTypeEnum.lowerThan => "lower than",
            FilterTypeEnum.lowerOrEqualThan => "lower or equal than",
            FilterTypeEnum.isNull => "is null",
            FilterTypeEnum.isNotNull => "is not null",
            _ => filterType.ToString(),
        };
    }
    public static FilterTypeEnum FilterTypeEnumFromStringRepresentation(this string rep)
    {
        return rep switch
        {
            "contains" => FilterTypeEnum.contains,
            "not contains" => FilterTypeEnum.notContains,
            "equals" => FilterTypeEnum.equals,
            "not equals" => FilterTypeEnum.notEquals,
            "starts with" => FilterTypeEnum.startsWith,
            "ends with" => FilterTypeEnum.endsWith,
            "greater than" => FilterTypeEnum.greaterThan,
            "greater or equal than" => FilterTypeEnum.greaterOrEqualThan,
            "lower than" => FilterTypeEnum.lowerThan,
            "lower or equal than" => FilterTypeEnum.lowerOrEqualThan,
            "is null" => FilterTypeEnum.isNull,
            "is not null" => FilterTypeEnum.isNotNull,
            _ => throw new NotImplementedException(rep),
        };
    }
}

