using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class RedundantPairOfParenthesesAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

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
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion(), expression.GetSql());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5031",
            IssueType.Warning,
            "Redundant pair of parentheses",
            "The outer redundant pair of parentheses can be removed from `{0}`.",
            ["Expression"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
