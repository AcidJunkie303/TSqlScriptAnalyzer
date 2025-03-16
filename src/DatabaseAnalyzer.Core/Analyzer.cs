using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Collections;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Core;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
internal sealed class Analyzer : IAnalyzer
{
    private static readonly ParallelOptions ParallelOptions = new()
    {
#if DEBUG
        MaxDegreeOfParallelism = 1,
#else
            MaxDegreeOfParallelism = Environment.ProcessorCount
#endif
    };

    private readonly ApplicationSettings _applicationSettings;
    private readonly IDiagnosticDefinitionProvider _diagnosticDefinitionProvider;
    private readonly IReadOnlyList<IGlobalAnalyzer> _globalAnalyzers;
    private readonly IIssueReporter _issueReporter;
    private readonly ILogger<Analyzer> _logger;
    private readonly IProgressCallback _progressCallback;
    private readonly IReadOnlyList<ScriptAnalyzerAndContext> _scriptAnalyzersAndContexts;
    private readonly IReadOnlyList<IScriptModel> _scripts;

    public Analyzer
    (
        IProgressCallback progressCallback,
        ApplicationSettings applicationSettings,
        IEnumerable<ScriptAnalyzerAndContext> scriptAnalyzersAndContexts,
        IEnumerable<IGlobalAnalyzer> globalAnalyzers,
        ILogger<Analyzer> logger,
        IIssueReporter issueReporter,
        IReadOnlyList<IScriptModel> scripts,
        IDiagnosticDefinitionProvider diagnosticDefinitionProvider)
    {
        _progressCallback = progressCallback;
        _applicationSettings = applicationSettings;
        _scriptAnalyzersAndContexts = scriptAnalyzersAndContexts.ToList();
        _globalAnalyzers = globalAnalyzers.ToList();
        _logger = logger;
        _issueReporter = issueReporter;
        _scripts = scripts;
        _diagnosticDefinitionProvider = diagnosticDefinitionProvider;
    }

    public AnalysisResult Analyze()
    {
        _logger.LogTrace("Starting analysis");

        var stopwatch = Stopwatch.StartNew();
        var scriptParseDuration = stopwatch.Elapsed;
        var analysisDuration = PerformAnalysis();

        return CalculateAnalysisResult(_issueReporter.Issues, ref scriptParseDuration, ref analysisDuration);
    }

    private TimeSpan PerformAnalysis()
    {
        var stopwatch = Stopwatch.StartNew();

        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Performing scripts analysis");

#if DEBUG
        AnalyzeWithGlobalAnalyzers();
        AnalyzeWithScriptAnalyzers();
#else
            var task1 = Task.Run(() => AnalyzeWithGlobalAnalyzers(_globalAnalyzers, parallelOptions, analysisContext), parallelOptions.CancellationToken);
            var task2 = Task.Run(() => AnalyzeWithScriptAnalyzers(_scriptAnalyzers, parallelOptions, analysisContext), parallelOptions.CancellationToken);
            Task.WaitAll(task1, task2);
#endif

        return stopwatch.Elapsed;
    }

    private void AnalyzeWithGlobalAnalyzers()
    {
        Parallel.ForEach(_globalAnalyzers, ParallelOptions, analyzer =>
        {
            try
            {
                analyzer.Analyze();
            }
#pragma warning disable CA1031 // Do not catch general exception types -> yes we do
            catch (Exception ex)
#pragma warning restore CA1031
            {
                var analyzerName = analyzer.GetType().FullName ?? "<unknown>";

                // We need to have a script file path, otherwise the aggregation between issue and script file for Global Analyzers won't work
                var relativeScriptFilePath = _scripts.Count == 0 ? "Unknown" : _scripts[0].RelativeScriptFilePath;
                _issueReporter.Report(WellKnownDiagnosticDefinitions.UnhandledAnalyzerException, "<Unknown>", relativeScriptFilePath, null, CodeRegion.Unknown, analyzerName, ex.Message);
                _logger.LogError(ex, "Analyzer threw an unhandled exception");
            }
        });
    }

    private void AnalyzeWithScriptAnalyzers()
    {
        Parallel.ForEach(_scriptAnalyzersAndContexts, ParallelOptions, a =>
        {
            var (analyzer, context) = a;

            try
            {
                analyzer.AnalyzeScript();
            }
#pragma warning disable CA1031 // Do not catch general exception types -> yes we do
            catch (Exception ex)
#pragma warning restore CA1031
            {
                var analyzerName = analyzer.GetType().FullName ?? "<unknown>";
                _issueReporter.Report(WellKnownDiagnosticDefinitions.UnhandledAnalyzerException, context.Script.DatabaseName, context.Script.RelativeScriptFilePath, null, CodeRegion.Unknown, analyzerName, ex.Message);
                _logger.LogError(ex, "Script analyzer threw an exception");
            }
        });
    }

    private AnalysisResult CalculateAnalysisResult(IReadOnlyList<IIssue> issues, ref readonly TimeSpan scriptParseDuration, ref readonly TimeSpan analysisDuration)
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Calculating results");

        var deduplicatedIssues = issues
            .Where(a => !_applicationSettings.Diagnostics.DisabledDiagnostics.Contains(a.DiagnosticDefinition.DiagnosticId))
            .Deduplicate(IssueEqualityComparers.ByPathAndDatabaseNameAndObjectNameAndCodeRegionAndMessage)
            .ToList();

        var (unsuppressedIssues, suppressedIssues) = SplitIssuesToSuppressedAndUnsuppressed(deduplicatedIssues);
        var issuesByObjectName = deduplicatedIssues
            .GroupBy(a => a.FullObjectNameOrFileName, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                a => a.Key,
                a => (IReadOnlyList<IIssue>) a
                    .OrderBy(x => x.CodeRegion)
                    .ToImmutableArray(),
                StringComparer.OrdinalIgnoreCase
            );

        var statistics = new AnalysisResultStatistics(
            TotalDisabledDiagnosticCount: _applicationSettings.Diagnostics.DisabledDiagnostics.Count,
            TotalErrorCount: deduplicatedIssues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.Error),
            TotalFormattingIssueCount: deduplicatedIssues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.Formatting),
            TotalInformationIssueCount: deduplicatedIssues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.Information),
            TotalIssueCount: deduplicatedIssues.Count,
            TotalMissingIndexIssueCount: deduplicatedIssues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.MissingIndex),
            TotalSuppressedIssueCount: suppressedIssues.Count,
            TotalWarningCount: deduplicatedIssues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.Warning),
            TotalScripts: _scripts.Count,
            ScriptsParseDuration: scriptParseDuration,
            AnalysisDuration: analysisDuration
        );

        return new AnalysisResult(
            ScriptsRootDirectoryPath: _applicationSettings.ScriptSource.ScriptsRootDirectoryPath,
            Issues: unsuppressedIssues,
            SuppressedIssues: suppressedIssues,
            IssuesByObjectName: issuesByObjectName,
            DisabledDiagnostics: _applicationSettings.Diagnostics.DisabledDiagnostics,
            Statistics: statistics,
            DiagnosticDefinitionsById: _diagnosticDefinitionProvider.DiagnosticDefinitionsById
        );
    }

    private (List<IIssue> UnsuppressedIssues, List<SuppressedIssue> SuppressedIssues) SplitIssuesToSuppressedAndUnsuppressed(List<IIssue> issues)
    {
        var unsuppressedIssues = new List<IIssue>(issues.Count);
        var suppressedIssues = new List<SuppressedIssue>(issues.Count);

        foreach (var (script, scriptIssues) in AggregateScriptsAndIssues(issues))
        {
            var (currentUnsuppressedIssues, currentSuppressedIssues)
                = DiagnosticSuppressionFilterer.Filter(script, scriptIssues);
            unsuppressedIssues.AddRange(currentUnsuppressedIssues);
            suppressedIssues.AddRange(currentSuppressedIssues);
        }

        return (unsuppressedIssues, suppressedIssues);
    }

    private IEnumerable<(IScriptModel Script, List<IIssue> Issues)> AggregateScriptsAndIssues(IEnumerable<IIssue> issues)
    {
        var issuesByFileName = issues
            .GroupBy(static a => a.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase);

        return _scripts
            .Join(
                inner: issuesByFileName,
                outerKeySelector: static a => a.RelativeScriptFilePath,
                innerKeySelector: static a => a.Key,
                resultSelector: static (a, b) => (a, b.ToList()),
                comparer: StringComparer.OrdinalIgnoreCase);
    }
}
