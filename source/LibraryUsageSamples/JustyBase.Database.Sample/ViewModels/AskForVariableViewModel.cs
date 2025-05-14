using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace JustyBase.Database.Sample.ViewModels
{
    public partial class AskForVariableViewModel : ViewModelBase
    {
        [ObservableProperty]
        public partial string VariableValue { get; set; }

        [ObservableProperty]
        public partial string VariableName { get; set; }
       
        public event EventHandler? CloseRequested;

        [RelayCommand]
        private void OnOk()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void OnCancel()
        {
            VariableValue = string.Empty;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}