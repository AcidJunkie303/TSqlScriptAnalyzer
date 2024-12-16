using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;

public sealed class FirstStatementIsNotUseDatabaseAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var batch = script.ParsedScript.Batches.FirstOrDefault();
        if (batch is null)
        {
            return;
        }

        var statement = batch.Statements.FirstOrDefault();
        if (statement is null or UseStatement)
        {
            return;
        }

        context.IssueReporter.Report(DiagnosticDefinitions.Default, script, null, statement, script.DatabaseName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5004",
            IssueType.Warning,
            "The first statement in a script must be 'USE DATABASE'",
            "The first statement in a script must be 'USE {0}'"
        );
    }
}
