using CommunityToolkit.Mvvm.ComponentModel;

namespace JustyBase.Shared.Models;

public sealed class MenuItemForCurrentOptions : ObservableObject
{
    public string OptionHeader { get; set; }

    private ICommand _optionCommand;
    public ICommand OptionCommand
    {
        get => _optionCommand;
        set => SetProperty(ref _optionCommand, value);
    }
}
