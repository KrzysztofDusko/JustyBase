using System.Globalization;

namespace JustyBase.PluginCommon.Contracts;

public static class ImportEssentials
{
    public const int NumericPrecision = 6;

    public static readonly NumberFormatInfo NUMBER_WITH_DOT_FORMAT = new()
    {
        NumberDecimalSeparator = ".",
        NumberDecimalDigits = NumericPrecision
    };

    public const NumberStyles NumberExcelStyle = NumberStyles.Number | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowExponent;
}