using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;

namespace DatabaseAnalyzer.Core;

internal sealed class Analyzer : IAnalyzer
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly IDiagnosticSettingsProviderFactory _diagnosticSettingsProviderFactory;
    private readonly IProgressCallback _progressCallback;
    private readonly IScriptLoader _scriptLoader;
    private readonly IScriptSourceProvider _scriptSourceProvider;

    public Analyzer
    (
        IProgressCallback progressCallback,
        IScriptSourceProvider scriptSourceProvider, IScriptLoader scriptLoader, ApplicationSettings applicationSettings, IDiagnosticSettingsProviderFactory diagnosticSettingsProviderFactory)
    {
        _progressCallback = progressCallback;
        _scriptSourceProvider = scriptSourceProvider;
        _scriptLoader = scriptLoader;
        _applicationSettings = applicationSettings;
        _diagnosticSettingsProviderFactory = diagnosticSettingsProviderFactory;
    }

    public AnalysisResult Analyze()
    {
        var sourceScripts = GetScriptFilePaths();
        var scripts = LoadScriptFiles(sourceScripts);

        var analysisContext = new AnalysisContext
        (
            _applicationSettings.DatabaseToAnalyze,
            scripts,
            scripts.ToFrozenDictionary(a => a.DatabaseName, a => a, StringComparer.OrdinalIgnoreCase),
            _diagnosticSettingsProviderFactory
        );
        Console.WriteLine(scripts);
        return null!;
    }

    private IReadOnlyList<SourceScript> GetScriptFilePaths()
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Searching SQL script files");
        return _scriptSourceProvider.GetScriptFilePaths();
    }

    private ImmutableArray<BasicScriptInformation> LoadScriptFiles(IReadOnlyCollection<SourceScript> scripts)
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Loading SQL script files");

        return scripts
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .WithDegreeOfParallelism(4)
            .Select(_scriptLoader.LoadScript)
            .ToImmutableArray();
    }
}
