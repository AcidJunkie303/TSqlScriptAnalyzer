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
    private readonly IDiagnosticSuppressionExtractor _diagnosticSuppressionExtractor;
    private readonly IEnumerable<IGlobalAnalyzer> _globalAnalyzers;
    private readonly IProgressCallback _progressCallback;
    private readonly IEnumerable<IScriptAnalyzer> _scriptAnalyzers;
    private readonly IScriptLoader _scriptLoader;
    private readonly IScriptSourceProvider _scriptSourceProvider;

    public Analyzer
    (
        IProgressCallback progressCallback,
        IScriptSourceProvider scriptSourceProvider,
        IScriptLoader scriptLoader,
        ApplicationSettings applicationSettings,
        IDiagnosticSettingsRetriever diagnosticSettingsProviderFactory,
        IEnumerable<IScriptAnalyzer> scriptAnalyzers,
        IEnumerable<IGlobalAnalyzer> globalAnalyzers,
        IDiagnosticSuppressionExtractor diagnosticSuppressionExtractor)
    {
        _progressCallback = progressCallback;
        _scriptSourceProvider = scriptSourceProvider;
        _scriptLoader = scriptLoader;
        _applicationSettings = applicationSettings;
        _diagnosticSettingsProviderFactory = diagnosticSettingsProviderFactory;
        _scriptAnalyzers = scriptAnalyzers;
        _globalAnalyzers = globalAnalyzers;
        _diagnosticSuppressionExtractor = diagnosticSuppressionExtractor;
    }

    public AnalysisResult Analyze()
    {
        var scripts = ParseScripts();

        var analysisContext = new AnalysisContext
        (
            _applicationSettings.DatabaseToAnalyze,
            _applicationSettings.DefaultSchemaName,
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

        var issues = analysisContext.IssueReporter.GetIssues();

        var (unsuppressedIssues, suppressedIssues) = SplitIssuesToSuppressedAndUnsuppressed(scripts, issues);

        var issuesByObjectName = issues
            .GroupBy(a => a.FullObjectNameOrFileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                a => a.Key,
                a => (IReadOnlyList<IIssue>)a.ToImmutableArray(),
                StringComparer.OrdinalIgnoreCase
            );

        return new AnalysisResult(
            unsuppressedIssues,
            suppressedIssues,
            issuesByObjectName,
            _applicationSettings.Diagnostics.DisabledDiagnostics
        );
    }

    private static (List<IIssue> UnsuppressedIssues, List<SuppressedIssue> SuppressedIssues) SplitIssuesToSuppressedAndUnsuppressed(IReadOnlyCollection<ScriptModel> scripts, IReadOnlyCollection<IIssue> issues)
    {
        var unsuppressedIssues = new List<IIssue>(issues.Count);
        var suppressedIssues = new List<SuppressedIssue>(issues.Count);

        foreach (var scriptAndIssues in AggregateScriptsAndIssues(scripts, issues))
        {
            var (currentUnsuppressedIssues, currentSuppressedIssues) = DiagnosticSuppressionFilterer.Filter(scriptAndIssues.Script, scriptAndIssues.Issues);
            unsuppressedIssues.AddRange(currentUnsuppressedIssues);
            suppressedIssues.AddRange(currentSuppressedIssues);
        }

        return (unsuppressedIssues, suppressedIssues);
    }

    private static List<(ScriptModel Script, List<IIssue> Issues)> AggregateScriptsAndIssues(IEnumerable<ScriptModel> scripts, IEnumerable<IIssue> issues)
    {
        var issuesByFileName = issues
            .GroupBy(a => a.FullScriptFilePath, StringComparer.OrdinalIgnoreCase);

        return scripts
            .Join(
                issuesByFileName,
                a => a.FullScriptFilePath,
                a => a.Key,
                (a, b) => (a, b.ToList()),
                StringComparer.OrdinalIgnoreCase)
            .ToList();
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

        ScriptModel ParseScript(BasicScriptInformation script)
        {
            var parseResult = Parser.Parse(script.Content);
            var errors = parseResult.Errors
                .Select(a => $"{a.Message} at {CodeRegion.From(a.Start, a.End)}")
                .ToImmutableArray();
            var parsedScript = parseResult.Script;
            var suppressions = _diagnosticSuppressionExtractor.ExtractSuppressions(parsedScript).ToList();

            return new ScriptModel
            (
                script.DatabaseName,
                script.FullScriptPath,
                script.Content,
                parsedScript,
                errors,
                suppressions
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
