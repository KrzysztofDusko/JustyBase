using Avalonia.Themes.Fluent;

namespace JustyBase.Themes;

public interface IThemeManager
{
    void Initialize(Application application);

    void Switch(int index, ColorPaletteResources? pal = null);
}
