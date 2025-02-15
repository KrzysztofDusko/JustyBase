using Avalonia.Data;
using JustyBase.ViewModels.Documents;

namespace JustyBase.Views.Documents;
public partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();
        this.DataContextChanged += ImportView_DataContextChanged;
    }
    private void ImportView_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (this.DataContext is ImportViewModel vm)
        {
            vm.ActionFromView ??= ActionFromViewBase;
        }
    }

    private void ActionFromViewBase(string[] headers)
    {
        if (this.DataContext is ImportViewModel vm)
        {
            previewDataGrid.ItemsSource = vm.PreviewRows;
            for (var i = 0; i < headers.Length; ++i)
            {
                int index = i;
                var bb = new Binding($"[{index}]", BindingMode.OneWay);
                DataGridBoundColumn col = new DataGridTextColumn()
                {
                    Header = headers[index],
                    MaxWidth = 200,
                    Binding = bb,
                    Width = DataGridLength.Auto,
                    IsReadOnly = true,
                };
                previewDataGrid.Columns.Add(col);
            }
        }
    }
}
