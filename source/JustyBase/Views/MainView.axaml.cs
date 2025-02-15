using JustyBase.Common.Contracts;
using JustyBase.Editor;
using JustyBase.Themes;

namespace JustyBase.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        InitializeThemes();
    }
    private void InitializeThemes()
    {
        ThemeButton.Click += (_, _) => ChangeTheme();
    }
    private static IGeneralApplicationData _generalApplicationData;
    private static IThemeManager _themeManager;

    public static void ChangeTheme()
    {
        _generalApplicationData ??= App.GetRequiredService<IGeneralApplicationData>();
        _themeManager ??= App.GetRequiredService<IThemeManager>();
        _generalApplicationData.Config.ThemeNum = 1 - _generalApplicationData.Config.ThemeNum;
        _themeManager?.Switch(FluentThemeManager.IsDark ? 1 : 0);
        SqlCodeEditorHelpers.ResetStyle(FluentThemeManager.IsDark);
    }
}
