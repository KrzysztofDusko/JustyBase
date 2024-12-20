using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.ViewModels.Documents;

namespace JustyBase.ViewModels;

public sealed class DbObjectQuickMenuViewModel : ObservableObject
{
    public string ObjectTitle { get; set; } = "some title..";
    public ObservableCollection<QuickMenuItem> OptionsList { get; set; }
    public Action CloseAction { get; set; }

    public SqlDocumentViewModel SqlDocVM 
    {
        get;
        set
        {
            field = value;
            OptionsList = new ObservableCollection<QuickMenuItem>([
            new QuickMenuItem("Jump to [F6]", 12, SqlDocVM.JumpToSelectedItem),
            new QuickMenuItem("Select [F7]", 1, SqlDocVM.SelectSelectedItem),
            new QuickMenuItem("Drop", 12, SqlDocVM.DropSelectedItem),
            new QuickMenuItem("Rename", 12, SqlDocVM.RenameSelectedItem),
            new QuickMenuItem("Groom", 6, SqlDocVM.GroomSelectedItem),
            new QuickMenuItem("Recreate", 10, SqlDocVM.RecreateSelectedItem),
            new QuickMenuItem("DDL", 10, SqlDocVM.DdlSelectedItem),
            new QuickMenuItem("Create from", 6, SqlDocVM.CreateFromSelectedItem),
        ]);
        }
    }
}
public class QuickMenuItem(string title, int number, Func<Task> action)
{
    public string Title { get; set; } = title;
    public int Number { get; set; } = number;
    public ICommand DoJob { get; set; } = new AsyncRelayCommand(action);
}