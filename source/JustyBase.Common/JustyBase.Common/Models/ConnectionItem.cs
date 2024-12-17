using JustyBase.PluginCommon.Enums;
using System.Collections.ObjectModel;

namespace JustyBase.Common.Models;

public sealed class ConnectionItem(string name, DatabaseTypeEnum dbType)
{
    public string Name { get; set; } = name;
    public DatabaseTypeEnum DatabaseType { get; set; } = dbType;
    public required ObservableCollection<string> DatabaseList { get; set; }
    public required string DefaultDatabase { get; set; }
}
