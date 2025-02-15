using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using JustyBase.Common.Contracts;
using System.Linq;

namespace JustyBase.ViewModels.Tools;

public sealed partial class AddNewConnectionViewModel : Tool
{
    public AddNewConnectionViewModel(IFactory factory, IGeneralApplicationData generalApplicationData)
    {
        _generalApplicationData = generalApplicationData;
        this.Factory = factory;
        AddNewCommand = new RelayCommand(AddNew);
        DeleteCommand = new RelayCommand(Delete);
        RefreshConnectionsCommand = new RelayCommand(RefreshConnections);
        CloneConnectionCommand = new RelayCommand(CloneConnection);
    }

    private void RefreshConnections()
    {
        OnPropertyChanged(nameof(ConnectionList));
        DbSchemaViewModel? dbChemaViewModel = Factory.Find(a => a is DbSchemaViewModel).FirstOrDefault() as DbSchemaViewModel;
        dbChemaViewModel?.ResedConnectionList();
    }

    public string Pass
    {
        get;
        set => SetProperty(ref field, value);
    }
}
