using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class SetOptionWhichShouldNotBeTurnedOffAnalyzer : IScriptAnalyzer
{
    private static readonly ImmutableArray<KeyValuePair<SetOptions, string>> SetOptionsWhichShouldNotBeTurnedOff = new[]
    {
        KeyValuePair.Create(SetOptions.AnsiWarnings, "ANSI_WARNINGS"),
        KeyValuePair.Create(SetOptions.ArithAbort, "ARITHABORT")
    }.ToImmutableArray();

    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var predicateStatement in script.ParsedScript.GetChildren<PredicateSetStatement>(recursive: true))
        {
            Analyze(context, script, predicateStatement);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, PredicateSetStatement predicateStatement)
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

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(predicateStatement) ?? DatabaseNames.Unknown;
        var fullObjectName = predicateStatement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, predicateStatement.GetCodeRegion(), sqlRepresentationOfOptionsWhichShouldNotBeTurnedOff);
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

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5021",
            IssueType.Warning,
            "Specific options should not be turned off",
            "The option '{0}' should not be turned off."
        );
    }
}
