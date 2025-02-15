using Avalonia.Themes.Fluent;
using System.Text.Json.Serialization;

namespace JustyBase.Themes;

public sealed class PaletteDTO
{
    public bool HasResources { get; set; }
    public ColorInfo Accent { get; set; }
    public ColorInfo AltHigh { get; set; }
    public ColorInfo AltLow { get; set; }
    public ColorInfo AltMedium { get; set; }
    public ColorInfo AltMediumHigh { get; set; }
    public ColorInfo AltMediumLow { get; set; }
    public ColorInfo BaseHigh { get; set; }
    public ColorInfo BaseLow { get; set; }
    public ColorInfo BaseMedium { get; set; }
    public ColorInfo BaseMediumHigh { get; set; }
    public ColorInfo BaseMediumLow { get; set; }
    public ColorInfo ChromeAltLow { get; set; }
    public ColorInfo ChromeBlackHigh { get; set; }
    public ColorInfo ChromeBlackLow { get; set; }
    public ColorInfo ChromeBlackMedium { get; set; }
    public ColorInfo ChromeBlackMediumLow { get; set; }
    public ColorInfo ChromeDisabledHigh { get; set; }
    public ColorInfo ChromeDisabledLow { get; set; }
    public ColorInfo ChromeGray { get; set; }
    public ColorInfo ChromeHigh { get; set; }
    public ColorInfo ChromeLow { get; set; }
    public ColorInfo ChromeMedium { get; set; }
    public ColorInfo ChromeMediumLow { get; set; }
    public ColorInfo ChromeWhite { get; set; }
    public ColorInfo ErrorText { get; set; }
    public ColorInfo ListLow { get; set; }
    public ColorInfo ListMedium { get; set; }
    public ColorInfo RegionColor { get; set; }

    public ColorPaletteResources GetColorPaletteResources()
    {
        ColorPaletteResources pal = new ColorPaletteResources
        {
            Accent = new Color(Accent.A, Accent.R, Accent.G, Accent.B),
            AltHigh = new Color(AltHigh.A, AltHigh.R, AltHigh.G, AltHigh.B),
            AltLow = new Color(AltLow.A, AltLow.R, AltLow.G, AltLow.B),
            AltMedium = new Color(AltMedium.A, AltMedium.R, AltMedium.G, AltMedium.B),
            AltMediumHigh = new Color(AltMediumHigh.A, AltMediumHigh.R, AltMediumHigh.G, AltMediumHigh.B),
            AltMediumLow = new Color(AltMediumLow.A, AltMediumLow.R, AltMediumLow.G, AltMediumLow.B),
            BaseHigh = new Color(BaseHigh.A, BaseHigh.R, BaseHigh.G, BaseHigh.B),
            BaseLow = new Color(BaseLow.A, BaseLow.R, BaseLow.G, BaseLow.B),
            BaseMedium = new Color(BaseMedium.A, BaseMedium.R, BaseMedium.G, BaseMedium.B),
            BaseMediumHigh = new Color(BaseMediumHigh.A, BaseMediumHigh.R, BaseMediumHigh.G, BaseMediumHigh.B),
            BaseMediumLow = new Color(BaseMediumLow.A, BaseMediumLow.R, BaseMediumLow.G, BaseMediumLow.B),
            ChromeBlackHigh = new Color(ChromeBlackHigh.A, ChromeBlackHigh.R, ChromeBlackHigh.G, ChromeBlackHigh.B),
            ChromeBlackLow = new Color(ChromeBlackLow.A, ChromeBlackLow.R, ChromeBlackLow.G, ChromeBlackLow.B),
            ChromeBlackMedium = new Color(ChromeBlackMedium.A, ChromeBlackMedium.R, ChromeBlackMedium.G, ChromeBlackMedium.B),
            ChromeBlackMediumLow = new Color(ChromeBlackMediumLow.A, ChromeBlackMediumLow.R, ChromeBlackMediumLow.G, ChromeBlackMediumLow.B),
            ChromeDisabledHigh = new Color(ChromeDisabledHigh.A, ChromeDisabledHigh.R, ChromeDisabledHigh.G, ChromeDisabledHigh.B),
            ChromeDisabledLow = new Color(ChromeDisabledLow.A, ChromeDisabledLow.R, ChromeDisabledLow.G, ChromeDisabledLow.B),
            ChromeGray = new Color(ChromeGray.A, ChromeGray.R, ChromeGray.G, ChromeGray.B),
            ChromeHigh = new Color(ChromeHigh.A, ChromeHigh.R, ChromeHigh.G, ChromeHigh.B),
            ChromeLow = new Color(ChromeLow.A, ChromeLow.R, ChromeLow.G, ChromeLow.B),
            ChromeMedium = new Color(ChromeMedium.A, ChromeMedium.R, ChromeMedium.G, ChromeMedium.B),
            ChromeMediumLow = new Color(ChromeMediumLow.A, ChromeMediumLow.R, ChromeMediumLow.G, ChromeMediumLow.B),
            ChromeWhite = new Color(ChromeWhite.A, ChromeWhite.R, ChromeWhite.G, ChromeWhite.B),
            ListLow = new Color(ListLow.A, ListLow.R, ListLow.G, ListLow.B),
            ListMedium = new Color(ListMedium.A, ListMedium.R, ListMedium.G, ListMedium.B),
            RegionColor = new Color(RegionColor.A, RegionColor.R, RegionColor.G, RegionColor.B)
        };

        return pal;
    }

}
[JsonSerializable(typeof(PaletteDTO))]
public partial class MyJsonContextPaletteDTO : JsonSerializerContext
{
}

[JsonSerializable(typeof(ColorPaletteResources))]
public partial class MyJsonContextColorPaletteResources : JsonSerializerContext
{
}




public sealed class ColorInfo
{
    public byte A { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
}

[JsonSerializable(typeof(ColorInfo))]
public partial class MyJsonContextColorInfo : JsonSerializerContext
{
}