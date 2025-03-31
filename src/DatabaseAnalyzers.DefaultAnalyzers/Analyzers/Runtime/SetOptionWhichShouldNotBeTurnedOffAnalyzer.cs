using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class SetOptionWhichShouldNotBeTurnedOffAnalyzer : IScriptAnalyzer
{
    private static readonly ImmutableArray<KeyValuePair<SetOptions, string>> SetOptionsWhichShouldNotBeTurnedOff = new[]
    {
        KeyValuePair.Create(SetOptions.AnsiWarnings, "ANSI_WARNINGS"),
        KeyValuePair.Create(SetOptions.ArithAbort, "ARITHABORT")
    }.ToImmutableArray();

    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public SetOptionWhichShouldNotBeTurnedOffAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var predicateStatement in _script.ParsedScript.GetChildren<PredicateSetStatement>(recursive: true))
        {
            Analyze(predicateStatement);
        }
    }

    private static IEnumerable<string> GetSqlRepresentationOfOptionsWhichShouldNotBeTurnedOff(SetOptions setOptions)
    {
        foreach (var (setOption, sqlRepresentation) in SetOptionsWhichShouldNotBeTurnedOff)
        {
            if (setOptions.HasFlag(setOption))
            {
                yield return sqlRepresentation;
            }
        }
    }

    private void Analyze(PredicateSetStatement predicateStatement)
    {
        if (predicateStatement.IsOn)
        {
            return;
        }

        var sqlRepresentationOfOptionsWhichShouldNotBeTurnedOff = GetSqlRepresentationOfOptionsWhichShouldNotBeTurnedOff(predicateStatement.Options).StringJoin(", ");
        if (sqlRepresentationOfOptionsWhichShouldNotBeTurnedOff.Length == 0)
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(predicateStatement) ?? DatabaseNames.Unknown;
        var fullObjectName = predicateStatement.TryGetFirstClassObjectName(_context, _script);
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, predicateStatement.GetCodeRegion(), sqlRepresentationOfOptionsWhichShouldNotBeTurnedOff);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5021",
            IssueType.Warning,
            "Specific options should not be turned off",
            "The option `{0}` should not be turned off.",
            ["Option name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
