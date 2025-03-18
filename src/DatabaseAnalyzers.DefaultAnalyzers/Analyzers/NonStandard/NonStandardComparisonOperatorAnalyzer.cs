using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.NonStandard;

public sealed class NonStandardComparisonOperatorAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public NonStandardComparisonOperatorAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var expression in _script.ParsedScript.GetChildren<BooleanComparisonExpression>(recursive: true))
        {
            Analyze(expression);
        }
    }

    private void Analyze(BooleanComparisonExpression expression)
    {
        if (expression.ComparisonType != BooleanComparisonType.NotEqualToExclamation)
        {
            return;
        }

        var codeRegion = GetOperatorCodeRegion(_script.ParsedScript.ScriptTokenStream, expression);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        var fullObjectName = expression.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion, "!=", "<>");
    }

    private static CodeRegion GetOperatorCodeRegion(IList<TSqlParserToken> scriptTokens, BooleanComparisonExpression expression)
    {
        var tokens = new List<TSqlParserToken>();

        for (var i = expression.SecondExpression.FirstTokenIndex - 1; i > expression.FirstExpression.LastTokenIndex; i--)
        {
            var token = scriptTokens[i];
            if (token.TokenType is TSqlTokenType.MultilineComment or TSqlTokenType.SingleLineComment or TSqlTokenType.WhiteSpace)
            {
                continue;
            }

            tokens.Add(token);
        }

        if (tokens.Count == 0)
        {
            return expression.GetCodeRegion(); // fallback to whole comparison if we cannot extract the operator tokens
        }

        tokens = tokens
            .OrderBy(static a => a.Line)
            .ThenBy(static a => a.Column)
            .ToList();

        var firstOperatorTokenCodeRegion = tokens[0].GetCodeRegion();
        var lastOperatorTokenCodeRegion = tokens[^1].GetCodeRegion();

        return CodeRegion.Create
        (
            firstOperatorTokenCodeRegion.Begin,
            lastOperatorTokenCodeRegion.End
        );
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5032",
            IssueType.Warning,
            "Non-standard comparison operator",
            "The non-standard comparison operator `{0}` should not be used. Use `{1}` instead.",
            ["Comparison Operator", "Standard Comparison Operator"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
