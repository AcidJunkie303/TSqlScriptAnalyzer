using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class RedundantPairOfParenthesesAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public RedundantPairOfParenthesesAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (var columnReference in _script.ParsedScript.GetChildren<BooleanParenthesisExpression>(recursive: true))
        {
            Analyze(columnReference);
        }
    }

    private void Analyze(BooleanParenthesisExpression expression)
    {
        if (expression.Expression is not BooleanParenthesisExpression)
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(_context, _script);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion(), expression.GetSql());
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
