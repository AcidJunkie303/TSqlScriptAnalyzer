using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Collections;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Core;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
internal sealed class Analyzer : IAnalyzer
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly IReadOnlyDictionary<string, IDiagnosticDefinition> _diagnosticDefinitionsById;
    private readonly IDiagnosticSettingsProvider _diagnosticSettingsProvider;
    private readonly IDiagnosticSuppressionExtractor _diagnosticSuppressionExtractor;
    private readonly IReadOnlyList<IGlobalAnalyzer> _globalAnalyzers;
    private readonly ILogger<Analyzer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IProgressCallback _progressCallback;
    private readonly IReadOnlyList<IScriptAnalyzer> _scriptAnalyzers;
    private readonly IScriptLoader _scriptLoader;
    private readonly IScriptSourceProvider _scriptSourceProvider;

    public Analyzer
    (
        IProgressCallback progressCallback,
        IScriptSourceProvider scriptSourceProvider,
        IScriptLoader scriptLoader,
        ApplicationSettings applicationSettings,
        IDiagnosticSettingsProvider diagnosticSettingsProvider,
        IEnumerable<IScriptAnalyzer> scriptAnalyzers,
        IEnumerable<IGlobalAnalyzer> globalAnalyzers,
        IDiagnosticSuppressionExtractor diagnosticSuppressionExtractor,
        IReadOnlyDictionary<string, IDiagnosticDefinition> diagnosticDefinitionsById,
        ILogger<Analyzer> logger, ILoggerFactory loggerFactory)
    {
        _progressCallback = progressCallback;
        _scriptSourceProvider = scriptSourceProvider;
        _scriptLoader = scriptLoader;
        _applicationSettings = applicationSettings;
        _diagnosticSettingsProvider = diagnosticSettingsProvider;
        _scriptAnalyzers = scriptAnalyzers
            .Where(a => !AreAllDiagnosticsForAnalyzerDisabled(a, applicationSettings.Diagnostics))
            .ToImmutableArray();
        _globalAnalyzers = globalAnalyzers
            .Where(a => !AreAllDiagnosticsForAnalyzerDisabled(a, applicationSettings.Diagnostics))
            .ToImmutableArray();
        _diagnosticSuppressionExtractor = diagnosticSuppressionExtractor;
        _diagnosticDefinitionsById = diagnosticDefinitionsById;
        _logger = logger;
        _loggerFactory = loggerFactory;

        static bool AreAllDiagnosticsForAnalyzerDisabled(IObjectAnalyzer analyzer, DiagnosticsSettings diagnosticsSettings)
            => analyzer.SupportedDiagnostics.All(a => diagnosticsSettings.DisabledDiagnostics.Contains(a.DiagnosticId));
    }

    public AnalysisResult Analyze()
    {
        _logger.LogTrace("Starting analysis");

        var issueReporter = new IssueReporter();

        var stopwatch = Stopwatch.StartNew();
        var scripts = ParseScripts();
        var scriptParseDuration = stopwatch.Elapsed;

        scripts = ReportAndRemoveErroneousScripts(scripts, issueReporter);

        var scriptByDatabaseName = scripts
            .GroupBy(a => a.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(a => a.Key, a => (IReadOnlyList<IScriptModel>) a.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);

        var analysisContextFactory = new AnalysisContextFactory
        (
            _applicationSettings.DefaultSchemaName,
            scripts,
            scriptByDatabaseName,
            _diagnosticSettingsProvider,
            issueReporter,
            _loggerFactory,
            _applicationSettings.Diagnostics.DisabledDiagnostics
        );

        var analysisDuration = PerformAnalysis(scripts, analysisContextFactory);

        return CalculateAnalysisResult(issueReporter.Issues, scripts, ref scriptParseDuration, ref analysisDuration);
    }

    private TimeSpan PerformAnalysis(IReadOnlyList<IScriptModel> scripts, AnalysisContextFactory analysisContextFactory)
    {
        var stopwatch = Stopwatch.StartNew();
        var parallelOptions = new ParallelOptions
        {
#if DEBUG
            MaxDegreeOfParallelism = 1,
#else
            MaxDegreeOfParallelism = Environment.ProcessorCount
#endif
        };

        using (_progressCallback.OnProgressWithAutoEndActionNotification("Running analyzers"))
        {
#if DEBUG
            AnalyzeWithGlobalAnalyzers(_globalAnalyzers);
            AnalyzeWithScriptAnalyzers(_scriptAnalyzers);
#else
            var task1 = Task.Run(() => AnalyzeWithGlobalAnalyzers(_globalAnalyzers, parallelOptions, analysisContext), parallelOptions.CancellationToken);
            var task2 = Task.Run(() => AnalyzeWithScriptAnalyzers(_scriptAnalyzers, parallelOptions, analysisContext), parallelOptions.CancellationToken);
            Task.WaitAll(task1, task2);
#endif
        }

        return stopwatch.Elapsed;

        void AnalyzeWithScriptAnalyzers(IReadOnlyList<IScriptAnalyzer> analyzers)
        {
            var scriptsAndAnalyzers =
                from script in scripts
                from analyzer in analyzers
                select (Script: script, Analyzer: analyzer);

            Parallel.ForEach(scriptsAndAnalyzers, parallelOptions, scriptAndAnalyzer =>
            {
                var (script, analyzer) = scriptAndAnalyzer;
                var analysisContext = analysisContextFactory.Create(analyzer, script);
                try
                {
                    analyzer.AnalyzeScript(analysisContext, script);
                }
#pragma warning disable CA1031 // Do not catch general exception types -> yes we do
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    var analyzerName = analyzer.GetType().FullName ?? "<unknown>";
                    analysisContext.IssueReporter.Report(WellKnownDiagnosticDefinitions.UnhandledAnalyzerException, script.DatabaseName, script.RelativeScriptFilePath, null, CodeRegion.Unknown, analyzerName, ex.Message);
                    _logger.LogError(ex, "Analyzer threw an unhandled exception");
                }
            });
        }

        void AnalyzeWithGlobalAnalyzers(IReadOnlyList<IGlobalAnalyzer> analyzers)
        {
            Parallel.ForEach(analyzers, parallelOptions, analyzer =>
            {
                var analysisContext = analysisContextFactory.Create(analyzer);
                try
                {
                    analyzer.Analyze(analysisContext);
                }
#pragma warning disable CA1031 // Do not catch general exception types -> yes we do
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    var analyzerName = analyzer.GetType().FullName ?? "<unknown>";

                    // We need to have a script file path, otherwise the aggregation between issue and script file for Global Analyzers won't work
                    var relativeScriptFilePath = analysisContext.Scripts.Count == 0 ? "Unknown" : analysisContext.Scripts[0].RelativeScriptFilePath;
                    analysisContext.IssueReporter.Report(WellKnownDiagnosticDefinitions.UnhandledAnalyzerException, "<Unknown>", relativeScriptFilePath, null, CodeRegion.Unknown, analyzerName, ex.Message);
                    _logger.LogError(ex, "Analyzer threw an unhandled exception");
                }
            });
        }
    }

    private AnalysisResult CalculateAnalysisResult(IReadOnlyList<IIssue> issues, List<IScriptModel> scripts, ref readonly TimeSpan scriptParseDuration, ref readonly TimeSpan analysisDuration)
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Calculating results");

        var deduplicatedIssues = issues
            .Where(a => !_applicationSettings.Diagnostics.DisabledDiagnostics.Contains(a.DiagnosticDefinition.DiagnosticId))
            .Deduplicate(IssueEqualityComparers.ByPathAndDatabaseNameAndObjectNameAndCodeRegionAndMessage)
            .ToList();

        var (unsuppressedIssues, suppressedIssues) = SplitIssuesToSuppressedAndUnsuppressed(scripts, deduplicatedIssues);
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
            TotalScripts: scripts.Count,
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
            DiagnosticDefinitionsById: _diagnosticDefinitionsById
        );
    }

    private static (List<IIssue> UnsuppressedIssues, List<SuppressedIssue> SuppressedIssues) SplitIssuesToSuppressedAndUnsuppressed(IReadOnlyCollection<IScriptModel> scripts, List<IIssue> issues)
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

    private static IEnumerable<(IScriptModel Script, List<IIssue> Issues)> AggregateScriptsAndIssues(IEnumerable<IScriptModel> scripts, IEnumerable<IIssue> issues)
    {
        var issuesByFileName = issues
            .GroupBy(static a => a.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase);

        return scripts
            .Join(
                inner: issuesByFileName,
                outerKeySelector: static a => a.RelativeScriptFilePath,
                innerKeySelector: static a => a.Key,
                resultSelector: static (a, b) => (a, b.ToList()),
                comparer: StringComparer.OrdinalIgnoreCase);
    }

    private List<IScriptModel> ParseScripts()
    {
        var sourceScripts = GetScriptFilePaths();
        var basicScripts = LoadScriptFiles(sourceScripts);

#if DEBUG
        return basicScripts.ConvertAll(ParseScript);
#else
        return basicScripts
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .WithDegreeOfParallelism(1)
            .Select(ParseScript)
            .ToList();
#endif
    }

    private IScriptModel ParseScript(BasicScriptInformation script)
    {
        var parser = TSqlParser.CreateParser(SqlVersion.Sql170, initialQuotedIdentifiers: true);
        using var reader = new StringReader(script.Contents);
        var parsedScript = parser.Parse(reader, out var parserErrors) as TSqlScript ?? new TSqlScript();
        var errorMessages = parserErrors
            .Select(a => new ScriptError(a.Message, CodeRegion.Create(a.Line, a.Column, a.Line, a.Column)))
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

    private List<BasicScriptInformation> LoadScriptFiles(IReadOnlyCollection<SourceScript> scripts)
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

    private IReadOnlyList<SourceScript> GetScriptFilePaths()
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Searching SQL script files");
        return _scriptSourceProvider.GetScriptFilePaths();
    }

    private static List<IScriptModel> ReportAndRemoveErroneousScripts(List<IScriptModel> scripts, IssueReporter issueReporter)
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
}
