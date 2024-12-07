using JustyBase.PluginCommon.Enums;
using JustyBase.PluginDatabaseBase.Database;
using System.Reflection;

namespace JustyBase.Common.Helpers;

public static class PluginLoadHelper
{
    public static void InstallSpecificDatabaseService(Assembly pluginAssembly)
    {
        foreach (Type type in pluginAssembly.GetTypes())
        {
            if (typeof(IDatabaseService).IsAssignableFrom(type))
            {
                var activatorFunc = (string userName, string password, string port, string ip, string db, int connectionTimeout) 
                    => (Activator.CreateInstance(type, userName, password, "5480", ip, db, connectionTimeout) as IDatabaseService);
                var WHO_I_AM_CONST_FIELD = type.GetField(nameof(IDatabaseService.WHO_I_AM_CONST));
                DatabaseTypeEnum databaseType = (DatabaseTypeEnum)WHO_I_AM_CONST_FIELD.GetValue(null);
                DatabaseServiceHelpers.AddDatabaseImplementation(databaseType, activatorFunc);
            }
        }
    }

    public static Assembly LoadPlugin(string pluginLocation)
    {
        PluginLoadContext loadContext = new PluginLoadContext(pluginLocation);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
    }

    public static bool PluginWasLoaded = false;
    private static readonly Lock _pluginLock = new Lock();

    public static void LoadPlugins(string pluginsLocation)
    {
        lock (_pluginLock)
        {
#if DEBUG
            string[] files = [
                @$"{pluginsLocation}NetezzaDotnetPlugin\bin\Debug\net9.0\NetezzaDotnetPlugin.dll",
                @$"{pluginsLocation}OraclePlugin\bin\Debug\net9.0\OraclePlugin.dll",
                @$"{pluginsLocation}DB2Plugin\bin\Debug\net9.0\DB2Plugin.dll",
                @$"{pluginsLocation}PostgresPlugin\bin\Debug\net9.0\PostgresPlugin.dll",
                @$"{pluginsLocation}SqlitePlugin\bin\Debug\net9.0\SqlitePlugin.dll",
                @$"{pluginsLocation}DuckDBPlugin\bin\Debug\net9.0\DuckDBPlugin.dll",
                @$"{pluginsLocation}MySqlPlugin\bin\Debug\net9.0\MySqlPlugin.dll",
                ];
            foreach (var filePath in files)
            {
                var pluginAssembly = PluginLoadHelper.LoadPlugin(filePath);
                PluginLoadHelper.InstallSpecificDatabaseService(pluginAssembly);
            }
            PluginWasLoaded = true;
#else
            foreach (var dir in Directory.GetDirectories(pluginsLocation))
            {
                foreach (var file in Directory.GetFiles(dir, "*Plugin.dll"))
                {
                    var pluginAssembly = PluginLoadHelper.LoadPlugin(file);
                    PluginLoadHelper.InstallSpecificDatabaseService(pluginAssembly);
                }
            }
            PluginWasLoaded = true;
#endif
        }
    }

}
