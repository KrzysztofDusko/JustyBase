using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace JustyBase.Shared.Models;

public sealed class MenuItemForCurrentOptions : ObservableObject
{
    public required string OptionHeader { get; set; }

    private ICommand _optionCommand;
    public required ICommand OptionCommand
    {
        get => _optionCommand;
        set => SetProperty(ref _optionCommand, value);
    }
}
