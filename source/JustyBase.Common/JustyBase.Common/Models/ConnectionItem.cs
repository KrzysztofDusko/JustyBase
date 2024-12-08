using JustyBase.PluginCommon.Enums;
using System.Collections.ObjectModel;

namespace JustyBase.Services.Database;

public sealed class ConnectionItem
{
    public string Name { get; set; }
    public DatabaseTypeEnum DatabaseType { get; set; }
    public required ObservableCollection<string> DatabaseList { get; set; }
    public required string DefaultDatabase { get; set; }
    public ConnectionItem(string name, DatabaseTypeEnum dbType)
    {
        Name = name;
        DatabaseType = dbType;
    }
}
