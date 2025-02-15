using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace JustyBase.ViewModels;

public partial class AskForConfirmViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = "abc";

    [ObservableProperty]
    public partial string TextMessage { get; set; } = "xyz";

    public string ResultAsString { get; set; } = "Cancel";

    public Action? CloseAction;
    public Action? AdditionalYesAction;

    [RelayCommand]
    private void ProcessAnswerKeys(string answerName)
    {
        ResultAsString = answerName;
        if (ResultAsString == "Yes")
        {
            AdditionalYesAction?.Invoke();
        }
        CloseAction?.Invoke();
    }
}
