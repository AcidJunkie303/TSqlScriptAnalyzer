using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Core;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
internal sealed class Analyzer : IAnalyzer
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly IDiagnosticSettingsProvider _diagnosticSettingsProvider;
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
        IDiagnosticSettingsProvider diagnosticSettingsProvider,
        IEnumerable<IScriptAnalyzer> scriptAnalyzers,
        IEnumerable<IGlobalAnalyzer> globalAnalyzers,
        IDiagnosticSuppressionExtractor diagnosticSuppressionExtractor)
    {
        _progressCallback = progressCallback;
        _scriptSourceProvider = scriptSourceProvider;
        _scriptLoader = scriptLoader;
        _applicationSettings = applicationSettings;
        _diagnosticSettingsProvider = diagnosticSettingsProvider;
        _scriptAnalyzers = scriptAnalyzers;
        _globalAnalyzers = globalAnalyzers;
        _diagnosticSuppressionExtractor = diagnosticSuppressionExtractor;
    }

    public AnalysisResult Analyze()
    {
        var issueReporter = new IssueReporter();
        var scripts = ParseScripts();

        var scriptsWithIssues = GetScriptsWithIssues(scripts, issueReporter).ToList();
        var issuesFound = scriptsWithIssues.Count > 0;

        // if we found scripts with issues already, we only use them
        scripts = issuesFound
            ? scriptsWithIssues
            : scripts;

        var scriptByDatabaseName = scripts
            .GroupBy(a => a.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(a => a.Key, a => (IReadOnlyList<IScriptModel>) a.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);

        var analysisContext = new AnalysisContext
        (
            _applicationSettings.DefaultSchemaName,
            scripts,
            scriptByDatabaseName,
            _diagnosticSettingsProvider,
            issueReporter
        );

        if (!issuesFound)
        {
            PerformAnalysis(analysisContext);
        }

        return CalculateAnalysisResult(analysisContext, scripts);
    }

    private void PerformAnalysis(AnalysisContext analysisContext)
    {
        using (_progressCallback.OnProgressWithAutoEndActionNotification("Running analyzers"))
        {
            Parallel.ForEach(analysisContext.Scripts, script =>
            {
                foreach (var analyzer in _scriptAnalyzers)
                {
                    ExecuteAndCaptureExceptions(analyzer, script.DatabaseName, script.RelativeScriptFilePath, analysisContext, () => analyzer.AnalyzeScript(analysisContext, script));
                }
            });

            Parallel.ForEach(_globalAnalyzers, analyzer => ExecuteAndCaptureExceptions(analyzer, string.Empty, string.Empty, analysisContext, () => analyzer.Analyze(analysisContext)));
        }

        static void ExecuteAndCaptureExceptions(IObjectAnalyzer analyzer, string databaseName, string relativeScriptFilePath, AnalysisContext analysisContext, Action action)
        {
            try
            {
                action();
            }
#pragma warning disable CA1031 // Do not catch general exception types -> yes we do
            catch (Exception ex)
#pragma warning restore CA1031
            {
                var analyzerName = analyzer.GetType().FullName ?? "<unknown>";
                analysisContext.IssueReporter.Report(WellKnownDiagnosticDefinitions.UnhandledAnalyzerException, databaseName, relativeScriptFilePath, null, CodeRegion.Unknown, analyzerName, ex.Message);
            }
        }
    }

    private AnalysisResult CalculateAnalysisResult(AnalysisContext analysisContext, IReadOnlyList<IScriptModel> scripts)
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Calculating results");
        var issues = analysisContext.IssueReporter.Issues
            .Deduplicate(
                a => $"{a.DiagnosticDefinition.DiagnosticId}::{a.RelativeScriptFilePath}::{a.CodeRegion}",
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var (unsuppressedIssues, suppressedIssues) = SplitIssuesToSuppressedAndUnsuppressed(scripts, issues);
        var issuesByObjectName = issues
            .GroupBy(a => a.FullObjectNameOrFileName, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                a => a.Key,
                a => (IReadOnlyList<IIssue>) a
                    .OrderBy(x => x.CodeRegion)
                    .ToImmutableArray(),
                StringComparer.OrdinalIgnoreCase
            );

        var staticitics = new AnalysisResultStatistics(
            TotalDisabledDiagnosticCount: _applicationSettings.Diagnostics.DisabledDiagnostics.Count,
            TotalErrorCount: issues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.Error),
            TotalFormattingIssueCount: issues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.Formatting),
            TotalInformationIssueCount: issues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.Info),
            TotalIssueCount: issues.Count,
            TotalMissingIndexIssueCount: issues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.MissingIndex),
            TotalSuppressedIssueCount: suppressedIssues.Count,
            TotalWarningCount: issues.Count(a => a.DiagnosticDefinition.IssueType == IssueType.Warning)
        );
        return new AnalysisResult(
            ScriptsRootDirectoryPath: _applicationSettings.ScriptSource.ScriptsRootDirectoryPath,
            Issues: unsuppressedIssues,
            SuppressedIssues: suppressedIssues,
            IssuesByObjectName: issuesByObjectName,
            DisabledDiagnostics: _applicationSettings.Diagnostics.DisabledDiagnostics,
            Statistics: staticitics
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

    private static List<(IScriptModel Script, List<IIssue> Issues)> AggregateScriptsAndIssues(IEnumerable<IScriptModel> scripts, IEnumerable<IIssue> issues)
    {
        var issuesByFileName = issues
            .GroupBy(static a => a.RelativeScriptFilePath, StringComparer.OrdinalIgnoreCase);

        return scripts
            .Join(
                issuesByFileName,
                static a => a.RelativeScriptFilePath,
                static a => a.Key,
                static (a, b) => (a, b.ToList()),
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<IScriptModel> ParseScripts()
    {
        var sourceScripts = GetScriptFilePaths();
        var basicScripts = LoadScriptFiles(sourceScripts);
        return basicScripts
#if !DEBUG
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
#endif
            .ConvertAll(ParseScript);

        IScriptModel ParseScript(BasicScriptInformation script)
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

    private static IEnumerable<IScriptModel> GetScriptsWithIssues(List<IScriptModel> scripts, IIssueReporter issueReporter)
    {
        foreach (var script in scripts)
        {
            if (!DoesScriptContainStatements(script))
            {
                continue;
            }

            if (CheckAndReportErrors(script, issueReporter))
            {
                yield return script;
            }
        }
    }

    private static bool CheckAndReportErrors(IScriptModel script, IIssueReporter issueReporter)
    {
        foreach (var error in script.Errors)
        {
            issueReporter.Report(WellKnownDiagnosticDefinitions.ScriptContainsErrors, script.DatabaseName, script.RelativeScriptFilePath, null, error.CodeRegion, error.Message);
        }

        return script.Errors.Count > 0;
    }

    private static bool DoesScriptContainStatements(IScriptModel script)
        => script.ParsedScript.Batches.Any(a => a.Statements.Count > 0);
}
