using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace DatabaseAnalyzer.Core;

internal sealed class Analyzer : IAnalyzer
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly IDiagnosticSettingsRetriever _diagnosticSettingsProviderFactory;
    private readonly IEnumerable<IGlobalAnalyzer> _globalAnalyzers;
    private readonly IProgressCallback _progressCallback;
    private readonly IEnumerable<IScriptAnalyzer> _scriptAnalyzers;
    private readonly IScriptLoader _scriptLoader;
    private readonly IScriptSourceProvider _scriptSourceProvider;

    public Analyzer
    (
        IProgressCallback progressCallback,
        IScriptSourceProvider scriptSourceProvider, IScriptLoader scriptLoader, ApplicationSettings applicationSettings, IDiagnosticSettingsRetriever diagnosticSettingsProviderFactory, IEnumerable<IScriptAnalyzer> scriptAnalyzers, IEnumerable<IGlobalAnalyzer> globalAnalyzers)
    {
        _progressCallback = progressCallback;
        _scriptSourceProvider = scriptSourceProvider;
        _scriptLoader = scriptLoader;
        _applicationSettings = applicationSettings;
        _diagnosticSettingsProviderFactory = diagnosticSettingsProviderFactory;
        _scriptAnalyzers = scriptAnalyzers;
        _globalAnalyzers = globalAnalyzers;
    }

    public AnalysisResult Analyze()
    {
        var scripts = ParseScripts();

        var analysisContext = new AnalysisContext
        (
            _applicationSettings.DatabaseToAnalyze,
            scripts,
            scripts.ToFrozenDictionary(a => a.DatabaseName, a => a, StringComparer.OrdinalIgnoreCase),
            _diagnosticSettingsProviderFactory,
            new IssueReporter()
        );

        Parallel.ForEach(analysisContext.CurrentDatabaseScripts, script =>
        {
            foreach (var analyzer in _scriptAnalyzers)
            {
                analyzer.AnalyzeScript(analysisContext, script);
            }
        });

        Parallel.ForEach(_globalAnalyzers, analyzer => analyzer.Analyze(analysisContext));

        var issues = analysisContext.IssueReporter.GetReportedIssues();
        var issuesByObjectName = issues
            .GroupBy(a => a.FullObjectNameOrFileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                a => a.Key,
                a => (IReadOnlyList<IIssue>)a.ToImmutableArray(),
                StringComparer.OrdinalIgnoreCase
            );

        return new AnalysisResult(
            issues,
            issuesByObjectName,
            _applicationSettings.Diagnostics.DisabledDiagnostics
        );
    }

    private ImmutableArray<ScriptModel> ParseScripts()
    {
        var sourceScripts = GetScriptFilePaths();
        var basicScripts = LoadScriptFiles(sourceScripts);
        return basicScripts
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .Select(ParseScript)
            .ToImmutableArray();

        static ScriptModel ParseScript(BasicScriptInformation script)
        {
            var parseResult = Parser.Parse(script.Content);
            var errors = parseResult.Errors
                .Select(a => $"{a.Message} at {a.Start.LineNumber},{a.Start.ColumnNumber} - {a.End.LineNumber},{a.End.ColumnNumber}")
                .ToImmutableArray();
            var parsedScript = parseResult.Script;

            return new ScriptModel
            (
                DatabaseName: script.DatabaseName,
                FullScriptFilePath: script.FullScriptPath,
                Content: script.Content,
                Script: parsedScript,
                Errors: errors
            );
        }

        List<BasicScriptInformation> LoadScriptFiles(IReadOnlyCollection<SourceScript> scripts)
        {
            using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Loading SQL script files");

            return scripts
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithDegreeOfParallelism(4)
                .Select(_scriptLoader.LoadScript)
                .ToList();
        }

        IReadOnlyList<SourceScript> GetScriptFilePaths()
        {
            using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Searching SQL script files");
            return _scriptSourceProvider.GetScriptFilePaths();
        }
    }
}
