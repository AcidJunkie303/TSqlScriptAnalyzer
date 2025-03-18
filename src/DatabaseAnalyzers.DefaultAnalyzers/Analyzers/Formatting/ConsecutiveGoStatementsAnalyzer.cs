using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class ConsecutiveGoStatementsAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ConsecutiveGoStatementsAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        for (var i = 0; i < _script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = _script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.Go)
            {
                continue;
            }

            AnalyzeToken(token, i);
        }
    }

    private void AnalyzeToken(TSqlParserToken goStatementToken, int tokenIndex)
    {
        var tokensAfter = _script.ParsedScript.ScriptTokenStream
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
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(goStatementToken.Line, goStatementToken.Column) ?? DatabaseNames.Unknown;
        var fullObjectName = _script.ParsedScript
            .TryGetSqlFragmentAtPosition(goStatementToken)
            ?.TryGetFirstClassObjectName(_context, _script);

        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion);
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
            "Consecutive `GO` statements.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
