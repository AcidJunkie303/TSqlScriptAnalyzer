using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Comparison;

public sealed class NullComparisonAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var comparison in script.ParsedScript.GetChildren<BooleanComparisonExpression>(recursive: true))
        {
            Analyze(context, script, comparison);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, BooleanComparisonExpression expression)
    {
        Analyze(context, script, expression.FirstExpression);
        Analyze(context, script, expression.SecondExpression);
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, ScalarExpression expression)
    {
        if (expression is not NullLiteral)
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(context.DefaultSchemaName, script.ParsedScript, script.ParentFragmentProvider);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, expression);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5018",
            IssueType.Warning,
            "Null comparison",
            "Do not use equality comparison for NULL. Instead, use 'IS NULL' or 'IS NOT NULL'."
        );
    }
}
