using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Core.Collections;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.DependencyInjection;
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

    private readonly AnalyzerTypes _analyzerTypes;
    private readonly ApplicationSettings _applicationSettings;
    private readonly IDiagnosticDefinitionProvider _diagnosticDefinitionProvider;
    private readonly IIssueReporter _issueReporter;
    private readonly ILogger<Analyzer> _logger;
    private readonly IProgressCallback _progressCallback;
    private readonly IReadOnlyList<IScriptModel> _scripts;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> _scriptsByDatabaseName;
    private readonly IServiceProvider _serviceProvider;

    public Analyzer
    (
        IProgressCallback progressCallback,
        ApplicationSettings applicationSettings,
        ILogger<Analyzer> logger,
        IIssueReporter issueReporter,
        IReadOnlyList<IScriptModel> scripts,
        IDiagnosticDefinitionProvider diagnosticDefinitionProvider,
        AnalyzerTypes analyzerTypes,
        IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> scriptsByDatabaseName,
        IServiceProvider serviceProvider)
    {
        _progressCallback = progressCallback;
        _applicationSettings = applicationSettings;
        _logger = logger;
        _issueReporter = issueReporter;
        _scripts = scripts;
        _diagnosticDefinitionProvider = diagnosticDefinitionProvider;
        _analyzerTypes = analyzerTypes;
        _scriptsByDatabaseName = scriptsByDatabaseName;
        _serviceProvider = serviceProvider;
    }

    public AnalysisResult Analyze()
    {
        ReportErroneousScripts();

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
        var task1 = Task.Run(() => AnalyzeWithGlobalAnalyzers(), ParallelOptions.CancellationToken);
        var task2 = Task.Run(() => AnalyzeWithScriptAnalyzers(), ParallelOptions.CancellationToken);
        Task.WaitAll(task1, task2);
#endif

        return stopwatch.Elapsed;
    }

    private void AnalyzeWithGlobalAnalyzers()
    {
        Parallel.ForEach(_analyzerTypes.GlobalAnalyzers, ParallelOptions, analyzerType =>
        {
            try
            {
                var analysisContext = new GlobalAnalysisContext
                (
                    _applicationSettings.DefaultSchemaName,
                    _scripts,
                    _scriptsByDatabaseName,
                    _issueReporter,
                    _logger,
                    _applicationSettings.Diagnostics.DisabledDiagnostics
                );

                var analyzer = (IGlobalAnalyzer) ActivatorUtilities.CreateInstance(_serviceProvider, analyzerType, analysisContext);
                analyzer.Analyze();
            }
#pragma warning disable CA1031 // Do not catch general exception types -> yes we do
            catch (Exception ex)
#pragma warning restore CA1031
            {
                var analyzerName = analyzerType.FullName ?? "<unknown>";

                // We need to have a script file path, otherwise the aggregation between issue and script file for Global Analyzers won't work
                var relativeScriptFilePath = _scripts.Count == 0 ? "Unknown" : _scripts[0].RelativeScriptFilePath;
                _issueReporter.Report(WellKnownDiagnosticDefinitions.UnhandledAnalyzerException, "<Unknown>", relativeScriptFilePath, null, CodeRegion.Unknown, analyzerName, ex.Message);
                _logger.LogError(ex, "Analyzer threw an unhandled exception");
            }
        });
    }

    private void AnalyzeWithScriptAnalyzers()
    {
        foreach (var script in _scripts)
        {
            Parallel.ForEach(_analyzerTypes.ScriptAnalyzers, ParallelOptions, analyzerType =>
            {
                try
                {
                    var analysisContext = new ScriptAnalysisContext
                    (
                        _applicationSettings.DefaultSchemaName,
                        _scripts,
                        script,
                        _scriptsByDatabaseName,
                        _issueReporter,
                        _logger,
                        _applicationSettings.Diagnostics.DisabledDiagnostics
                    );

                    var analyzer = (IScriptAnalyzer) ActivatorUtilities.CreateInstance(_serviceProvider, analyzerType, analysisContext);
                    analyzer.AnalyzeScript();
                }
#pragma warning disable CA1031 // Do not catch general exception types -> yes we do
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    var analyzerName = analyzerType.FullName ?? "<unknown>";

                    // We need to have a script file path, otherwise the aggregation between issue and script file for Global Analyzers won't work
                    var relativeScriptFilePath = _scripts.Count == 0 ? "Unknown" : _scripts[0].RelativeScriptFilePath;
                    _issueReporter.Report(WellKnownDiagnosticDefinitions.UnhandledAnalyzerException, "<Unknown>", relativeScriptFilePath, null, CodeRegion.Unknown, analyzerName, ex.Message);
                    _logger.LogError(ex, "Analyzer threw an unhandled exception");
                }
            });
        }
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
            Scripts: _scripts,
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

    private void ReportErroneousScripts()
    {
        foreach (var script in _scripts.Where(a => a.HasErrors))
        {
            foreach (var error in script.Errors)
            {
                _issueReporter.Report(WellKnownDiagnosticDefinitions.ScriptContainsErrors, script.DatabaseName, script.RelativeScriptFilePath, null, error.CodeRegion, error.Message);
            }
        }
    }
}
