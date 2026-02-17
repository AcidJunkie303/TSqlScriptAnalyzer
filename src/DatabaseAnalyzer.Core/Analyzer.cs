using System.Collections.Concurrent;
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
    private readonly Dictionary<Type, (AnalyzerKind AnalyzerKind, ConcurrentBag<TimeSpan> Durations)> _analysisDurationsByAnalyzerType = [];
    private readonly AnalyzerTypes _analyzerTypes;
    private readonly ApplicationSettings _applicationSettings;
    private readonly IDiagnosticDefinitionProvider _diagnosticDefinitionProvider;
    private readonly IIssueReporter _issueReporter;
    private readonly ILogger<Analyzer> _logger;
    private readonly ParallelOptions _parallelOptions;
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
        IServiceProvider serviceProvider,
        ParallelOptions parallelOptions)
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
        _parallelOptions = parallelOptions;

        foreach (var type in analyzerTypes.GlobalAnalyzers)
        {
            _analysisDurationsByAnalyzerType.Add(type, (AnalyzerKind.GlobalAnalyzer, new ConcurrentBag<TimeSpan>()));
        }

        foreach (var type in analyzerTypes.ScriptAnalyzers)
        {
            _analysisDurationsByAnalyzerType.Add(type, (AnalyzerKind.ScriptAnalyzer, new ConcurrentBag<TimeSpan>()));
        }
    }

    public AnalysisResult Analyze()
    {
        ReportErroneousScripts();

        _logger.LogTrace("Starting analysis");

        var analysisDuration = PerformAnalysis();

        return CalculateAnalysisResult(_issueReporter.Issues, ref analysisDuration);
    }

    private TimeSpan PerformAnalysis()
    {
        var stopwatch = Stopwatch.StartNew();

        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Performing scripts analysis");

        IEnumerable<(Type AnalyzerType, IScriptModel? Script)> analyzerTypesAndScripts =
        [
            .. _analyzerTypes.GlobalAnalyzers.Select(static type => (Type: type, (IScriptModel?) null)),
            .. _analyzerTypes.ScriptAnalyzers
                .CrossJoin(_scripts)
                .Select(static a => (Type: a.Item1, (IScriptModel?) a.Item2))
        ];

        PerformAnalysis(analyzerTypesAndScripts);
        LogExecutionDurations();

        return stopwatch.Elapsed;
    }

    private void PerformAnalysis(IEnumerable<(Type AnalyzerType, IScriptModel? Script)> analyzerTypesAndScripts)
    {
        Parallel.ForEach(analyzerTypesAndScripts, _parallelOptions, analyzerTypeAndScript =>
        {
            var (analyzerType, script) = analyzerTypeAndScript;
            var startedAt = Stopwatch.GetTimestamp();
            try
            {
                if (script is null)
                {
                    var analysisContext = new GlobalAnalysisContext
                    (
                        _applicationSettings.DefaultSchemaName,
                        _scripts,
                        _scriptsByDatabaseName,
                        _applicationSettings.Diagnostics.DisabledDiagnostics
                    );

                    var analyzer = (IGlobalAnalyzer) ActivatorUtilities.CreateInstance(_serviceProvider, analyzerType, analysisContext);
                    analyzer.Analyze();
                }
                else
                {
                    var analysisContext = new ScriptAnalysisContext
                    (
                        _applicationSettings.DefaultSchemaName,
                        _scripts,
                        script,
                        _scriptsByDatabaseName,
                        _applicationSettings.Diagnostics.DisabledDiagnostics
                    );

                    var analyzer = (IScriptAnalyzer) ActivatorUtilities.CreateInstance(_serviceProvider, analyzerType, analysisContext);
                    analyzer.AnalyzeScript();
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types -> yes we do
            catch (Exception ex)
#pragma warning restore CA1031
            {
                var analyzerName = analyzerType.FullName ?? "<unknown>";
                // We need to have a script file path, otherwise the aggregation between issue and script file for Global Analyzers won't work
                var relativeScriptFilePath = script?.RelativeScriptFilePath ?? (_scripts.Count == 0 ? "Unknown" : _scripts[0].RelativeScriptFilePath);
                _issueReporter.Report(WellKnownDiagnosticDefinitions.UnhandledAnalyzerException, "<Unknown>", relativeScriptFilePath, fullObjectName: null, CodeRegion.Unknown, analyzerName, ex.Message);
                _logger.LogError(ex, "Analyzer threw an unhandled exception");
            }

            var duration = Stopwatch.GetElapsedTime(startedAt);
            _analysisDurationsByAnalyzerType[analyzerType].Durations.Add(duration);
        });
    }

    private AnalysisResult CalculateAnalysisResult(IReadOnlyList<IIssue> issues, ref readonly TimeSpan analysisDuration)
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

    [SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging")]
    private void LogExecutionDurations()
    {
        var analyzerNameAndDurations = _analysisDurationsByAnalyzerType
            .Select(static a =>
            {
                var analyzerKind = a.Value.AnalyzerKind;
                var analyzerType = a.Key;
                var durations = a.Value.Durations;
                var analyzerName = analyzerType.Name;
                var analyzerFullName = analyzerType.FullName;
                var averageDuration = durations.Aggregate(TimeSpan.Zero, static (x, y) => x + y) / durations.Count;
                var maxDuration = durations.Max();
                var minDuration = durations.Min();

                return (AnalyzerKind: analyzerKind, Name: analyzerName, FullName: analyzerFullName, AverageDuration: averageDuration, MinDuration: minDuration, MaxDuration: maxDuration);
            })
            .OrderBy(static a => a.AnalyzerKind)
            .ThenByDescending(static a => a.AverageDuration);

        foreach (var entry in analyzerNameAndDurations)
        {
            _logger.LogInformation("Execution statistics {Kind}: Avg={AverageDuration} Min={MinDuration} Max={MaxDuration} {AnalyzerName} {FullName}",
                entry.AnalyzerKind, entry.AverageDuration, entry.MinDuration, entry.MaxDuration, entry.Name, entry.FullName);
        }
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
        foreach (var script in _scripts.Where(static a => a.HasErrors))
        {
            foreach (var error in script.Errors)
            {
                _issueReporter.Report(WellKnownDiagnosticDefinitions.ScriptContainsErrors, script.DatabaseName, script.RelativeScriptFilePath, fullObjectName: null, error.CodeRegion, error.Message);
            }
        }
    }

    private enum AnalyzerKind
    {
        None = 0,
        GlobalAnalyzer = 1,
        ScriptAnalyzer = 2
    }
}
