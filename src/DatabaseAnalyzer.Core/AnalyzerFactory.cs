using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Logging;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Plugins;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace DatabaseAnalyzer.Core;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes", Justification = "Since this is a factory class that sets up a DI container, kinda hard to prevent")]
public sealed class AnalyzerFactory : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IIssueReporter _issueReporter = new IssueReporter();
    private readonly string? _logFilePath;
    private readonly LogEventLevel _minimumLogLevel;
    private readonly IProgressCallback _progressCallback;
    private readonly FrozenDictionary<string, IReadOnlyList<IScriptModel>> _scriptByDatabaseName;
    private readonly IReadOnlyList<IScriptModel> _scripts;
    private readonly ApplicationSettings _settings;
    private IHost? _host;

    public AnalyzerFactory(IConfiguration configuration, ApplicationSettings settings, IReadOnlyList<IScriptModel> scripts, IProgressCallback? progressCallback, string? logFilePath, LogEventLevel minimumLogLevel)
    {
        _configuration = configuration;
        _settings = settings;
        _scripts = ReportAndRemoveErroneousScripts(scripts, _issueReporter);
        _progressCallback = progressCallback ?? new NullProgressWriter();
        _logFilePath = logFilePath;
        _minimumLogLevel = minimumLogLevel;

        _scriptByDatabaseName = scripts
            .GroupBy(a => a.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(a => a.Key, a => (IReadOnlyList<IScriptModel>) a.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _host?.Dispose();
        _host = null;
    }

    public IAnalyzer Create()
    {
        if (_host is not null)
        {
            throw new IOException("The analyzer has already been created.");
        }

        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Initializing analyzer");

        var host = CreateHostBuilder().Build();
        var analyzer = host.Services.GetRequiredService<IAnalyzer>();

        _host = host;
        return analyzer;
    }

    private IHostBuilder CreateHostBuilder()
        => Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(_configuration);
                services.AddSingleton(_settings);
                services.AddSingleton(_settings.Diagnostics);
                services.AddSingleton(_settings.ScriptSource);
                services.AddSingleton(_progressCallback);
                services.AddSingleton(_issueReporter);
                services.AddSingleton(_scripts);
                services.AddSingleton(_scriptByDatabaseName);

                var loggerFactory = services.AddLogging(_logFilePath, _minimumLogLevel);

                services.AddSingleton<IAnalyzer, Analyzer>();
                services.AddSingleton<IScriptSourceProvider, ScriptSourceProvider>();
                services.AddSingleton<IDiagnosticSuppressionExtractor, DiagnosticSuppressionExtractor>();

                var pluginAssemblies = PluginAssemblyLoader.LoadPlugins();
                RegisterAnalyzers(services, pluginAssemblies, loggerFactory);
                RegisterServices(services, pluginAssemblies);
                RegisterSettings(services, pluginAssemblies);

                RegisterDiagnosticDefinitions(services, pluginAssemblies);
            });

    private void RegisterAnalyzers(IServiceCollection services, IReadOnlyList<PluginAssembly> pluginAssemblies, ILoggerFactory loggerFactory)
    {
        var analysisContextFactory = new AnalysisContextFactory
        (
            _settings.DefaultSchemaName,
            _scripts,
            _scriptByDatabaseName,
            _issueReporter,
            loggerFactory,
            _settings.Diagnostics.DisabledDiagnostics
        );

        foreach (var pluginAssembly in pluginAssemblies)
        {
            foreach (var analyzerType in pluginAssembly.ScriptAnalyzerTypes)
            {
                foreach (var script in _scripts)
                {
                    services.AddSingleton(typeof(ScriptAnalyzerAndContext), sp =>
                    {
                        var context = analysisContextFactory.CreateForScriptAnalyzer(script, analyzerType);
                        var analyzer = (IScriptAnalyzer) ActivatorUtilities.CreateInstance(sp, analyzerType, context, script);
                        return new ScriptAnalyzerAndContext(analyzer, context);
                    });
                }
            }

            foreach (var analyzerType in pluginAssembly.GlobalAnalyzerTypes)
            {
                services.AddSingleton(typeof(IGlobalAnalyzer), sp =>
                {
                    var context = analysisContextFactory.CreateForGlobalAnalyzer(analyzerType);
                    return ActivatorUtilities.CreateInstance(sp, analyzerType, context);
                });
            }
        }
    }

    private static List<IScriptModel> ReportAndRemoveErroneousScripts(IReadOnlyList<IScriptModel> scripts, IIssueReporter issueReporter)
    {
        var result = new List<IScriptModel>(scripts.Count);

        foreach (var script in scripts)
        {
            if (script.Errors.Count == 0)
            {
                result.Add(script);
            }
            else
            {
                foreach (var error in script.Errors)
                {
                    issueReporter.Report(WellKnownDiagnosticDefinitions.ScriptContainsErrors, script.DatabaseName, script.RelativeScriptFilePath, null, error.CodeRegion, error.Message);
                }
            }
        }

        return result;
    }

    private static void RegisterServices(IServiceCollection services, IReadOnlyList<PluginAssembly> pluginAssemblies)
    {
        foreach (var pluginAssembly in pluginAssemblies)
        {
            foreach (var type in pluginAssembly.ServiceTypes)
            {
                services.AddSingleton(typeof(IService), type);
            }
        }
    }

    private void RegisterSettings(IServiceCollection services, IReadOnlyList<PluginAssembly> pluginAssemblies)
    {
        foreach (var pluginAssembly in pluginAssemblies)
        {
            foreach (var settings in CustomSettingsLoader.Load(pluginAssembly.CustomSettings, _configuration))
            {
                services.AddSingleton(settings.GetType(), settings);
            }
        }
    }

    private static void RegisterDiagnosticDefinitions(IServiceCollection services, IReadOnlyList<PluginAssembly> pluginAssemblies)
    {
        var diagnosticDefinitionsById = pluginAssemblies
            .SelectMany(a => a.DiagnosticDefinitions)
            .Deduplicate(a => a.DiagnosticId, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(a => a.DiagnosticId, a => a, StringComparer.OrdinalIgnoreCase);

        services.AddSingleton<IDiagnosticDefinitionProvider>(new DiagnosticDefinitionProvider(diagnosticDefinitionsById));
    }
}
