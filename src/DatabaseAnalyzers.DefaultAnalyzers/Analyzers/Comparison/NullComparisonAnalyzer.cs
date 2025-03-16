using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Comparison;

public sealed class NullComparisonAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public NullComparisonAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (var comparison in _script.ParsedScript.GetChildren<BooleanComparisonExpression>(recursive: true))
        {
            Analyze(comparison);
        }
    }

    private void Analyze(BooleanComparisonExpression expression)
    {
        Analyze(expression.FirstExpression);
        Analyze(expression.SecondExpression);
    }

    private void Analyze(ScalarExpression expression)
    {
        if (expression is not NullLiteral)
        {
            return;
        }

        var fullObjectName = expression.TryGetFirstClassObjectName(_context.DefaultSchemaName, _script.ParsedScript, _script.ParentFragmentProvider);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5018",
            IssueType.Warning,
            "Null comparison",
            "Do not use equality comparison for NULL. Instead, use `IS NULL` or `IS NOT NULL`.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
