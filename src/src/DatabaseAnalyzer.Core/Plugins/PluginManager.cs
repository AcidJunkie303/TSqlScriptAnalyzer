using System.Collections.Immutable;
using System.Reflection;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core.Plugins;

internal sealed class PluginManager : IDisposable
{
    private static readonly string PluginsDirectoryPath = GetPluginsDirectoryPath();
    private readonly List<PluginAssembly> _plugins = [];
    private bool _disposed;

    public ImmutableArray<PluginAssembly> PluginAssemblies { get; set; } = [];

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.AssemblyLoadContext.Unload();
            }
#pragma warning disable S108, CA1031, S2486 // for cleanup, we ignore all of them
            catch
            {
                // ignored
            }
#pragma warning restore S108, CA1031, S2486
        }

        _disposed = true;
    }

    public void LoadPlugins()
    {
        foreach (var assemblyPath in GetPluginAssemblyPaths())
        {
            LoadAssemblyFromPath(assemblyPath);
        }

        PluginAssemblies = [.. _plugins];
    }

    private void LoadAssemblyFromPath(string assemblyPath)
    {
        var assemblyLoadContext = new PluginLoadContext(assemblyPath);
        var assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
        var scriptAnalyzerTypes = GetPluginsOfType<IScriptAnalyzer>(assembly).ToImmutableArray();
        var globalAnalyzerTypes = GetPluginsOfType<IGlobalAnalyzer>(assembly).ToImmutableArray();
        var diagnosticSettingsProviderTypes = GetPluginsOfType<IDiagnosticSettingsProvider>(assembly).ToImmutableArray();

        if (scriptAnalyzerTypes.Length == 0 && globalAnalyzerTypes.Length == 0 && diagnosticSettingsProviderTypes.Length == 0)
        {
            return;
        }

        var pluginAssembly = new PluginAssembly(assemblyLoadContext, scriptAnalyzerTypes, globalAnalyzerTypes, diagnosticSettingsProviderTypes);
        _plugins.Add(pluginAssembly);
    }

    private static IEnumerable<Type> GetPluginsOfType<TPlugin>(Assembly assembly)
        => assembly
            .GetTypes()
            .Where(a =>
            {
                return a is
                {
                    IsAbstract: false,
                    IsClass: true,
                    IsPublic: true
                } && a.GetInterfaces().Any(x => x == typeof(TPlugin));
            });

    private static string[] GetPluginAssemblyPaths()
    {
        return Directory.Exists(PluginsDirectoryPath)
            ? Directory.GetFiles(PluginsDirectoryPath, "*.dll", SearchOption.AllDirectories)
            : [];
    }

    private static string GetPluginsDirectoryPath()
    {
        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Unable to determine application directory.");
        return Path.Combine(currentDirectory, "plugins");
    }
}
