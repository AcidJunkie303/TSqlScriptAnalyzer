using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Services;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Plugins;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DatabaseAnalyzer.Core;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
public static class AnalyzerFactory
{
    public static IAnalyzer Create(IConfiguration configuration, ApplicationSettings settings) => Create(configuration, settings, null);

    public static IAnalyzer Create(IConfiguration configuration, ApplicationSettings settings, IProgressCallback? progressCallback)
    {
        var host = CreateHostBuilder(configuration, settings, progressCallback).Build();
        return host.Services.GetRequiredService<IAnalyzer>();
    }

    private static IHostBuilder CreateHostBuilder(IConfiguration configuration, ApplicationSettings settings, IProgressCallback? progressCallback)
        => Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(configuration);
                services.AddSingleton(settings);
                services.AddSingleton(settings.Diagnostics);
                services.AddSingleton(settings.ScriptSource);
                services.AddSingleton(progressCallback ?? new NullProgressWriter());

                services.AddSingleton<IAnalyzer, Analyzer>();
                services.AddSingleton<IScriptLoader, ScriptLoader>();
                services.AddSingleton<IScriptSourceProvider, ScriptSourceProvider>();
                services.AddSingleton<IDiagnosticSuppressionExtractor, DiagnosticSuppressionExtractor>();

                var pluginAssemblies = PluginAssemblyLoader.LoadPlugins(services);
                RegisterPlugins(services, pluginAssemblies);
                RegisterDiagnosticsSettingsProvider(services, configuration, pluginAssemblies);
                RegisterDiagnosticDefinitions(services, pluginAssemblies);
            });

    private static void RegisterPlugins(IServiceCollection services, IReadOnlyList<PluginAssembly> pluginAssemblies)
    {
        foreach (var pluginAssembly in pluginAssemblies)
        {
            foreach (var type in pluginAssembly.ScriptAnalyzerTypes)
            {
                services.AddSingleton(typeof(IScriptAnalyzer), type);
            }

            foreach (var type in pluginAssembly.GlobalAnalyzerTypes)
            {
                services.AddSingleton(typeof(IGlobalAnalyzer), type);
            }

            foreach (var type in pluginAssembly.DiagnosticSettingsProviderTypes)
            {
                services.AddSingleton(typeof(IDiagnosticSettingsProvider), type);
            }

            foreach (var pair in pluginAssembly.SettingsTypes)
            {
                services.AddSingleton(pair);
            }
        }
    }

    private static void RegisterDiagnosticDefinitions(IServiceCollection services, IReadOnlyList<PluginAssembly> pluginAssemblies)
    {
        var diagnosticDefinitionsById = pluginAssemblies
            .SelectMany(a => a.DiagnosticDefinitions)
            .ToDictionary(a => a.DiagnosticId, a => a, StringComparer.OrdinalIgnoreCase);

        services.AddSingleton<IReadOnlyDictionary<string, IDiagnosticDefinition>>(diagnosticDefinitionsById);
    }

    private static void RegisterDiagnosticsSettingsProvider(IServiceCollection services, IConfiguration configuration, IReadOnlyList<PluginAssembly> pluginAssemblies)
    {
        var diagnosticsConfigurationSection = configuration.GetSection("Diagnostics");

        var allDiagnosticsSettingsById = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var pluginAssembly in pluginAssemblies)
        {
            var settingsByDiagnosticsId = DiagnosticsSettingsLoader.Load(pluginAssembly.SettingsTypes, diagnosticsConfigurationSection);
            foreach (var (diagnosticId, settings) in settingsByDiagnosticsId)
            {
                allDiagnosticsSettingsById.Add(diagnosticId, settings);
            }
        }

        var provider = new DiagnosticSettingsProvider(allDiagnosticsSettingsById.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
        services.AddSingleton<IDiagnosticSettingsProvider>(provider);
    }
}
