using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace JustyBase.PluginDatabaseBase;

sealed class PluginLoadContext(string pluginPath) : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver = new AssemblyDependencyResolver(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath is not null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return nint.Zero;
    }
}