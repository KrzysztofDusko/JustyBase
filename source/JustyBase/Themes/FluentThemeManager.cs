using Avalonia.Themes.Fluent;
using JustyBase.Common.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JustyBase.Themes;

public class FluentThemeManager : IThemeManager
{
    private static readonly Color AccentLightStandard = Avalonia.Media.Color.Parse("#0078D4");
    private static readonly Color RegionLightStandard = Avalonia.Media.Color.Parse("#FBFBFB");
    private static readonly Color AltHighLightStandard = Avalonia.Media.Colors.White;
    private static readonly Color AltMediumLowLightStandard = Avalonia.Media.Color.Parse("#EFEFEF"); // TextControlBackground 

    private static readonly Color AccentDarkStandard = Avalonia.Media.Color.Parse("#0078D4");
    private static readonly Color RegionDarkStandard = Avalonia.Media.Color.Parse("#202020");
    private static readonly Color AltHighDarkStandard = Avalonia.Media.Colors.Black;
    private static readonly Color AltMediumLowDarkStandard = Avalonia.Media.Color.Parse("#2F2F2F"); // TextControlBackground 

    
    static FluentThemeManager()
    {
        ColorPaletteResources paletteLight;
        ColorPaletteResources paletteDark;
        if (File.Exists(IGeneralApplicationData.ColorsPath))
        {
            PaletteDTO paletteDTO = JsonSerializer.Deserialize(File.ReadAllText(IGeneralApplicationData.ColorsPath), MyJsonContextPaletteDTO.Default.PaletteDTO);
            paletteLight = paletteDTO.GetColorPaletteResources();
            paletteDark = paletteDTO.GetColorPaletteResources();
        }
        else// StandardPath
        {
            paletteLight = new ColorPaletteResources()
            {
                Accent = AccentLightStandard,
                RegionColor = RegionLightStandard,
                AltHigh = AltHighLightStandard,
                AltMediumLow = AltMediumLowLightStandard
            };

            paletteDark = new ColorPaletteResources()
            {
                Accent = AccentDarkStandard,
                RegionColor = RegionDarkStandard,
                AltHigh = AltHighDarkStandard,
                AltMediumLow = AltMediumLowDarkStandard
            };
        }

        FluentPlain = new()
        {
            Palettes =
            {
                [ThemeVariant.Light] = paletteLight,
                [ThemeVariant.Dark] = paletteDark,
            }
        };
    }

    public static FluentThemeManager StaticFluentThemeManager { get; set; }
    public FluentThemeManager()
    {
        StaticFluentThemeManager = this;
    }

    private static IGeneralApplicationData _generalApplicationDataCached;
    private static IGeneralApplicationData GetgeneralAppData => _generalApplicationDataCached ??= App.GetRequiredService<IGeneralApplicationData>();

    private static FluentTheme FluentPlain;

    //https://www.color-meanings.com/shades-of-pink-color-names-html-hex-rgb-codes/
    private static FluentTheme FluentValentine;

    public static ColorPaletteResources GetCurrentPalette()
    {
        var variant = Application.Current.RequestedThemeVariant;
        return FluentPlain.Palettes[variant];
    }

    private static FluentTheme GetFluentBase
    {
        get
        {
            if (JustyBase.SplashWindow.IsValentine())
            {
                FluentValentine ??= new()
                {
                    Palettes =
                    {
                        [ThemeVariant.Light] = new ColorPaletteResources()
                        {
                            Accent = Avalonia.Media.Color.Parse("#FF007F"), // Bright Pink
                            //RegionColor = Avalonia.Media.Color.Parse("#FFBCD9"),  // Cotton Candy 
                            //AltHigh = Avalonia.Media.Color.Parse("#FFBCD9"), // Cotton Candy 
                            //AltMediumLow= Avalonia.Media.Color.Parse("#FEC5E5") // Blush
                        },
                        [ThemeVariant.Dark] = new ColorPaletteResources()
                        {
                            Accent = Avalonia.Media.Color.Parse("#E52B50"),
                            //RegionColor = Avalonia.Media.Color.Parse("#C32148"),  //Bright Maroon
                            //AltHigh = Avalonia.Media.Color.Parse("#C32148"),  //Bright Maroon
                            //AltMediumLow = Avalonia.Media.Color.Parse("#E25278") // Punch
                        },
                    }
                };
                return FluentValentine;
            }
            return FluentPlain;
        }
    }

    //private static readonly Styles SimpleAccentsLight = 
    //    AvaloniaXamlLoader.Load(new Uri("avares://JustyBase/Themes/SimpleAccentsLight.axaml")) as Styles;

    //private static readonly Styles SimpleAccentsDark = 
    //    AvaloniaXamlLoader.Load(new Uri("avares://JustyBase/Themes/SimpleAccentsDark.axaml")) as Styles;


    public static bool IsLight => GetgeneralAppData.Config.ThemeNum == 0;
    public static bool IsDark => !IsLight;


    public void Initialize(Application application)
    {
        ApplyFontSizes();
        application.RequestedThemeVariant = IsLight ? ThemeVariant.Light : ThemeVariant.Dark;
        application.Styles.Insert(0, GetFluentBase);


        if (GetgeneralAppData.Config.ThemeNum == 0)
        {
            application.Resources["DockApplicationAccentBrushLow"] = Application.Current.FindResource("SystemAccentColorLight1");
            application.Resources["DockApplicationAccentBrushMed"] = Application.Current.FindResource("SystemAccentColorLight2");
            application.Resources["DockApplicationAccentBrushHigh"] = Application.Current.FindResource("SystemAccentColorLight3");
            application.Resources["DockApplicationAccentBrushIndicator"] = Application.Current.FindResource("SystemAccentColorLight1");
        }
        else
        {
            application.Resources["DockApplicationAccentBrushLow"] = Application.Current.FindResource("SystemAccentColorDark1");
            application.Resources["DockApplicationAccentBrushMed"] = Application.Current.FindResource("SystemAccentColorDark2");
            application.Resources["DockApplicationAccentBrushHigh"] = Application.Current.FindResource("SystemAccentColorDark3");
            application.Resources["DockApplicationAccentBrushIndicator"] = Application.Current.FindResource("SystemAccentColorDark1");
        }
    }
    public static void ApplyFontSizes()
    {
        Application.Current.Resources["ControlContentThemeFontSize"] = GetgeneralAppData.Config.ControlContentThemeFontSize;
        Application.Current.Resources["CompletitionFontSize"] = GetgeneralAppData.Config.CompletitionFontSize;
    }

    public static void MakeFontsBigger()
    {
        GetgeneralAppData.Config.ControlContentThemeFontSize++;
        GetgeneralAppData.Config.CompletitionFontSize++;
        GetgeneralAppData.Config.DefaultFontSizeForDocuments = (int)Math.Floor(GetgeneralAppData.Config.CompletitionFontSize);
        ApplyFontSizes();
    }
    public static void MakeFontsSmaller()
    {
        GetgeneralAppData.Config.ControlContentThemeFontSize--;
        GetgeneralAppData.Config.CompletitionFontSize--;
        GetgeneralAppData.Config.DefaultFontSizeForDocuments = (int)Math.Floor(GetgeneralAppData.Config.CompletitionFontSize);
        ApplyFontSizes();
    }
    public static void MakeFontsDefault()
    {
        GetgeneralAppData.Config.ControlContentThemeFontSize = 12;
        GetgeneralAppData.Config.CompletitionFontSize = 13;
        GetgeneralAppData.Config.DefaultFontSizeForDocuments = (int)Math.Floor(GetgeneralAppData.Config.CompletitionFontSize);
        ApplyFontSizes();
    }

    public void Switch(int index, ColorPaletteResources? pal = null)
    {
        if (Application.Current is null)
        {
            return;
        }

        if (index == -1)
        {
            index = GetgeneralAppData.Config.ThemeNum;

            var paletteLight = new ColorPaletteResources()
            {
                Accent = pal.Accent,
                AltHigh = pal.AltHigh,
                AltLow = pal.AltLow,
                AltMedium = pal.AltMedium,
                AltMediumHigh = pal.AltMediumHigh,
                AltMediumLow = pal.AltMediumLow,
                BaseHigh = pal.BaseHigh,
                BaseLow = pal.BaseLow,
                BaseMedium = pal.BaseMedium,
                BaseMediumHigh = pal.BaseMediumHigh,
                BaseMediumLow = pal.BaseMediumLow,
                ChromeBlackHigh = pal.ChromeBlackHigh,
                ChromeBlackLow = pal.ChromeBlackLow,
                ChromeBlackMedium = pal.ChromeBlackMedium,
                ChromeBlackMediumLow = pal.ChromeBlackMediumLow,
                ChromeDisabledHigh = pal.ChromeDisabledHigh,
                ChromeDisabledLow = pal.ChromeDisabledLow,
                ChromeGray = pal.ChromeGray,
                ChromeHigh = pal.ChromeHigh,
                ChromeLow = pal.ChromeLow,
                ChromeMedium = pal.ChromeMedium,
                ChromeMediumLow = pal.ChromeMediumLow,
                ChromeWhite = pal.ChromeWhite,
                ListLow = pal.ListLow,
                ListMedium = pal.ListMedium,
                RegionColor = pal.RegionColor,
            };

            var paletteDark = new ColorPaletteResources()
            {
                Accent = pal.Accent,
                AltHigh = pal.AltHigh,
                AltLow = pal.AltLow,
                AltMedium = pal.AltMedium,
                AltMediumHigh = pal.AltMediumHigh,
                AltMediumLow = pal.AltMediumLow,
                BaseHigh = pal.BaseHigh,
                BaseLow = pal.BaseLow,
                BaseMedium = pal.BaseMedium,
                BaseMediumHigh = pal.BaseMediumHigh,
                BaseMediumLow = pal.BaseMediumLow,
                ChromeBlackHigh = pal.ChromeBlackHigh,
                ChromeBlackLow = pal.ChromeBlackLow,
                ChromeBlackMedium = pal.ChromeBlackMedium,
                ChromeBlackMediumLow = pal.ChromeBlackMediumLow,
                ChromeDisabledHigh = pal.ChromeDisabledHigh,
                ChromeDisabledLow = pal.ChromeDisabledLow,
                ChromeGray = pal.ChromeGray,
                ChromeHigh = pal.ChromeHigh,
                ChromeLow = pal.ChromeLow,
                ChromeMedium = pal.ChromeMedium,
                ChromeMediumLow = pal.ChromeMediumLow,
                ChromeWhite = pal.ChromeWhite,
                ListLow = pal.ListLow,
                ListMedium = pal.ListMedium,
                RegionColor = pal.RegionColor,
            };
            if (JustyBase.SplashWindow.IsValentine())
            {
                FluentValentine = new()
                {
                    Palettes =
                    {
                        [ThemeVariant.Light] = paletteLight,
                        [ThemeVariant.Dark] = paletteDark,
                    }
                };
            }
            else
            {
                FluentPlain = new()
                {
                    Palettes =
                    {
                        [ThemeVariant.Light] = paletteLight,
                        [ThemeVariant.Dark] = paletteDark,
                    }
                };
            }
        }

        Application.Current.RequestedThemeVariant = index == 0 ? ThemeVariant.Light : ThemeVariant.Dark;
        Application.Current.Styles[0] = GetFluentBase;

        if (GetgeneralAppData.Config.ThemeNum == 0)
        {
            Application.Current.Resources["DockApplicationAccentBrushLow"] = Application.Current.FindResource("SystemAccentColorLight1");
            Application.Current.Resources["DockApplicationAccentBrushMed"] = Application.Current.FindResource("SystemAccentColorLight2");
            Application.Current.Resources["DockApplicationAccentBrushHigh"] = Application.Current.FindResource("SystemAccentColorLight3");
            Application.Current.Resources["DockApplicationAccentBrushIndicator"] = Application.Current.FindResource("SystemAccentColorLight1");
        }
        else
        {
            Application.Current.Resources["DockApplicationAccentBrushLow"] = Application.Current.FindResource("SystemAccentColorDark1");
            Application.Current.Resources["DockApplicationAccentBrushMed"] = Application.Current.FindResource("SystemAccentColorDark2");
            Application.Current.Resources["DockApplicationAccentBrushHigh"] = Application.Current.FindResource("SystemAccentColorDark3");
            Application.Current.Resources["DockApplicationAccentBrushIndicator"] = Application.Current.FindResource("SystemAccentColorDark1");
        }
    }
}

//SimpleAccentsDark
//<SolidColorBrush x:Key="DockApplicationAccentBrushLow" Color="{DynamicResource SystemAccentColorDark1}" />
//<SolidColorBrush x:Key="DockApplicationAccentBrushMed" Color="{DynamicResource SystemAccentColorDark2}" />
//<SolidColorBrush x:Key="DockApplicationAccentBrushHigh" Color="{DynamicResource SystemAccentColorDark3}" />
//<SolidColorBrush x:Key="DockApplicationAccentBrushIndicator" Color="{DynamicResource SystemAccentColorDark1}" />

//SimpleAccentsLight
//<SolidColorBrush x:Key="DockApplicationAccentBrushLow" Color="{DynamicResource SystemAccentColorLight1}" />
//<SolidColorBrush x:Key="DockApplicationAccentBrushMed" Color="{DynamicResource SystemAccentColorLight2}" />
//<SolidColorBrush x:Key="DockApplicationAccentBrushHigh" Color="{DynamicResource SystemAccentColorLight3}" />
//<SolidColorBrush x:Key="DockApplicationAccentBrushIndicator" Color="{DynamicResource SystemAccentColorLight1}" />
