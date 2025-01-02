using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Readability;

public sealed class RedundantPairOfParenthesesAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var columnReference in script.ParsedScript.GetChildren<BooleanParenthesisExpression>(recursive: true))
        {
            Analyze(context, script, columnReference);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, BooleanParenthesisExpression expression)
    {
        if (expression.Expression is not BooleanParenthesisExpression)
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(context, script);
        var databaseName = script.ParsedScript.FindCurrentDatabaseNameAtFragment(expression);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion(), expression.GetSql());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5031",
            IssueType.Warning,
            "Redundant pair of parentheses",
            "One of the redundant pair of parentheses '{0}' can be removed."
        );
    }
}
