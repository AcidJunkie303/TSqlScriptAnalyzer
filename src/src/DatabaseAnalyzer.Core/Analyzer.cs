using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;
using Microsoft.SqlServer.TransactSql.ScriptDom;

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

        var scriptByDatabaseName = scripts
            .GroupBy(a => a.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(a => a.Key, a => (IReadOnlyList<IScriptModel>)a.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);

        var analysisContext = new AnalysisContext
        (
            _applicationSettings.DefaultSchemaName,
            scripts,
            scriptByDatabaseName,
            _diagnosticSettingsProviderFactory,
            new IssueReporter()
        );

        Parallel.ForEach(analysisContext.Scripts, script =>
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
            _applicationSettings.ScriptSource.ScriptsRootDirectoryPath,
            unsuppressedIssues,
            suppressedIssues,
            issuesByObjectName,
            _applicationSettings.Diagnostics.DisabledDiagnostics
        );
    }

    private static (List<IIssue> UnsuppressedIssues, List<SuppressedIssue> SuppressedIssues) SplitIssuesToSuppressedAndUnsuppressed(IReadOnlyCollection<IScriptModel> scripts, IReadOnlyCollection<IIssue> issues)
    {
        var unsuppressedIssues = new List<IIssue>(issues.Count);
        var suppressedIssues = new List<SuppressedIssue>(issues.Count);

        foreach (var (script, scriptIssues) in AggregateScriptsAndIssues(scripts, issues))
        {
            var (currentUnsuppressedIssues, currentSuppressedIssues)
                = DiagnosticSuppressionFilterer.Filter(script, scriptIssues);
            unsuppressedIssues.AddRange(currentUnsuppressedIssues);
            suppressedIssues.AddRange(currentSuppressedIssues);
        }

        return (unsuppressedIssues, suppressedIssues);
    }

    private static List<(IScriptModel Script, List<IIssue> Issues)> AggregateScriptsAndIssues(IEnumerable<IScriptModel> scripts, IEnumerable<IIssue> issues)
    {
        var issuesByFileName = issues
            .GroupBy(a => a.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase);

        return scripts
            .Join(
                issuesByFileName,
                a => a.RelativeScriptFilePath,
                a => a.Key,
                (a, b) => (a, b.ToList()),
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private ImmutableArray<IScriptModel> ParseScripts()
    {
        var sourceScripts = GetScriptFilePaths();
        var basicScripts = LoadScriptFiles(sourceScripts);
        return basicScripts
#if !DEBUG
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
#endif
            .Select(ParseScript)
            .ToImmutableArray();

        IScriptModel ParseScript(BasicScriptInformation script)
        {
            var parser = TSqlParser.CreateParser(SqlVersion.Sql170, true);
            using var reader = new StringReader(script.Contents);
            var parsedScript = parser.Parse(reader, out var parserErrors) as TSqlScript ?? new TSqlScript();
            var errorMessages = parserErrors
                .Select(a => $"{a.Message} at {CodeRegion.Create(a.Line, a.Column, a.Line, a.Column)}")
                .ToImmutableArray();
            var suppressions = _diagnosticSuppressionExtractor.ExtractSuppressions(parsedScript).ToList();
            var parentFragmentProvider = parsedScript.CreateParentFragmentProvider();

            return new ScriptModel
            (
                script.DatabaseName,
                script.FullScriptPath,
                script.Contents,
                parsedScript,
                parentFragmentProvider,
                errorMessages,
                suppressions
            );
        }

        List<BasicScriptInformation> LoadScriptFiles(IReadOnlyCollection<SourceScript> scripts)
        {
            using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Loading SQL script files");

            return scripts
#if !DEBUG
                .AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithDegreeOfParallelism(4)
#endif
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
