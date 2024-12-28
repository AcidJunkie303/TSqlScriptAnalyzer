using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.NonStandard;

public sealed class NonStandardComparisonOperatorAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var expression in script.ParsedScript.GetChildren<BooleanComparisonExpression>(true))
        {
            Analyze(context, script, expression);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, BooleanComparisonExpression expression)
    {
        if (expression.ComparisonType != BooleanComparisonType.NotEqualToExclamation)
        {
            return;
        }

        var codeRegion = GetOperatorCodeRegion(script.ParsedScript.ScriptTokenStream, expression);
        var databaseName = expression.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = expression.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion, "!=");
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
            .OrderBy(a => a.Line)
            .ThenBy(a => a.Column)
            .ToList();

        var firstOperatorTokenCodeRegion = tokens[0].GetCodeRegion();
        var lastOperatorTokenCodeRegion = tokens[^1].GetCodeRegion();

        return CodeRegion.Create
        (
            firstOperatorTokenCodeRegion.StartLineNumber,
            firstOperatorTokenCodeRegion.StartColumnNumber,
            lastOperatorTokenCodeRegion.EndLineNumber,
            lastOperatorTokenCodeRegion.EndColumnNumber
        );
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5032",
            IssueType.Warning,
            "Non-standard comparison operator",
            "The non-standard comparison operator '{0}' should not be used."
        );
    }
}
