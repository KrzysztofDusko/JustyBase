using JustyBase.PluginCommon.Models;

namespace JustyBase.PluginCommon.Contracts;

public interface IDatabaseInfo
{
    Task LoadPluginsIfNeeded(Action? uiAction);
    ISimpleLogger GlobalLoggerObject { get; }
    Dictionary<string, LoginDataModel> LoginDataDic { get; }
    string GetDataDir();
}