using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustyBase.Common.Contracts;
using JustyBase.Common.Models;
using System.Collections.ObjectModel;

namespace JustyBase.ViewModels;

public sealed partial class SnippetControlViewModel : ObservableObject
{
    private readonly IGeneralApplicationData _generalApplicationData;
    public string Txt { get; set; } = "TXT";
    public ObservableCollection<SnippetModel> SnippetModels { get; set; } = [];

    [ObservableProperty]
    public partial SnippetModel SelectedSnippetModel { get; set; }
    public int SnippetSelectedIndex { get; set; }
    public ICommand AddNewCommand { get; init; }
    public ICommand DeleteCommand { get; init; }
    public ICommand SaveCommand { get; init; }

    public SnippetControlViewModel()
    {
        _generalApplicationData = App.GetRequiredService<IGeneralApplicationData>();
        foreach (var itm in _generalApplicationData.Config.AllSnippets)
        {
            SnippetModels.Add(new SnippetModel() { SnippetType = itm.Value.snippetType, SnippetDesc = itm.Value.Description, SnippetName = itm.Key, SnippetText = itm.Value.Text });
        }
        AddNewCommand = new RelayCommand(() =>
        {
            var snp = new SnippetModel() { SnippetType = SnippetModel.STANDARD_STRING, SnippetName = "<NAME>" };
            int tmpInt = SnippetSelectedIndex > 0 ? SnippetSelectedIndex : 0;
            SnippetModels.Insert(tmpInt, new SnippetModel() { SnippetType = SnippetModel.STANDARD_STRING, SnippetName = "<NAME>" });
            SelectedSnippetModel = snp;
            SnippetSelectedIndex = tmpInt;
        });

        DeleteCommand = new RelayCommand(() =>
        {
            if (SelectedSnippetModel is not null)
            {
                SnippetModels.Remove(SelectedSnippetModel);
            }
        });

        SaveCommand = new RelayCommand(() =>
        {
            _generalApplicationData.Config.AllSnippets.Clear();
            _generalApplicationData.ClearTempSippetsObjects();
            foreach (var item in SnippetModels)
            {
                _generalApplicationData.Config.AllSnippets[item.SnippetName] = (item.SnippetType, item.SnippetDesc, item.SnippetText, item.SnippetName);
            }
        });
    }
}

