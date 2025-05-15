using Avalonia.Controls;
using JustyBase.Database.Sample.ViewModels;
using System;

namespace JustyBase.Database.Sample;

public partial class AskForVariable : Window
{
    private readonly AskForVariableViewModel _viewModel;

    public AskForVariable()
    {
        InitializeComponent();
        _viewModel = new AskForVariableViewModel();
        DataContext = _viewModel;
        this.DataContextChanged += AskForVariable_DataContextChanged;
    }

    private void AskForVariable_DataContextChanged(object? sender, System.EventArgs e)
    {
        (this.DataContext as AskForVariableViewModel).CloseRequested += (s, e) =>
        {
            Close();
        };
    }

    private void Window_Opened(object? sender, EventArgs e)
    {
        tbVariable.Focus();
    }

    public string? VariableValue => _viewModel.VariableValue;
}