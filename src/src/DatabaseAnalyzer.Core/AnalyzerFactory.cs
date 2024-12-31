using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Services;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Plugins;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DatabaseAnalyzer.Core;

public static class AnalyzerFactory
{
    public static IAnalyzer Create(IConfiguration configuration, ApplicationSettings settings, IProgressCallback? progressCallback = null)
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
                services.AddSingleton<IScriptSourceProvider, ScriptSourceSourceProvider>();
                services.AddSingleton<IDiagnosticSuppressionExtractor, DiagnosticSuppressionExtractor>();

                var pluginAssemblies = PluginAssemblyLoader.LoadPlugins(services);
                RegisterPlugins(services, pluginAssemblies);
                RegisterDiagnosticsSettingsProvider(services, configuration, pluginAssemblies);
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
