using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;

public class FirstStatementIsNotUseDatabaseAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        var batch = script.Script.Batches.FirstOrDefault();
        if (batch is null)
        {
            return;
        }

        var codeObject = batch.Children.FirstOrDefault();
        if (codeObject is null or SqlUseStatement)
        {
            return;
        }

        context.IssueReporter.Report(DiagnosticDefinitions.Default, script.FullScriptFilePath, null, codeObject, script.DatabaseName);
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
