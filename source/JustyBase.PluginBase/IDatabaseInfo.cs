using JustyBase.PluginCommon.Contracts;
using PluginDatabaseBase.Models;

namespace JustyBase.PluginDatabaseBase;

public interface IDatabaseInfo
{
    Task LoadPluginsIfNeeded(Action? uiAction);
    ISimpleLogger GlobalLoggerObject { get; }
    Dictionary<string, LoginDataModel> LoginDataDic { get; }
    string GetDataDir();
}