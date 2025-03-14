using System.Collections.Immutable;
using System.Reflection;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DatabaseAnalyzer.Core.Plugins;

internal static class PluginAssemblyLoader
{
    private static readonly string PluginsDirectoryPath = GetPluginsDirectoryPath();

    public static IReadOnlyList<PluginAssembly> LoadPlugins(IServiceCollection services)
    {
        var pluginAssemblies = new List<PluginAssembly>();

        foreach (var assemblyPath in GetPluginAssemblyPaths())
        {
            var pluginAssembly = LoadAssemblyFromPath(services, assemblyPath);
            if (pluginAssembly is null)
            {
                continue;
            }

            pluginAssemblies.Add(pluginAssembly);
        }

        return pluginAssemblies;
    }

    private static PluginAssembly? LoadAssemblyFromPath(IServiceCollection services, string assemblyPath)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope -> registered in the DI container which takes care of it
        var assemblyLoadContext = new PluginLoadContext(assemblyPath);
#pragma warning restore CA2000
        services.AddSingleton(assemblyLoadContext);

        try
        {
            var assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
            var scriptAnalyzerTypes = GetPluginsOfType<IScriptAnalyzer>(assembly).ToImmutableArray();
            var globalAnalyzerTypes = GetPluginsOfType<IGlobalAnalyzer>(assembly).ToImmutableArray();
            var diagnosticSettingsProviderTypes = GetPluginsOfType<IDiagnosticSettingsProvider>(assembly).ToImmutableArray();
            var settingsPairTypes = GetSettingsPairTypes(assembly).ToImmutableArray();
            var diagnosticDefinitions = GetDefinitionsFromAssembly(assembly).ToImmutableArray();

            if (scriptAnalyzerTypes.Length == 0 && globalAnalyzerTypes.Length == 0 && diagnosticSettingsProviderTypes.Length == 0 && diagnosticDefinitions.Length == 0)
            {
                return null;
            }

            return new PluginAssembly(assemblyLoadContext, scriptAnalyzerTypes, globalAnalyzerTypes, diagnosticSettingsProviderTypes, settingsPairTypes, diagnosticDefinitions);
        }
        catch
        {
            assemblyLoadContext.Unload();
            throw;
        }
    }

    private static IEnumerable<Type> GetPluginsOfType<TPlugin>(Assembly assembly)
        => assembly
            .GetTypes()
            .Where(static a =>
            {
                return a is
                {
                    IsAbstract: false,
                    IsClass: true,
                    IsPublic: true
                } && a.GetInterfaces().Any(static x => x == typeof(TPlugin));
            });

    private static string[] GetPluginAssemblyPaths()
    {
        return Directory.Exists(PluginsDirectoryPath)
            ? Directory.GetFiles(PluginsDirectoryPath, "*.dll", SearchOption.AllDirectories)
            : [];
    }

    private static string GetPluginsDirectoryPath()
    {
        var currentDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? throw new InvalidOperationException("Unable to determine application directory.");
        return Path.Combine(currentDirectory, "plugins");
    }

    private static List<SettingsPairTypes> GetSettingsPairTypes(Assembly assembly)
    {
        var rawSettingsTypes = assembly
            .GetTypes()
            .Where(a => !a.IsAbstract)
            .Where(a => a.GetInterfaces().Any(b => b.IsGenericType && b.GetGenericTypeDefinition() == typeof(IRawSettings<>)))
            .Select(a =>
            (
                RawSettignsType: a,
                SettingsType: a
                    .GetInterfaces()
                    .Single(b => b.IsGenericType && b.GetGenericTypeDefinition() == typeof(IRawSettings<>))
                    .GetGenericArguments()[0]
            ))
            .ToList();

        var settingsTypes = assembly
            .GetTypes()
            .Where(a => !a.IsAbstract)
            .Where(a => a.GetInterfaces().Any(b => b.IsGenericType && b.GetGenericTypeDefinition() == typeof(ISettings<>)))
            .ToHashSet();

        var result = new List<SettingsPairTypes>();

        foreach (var (rawSettingsType, settingsType) in rawSettingsTypes)
        {
            if (settingsTypes.Contains(settingsType)) // to make sure it's not abstract etc.
            {
                result.Add(new SettingsPairTypes(rawSettingsType, settingsType));
            }
        }

        return result;
    }

    private static IEnumerable<IDiagnosticDefinition> GetDefinitionsFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var definition in GetDiagnosticDefinitionsFromType(type))
            {
                if (!definition.DiagnosticId.StartsWith("AJ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return definition;
            }
        }
    }

    private static IEnumerable<IDiagnosticDefinition> GetDiagnosticDefinitionsFromType(Type type)
    {
        var properties = type
            .GetProperties()
            .Where(a => a.CanRead)
            .Where(a => a.GetMethod?.IsPublic ?? false)
            .Where(a => a.GetMethod?.IsStatic ?? false)
            .Where(a => a.PropertyType.IsAssignableTo(typeof(IDiagnosticDefinition)));

        foreach (var property in properties)
        {
            var value = property.GetValue(null);
            if (value is IDiagnosticDefinition definition)
            {
                yield return definition;
            }
        }

        foreach (var nestedType in type.GetNestedTypes())
        {
            foreach (var nestedDefinition in GetDiagnosticDefinitionsFromType(nestedType))
            {
                yield return nestedDefinition;
            }
        }
    }
}
