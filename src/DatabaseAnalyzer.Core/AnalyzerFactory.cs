using DatabaseAnalyzer.Core.Configuration;
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
            });
}
