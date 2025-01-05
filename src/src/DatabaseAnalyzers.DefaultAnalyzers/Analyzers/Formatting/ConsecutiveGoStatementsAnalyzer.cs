using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class ConsecutiveGoStatementsAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        for (var i = 0; i < script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.Go)
            {
                continue;
            }

            AnalyzeToken(context, script, token, i);
        }
    }

    private static void AnalyzeToken(IAnalysisContext context, IScriptModel script, TSqlParserToken goStatementToken, int tokenIndex)
    {
        var tokensAfter = script.ParsedScript.ScriptTokenStream
            .Skip(tokenIndex + 1)
            .TakeWhile(IsGoOrWhiteSpaceOrCommentToken)
            .SkipLast(1)
            .ToList();

        if (tokensAfter.TrueForAll(a => a.TokenType != TSqlTokenType.Go))
        {
            return;
        }

        var lastGoToken = tokensAfter.LastOrDefault(a => a.TokenType == TSqlTokenType.Go);
        if (lastGoToken is null)
        {
            return;
        }

        var codeRegion = CodeRegion.Create(goStatementToken.GetCodeLocation(), lastGoToken.GetCodeRegion().End);
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(goStatementToken.Line, goStatementToken.Column) ?? DatabaseNames.Unknown;
        var fullObjectName = script.ParsedScript
            .TryGetSqlFragmentAtPosition(goStatementToken)
            ?.TryGetFirstClassObjectName(context, script);

        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion);
    }

    private static bool IsGoOrWhiteSpaceOrCommentToken(TSqlParserToken token)
        => token.TokenType is TSqlTokenType.Go or TSqlTokenType.WhiteSpace or TSqlTokenType.MultilineComment or TSqlTokenType.SingleLineComment;

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5046",
            IssueType.Formatting,
            "Consecutive GO statements",
            "Consecutive GO statements.",
            [],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
