using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MultipleVariableDeclarationAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var statement in script.ParsedScript.GetChildren<DeclareVariableStatement>(true))
        {
            Analyze(context, script, statement);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, DeclareVariableStatement statement)
    {
        if (statement.Declarations.Count <= 1)
        {
            return;
        }

        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        var databaseName = statement.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5024",
            IssueType.Formatting,
            "Multiple variable declaration on same line",
            "Multiple variables should be declared on separate lines using a separate DECLARE statement."
        );
    }
}
