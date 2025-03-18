using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Logging;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Plugins;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        _scripts = scripts;
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
    {
        return Host
            .CreateDefaultBuilder()
            .UseDefaultServiceProvider((_, options) =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            })
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(_configuration);
                services.AddSingleton(_settings);
                services.AddSingleton(_settings.Diagnostics);
                services.AddSingleton(_settings.ScriptSource);
                services.AddSingleton(_progressCallback);
                services.AddSingleton(_issueReporter);
                services.AddSingleton(_scripts);
                services.AddSingleton<IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>>>(_scriptByDatabaseName);
                services.AddSingleton(_scripts);
                services.AddLogging(_logFilePath, _minimumLogLevel);
                services.AddSingleton<IAnalyzer, Analyzer>();
                services.AddSingleton<IScriptSourceProvider, ScriptSourceProvider>();
                services.AddSingleton<IDiagnosticSuppressionExtractor, DiagnosticSuppressionExtractor>();
                services.AddSingleton<IAstService, AstService>();
                services.AddSingleton<IObjectProvider>(_ =>
                {
                    var databasesByName = new DatabaseObjectExtractor(_issueReporter)
                        .Extract(_scripts, _settings.DefaultSchemaName);
                    return new ObjectProvider(databasesByName);
                });

                var pluginAssemblies = PluginAssemblyLoader.LoadPlugins();
                RegisterAnalyzers(services, pluginAssemblies);
                RegisterSettings(services, pluginAssemblies);
                RegisterInternalSettings(services, _configuration);
                RegisterDiagnosticDefinitions(services, pluginAssemblies);
            });
    }

    private static void RegisterAnalyzers(IServiceCollection services, IReadOnlyList<PluginAssembly> pluginAssemblies)
    {
        var scriptAnalyzerTypes = new List<Type>();
        var globalAnalyzerTypes = new List<Type>();

        foreach (var pluginAssembly in pluginAssemblies)
        {
            scriptAnalyzerTypes.AddRange(pluginAssembly.ScriptAnalyzerTypes);
            globalAnalyzerTypes.AddRange(pluginAssembly.GlobalAnalyzerTypes);
        }

        var analyzerTypes = new AnalyzerTypes(scriptAnalyzerTypes, globalAnalyzerTypes);
        services.AddSingleton(analyzerTypes);
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

    private static void RegisterInternalSettings(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Services:AstService");

        services.AddSingleton(section.Get<AstServiceSettingsRaw>()?.ToSettings() ?? AstServiceSettings.Default);
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
