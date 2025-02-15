using JustyBase.ViewModels;
using System;

namespace JustyBase.Views;

public partial class SqlParametrWindow : Window
{
    public SqlParametrWindow()
    {
        InitializeComponent();
        this.DataContextChanged += SqlParametrWindow_DataContextChanged;
        this.Activated += SqlParametrWindow_Activated;
    }

    private void SqlParametrWindow_DataContextChanged(object? sender, EventArgs e)
    {
        (this.DataContext as SqlParametrViewModel).CloseAction = () => this.Close();
    }

    private void SqlParametrWindow_Activated(object? sender, EventArgs e)
    {
        dg.Focus();
        dg.SelectedIndex = 0;
        dg.CurrentColumn = dg.Columns[1];
        dg.BeginEdit();
    }

    private void TextBox_Initialized(object? sender, System.EventArgs e)
    {
        TextBox tb = (TextBox)sender;
        tb.Focus();
        tb.SelectAll();
    }
}