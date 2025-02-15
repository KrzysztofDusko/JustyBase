using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JustyBase.ViewModels;

public class SqlParametrViewModel : ObservableObject
{
    public ICommand ClickOkCmd { get; }
    public ICommand ClickCancelCmd { get; }

    public Action CloseAction;

    private readonly ObservableCollection<Pair> _myItems = [];
    public ObservableCollection<Pair> MyItems => _myItems;

    public SqlParametrViewModel(List<string> toAsk, Dictionary<string, string> knownParams)
    {
        foreach (var item in toAsk)
        {
            _myItems.Add(new Pair() { Key = item, Value = knownParams[item] });
        }

        ClickOkCmd = new RelayCommand(() =>
        {
            foreach (var item in MyItems)
            {
                knownParams[item.Key] = item.Value;
            }
            CloseAction?.Invoke();
        });

        ClickCancelCmd = new RelayCommand(() =>
        {
            IsCancel = true;
            CloseAction?.Invoke();
        });
    }

    public bool IsCancel { get; set; }
}
public class Pair
{
    public string Key { get; set; }
    public string Value { get; set; }
}