using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Plugins;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DatabaseAnalyzer.Core;

public static class AnalyzerFactory
{
    public static IAnalyzer Create(ApplicationSettings settings, IProgressCallback? progressCallback = null)
    {
        var host = CreateHostBuilder(settings, progressCallback).Build();
        return host.Services.GetRequiredService<IAnalyzer>();
    }

    private static IHostBuilder CreateHostBuilder(ApplicationSettings settings, IProgressCallback? progressCallback)
        => Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(settings);
                services.AddSingleton(settings.Diagnostics);
                services.AddSingleton(settings.ScriptSource);
                services.AddSingleton(progressCallback ?? new NullProgressWriter());

                services.AddSingleton<IAnalyzer, Analyzer>();
                services.AddSingleton<IScriptLoader, ScriptLoader>();
                services.AddSingleton<IScriptSourceProvider, ScriptSourceSourceProvider>();

                RegisterPlugins(services);
            });

    private static void RegisterPlugins(IServiceCollection services)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope -> done via DI container or catch block below
        var pluginManager = new PluginManager();
#pragma warning restore CA2000
        try
        {
            services.AddSingleton(pluginManager);
            pluginManager.LoadPlugins();

            foreach (var pluginAssembly in pluginManager.PluginAssemblies)
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
            }
        }
        catch
        {
            pluginManager.Dispose();
            throw;
        }
    }
}
