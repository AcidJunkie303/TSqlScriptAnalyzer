using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingEmptyLineAfterEndBlockAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        for (var i = 0; i < script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.End)
            {
                continue;
            }

            // in case the next non-comment and non-whitespace token is another END token, we skip it
            var nextEndTokenIndex = FindNextTokenIndexWithCommentSkip(script.ParsedScript.ScriptTokenStream, i, IsEnd);
            if (nextEndTokenIndex >= 0)
            {
                continue;
            }

            // in case the next non-comment and non-whitespace token is a try (of 'END TRY') we skip it because we don't want to enforce an extra line after END TRY
            var tryTokenIndex = FindNextTokenIndexWithCommentSkip(script.ParsedScript.ScriptTokenStream, i, IsTry);
            if (tryTokenIndex >= 0)
            {
                continue;
            }

            // in case the next non-comment and non-whitespace token is catch, we use this instead of the END (END CATCH)
            var catchTokenIndex = FindNextTokenIndexWithCommentSkip(script.ParsedScript.ScriptTokenStream, i, IsCatch);
            if (catchTokenIndex < 0)
            {
                AnalyzeEndToken(context, script, token, i);
            }
            else
            {
                var alternativeToken = script.ParsedScript.ScriptTokenStream[catchTokenIndex];
                AnalyzeEndToken(context, script, alternativeToken, catchTokenIndex);
            }
        }
    }

    private static int FindNextTokenIndexWithCommentSkip(IList<TSqlParserToken> tokens, int tokenIndex, Predicate<TSqlParserToken> predicate)
    {
        var tokenCount = 0;
        var immediateCatchTokenAfter = tokens
            .Skip(tokenIndex + 1)
            .SkipWhile(a =>
            {
                var isSkipToken = IsSkipToken(a);
                if (isSkipToken)
                {
                    tokenCount++;
                }

                return isSkipToken;
            })
            .FirstOrDefault();

        return immediateCatchTokenAfter is not null && predicate(immediateCatchTokenAfter)
            ? tokenIndex + tokenCount + 1
            : -1;
    }

    private static void AnalyzeEndToken(IAnalysisContext context, IScriptModel script, TSqlParserToken endToken, int tokenIndex)
    {
        if (IsNextTokenOfAnyType(TSqlTokenType.Else, TSqlTokenType.RightParenthesis, TSqlTokenType.Go, TSqlTokenType.EndOfFile))
        {
            return;
        }

        var nextEndTokenIndex = FindNextTokenIndexWithCommentSkip(script.ParsedScript.ScriptTokenStream, tokenIndex, IsEnd);
        if (nextEndTokenIndex >= 0)
        {
            return;
        }

        var missing = IsMissingEmptyLineAfter();
        if (!missing)
        {
            return;
        }

        var statement = script.ParsedScript.TryGetSqlFragmentAtPosition(endToken);
        if (IsIgnoredStatement(statement))
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(endToken.Line, endToken.Column) ?? DatabaseNames.Unknown;
        var codeRegion = endToken.GetCodeRegion();
        var fullObjectName = script.ParsedScript
            .TryGetSqlFragmentAtPosition(endToken)
            ?.TryGetFirstClassObjectName(context, script);

        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion);

        bool IsMissingEmptyLineAfter() => GetNewLineCountAfterToken() < 2;

        bool IsNextTokenOfAnyType(params TSqlTokenType[] types)
        {
            var tokenAfterSkipTokens = script.ParsedScript.ScriptTokenStream
                .Skip(tokenIndex + 1)
                .SkipWhile(IsSkipToken)
                .FirstOrDefault();

            if (tokenAfterSkipTokens is null)
            {
                return false;
            }

            return types.Any(type => tokenAfterSkipTokens.TokenType == type);
        }

        int GetNewLineCountAfterToken()
            => script.ParsedScript.ScriptTokenStream
                .Skip(tokenIndex + 1)
                .TakeWhile(IsSkipToken)
                .Sum(a => a.Text.Count(c => c == '\n'));
    }

    private static bool IsIgnoredStatement(TSqlFragment? fragment) => fragment is SearchedCaseExpression or SimpleCaseExpression;

    private static bool IsSkipToken(TSqlParserToken token)
        => token.TokenType is TSqlTokenType.WhiteSpace or TSqlTokenType.SingleLineComment or TSqlTokenType.MultilineComment or TSqlTokenType.Semicolon;

    private static bool IsCatch(TSqlParserToken? token)
        => token is not null && token.TokenType == TSqlTokenType.Identifier && string.Equals(token.Text, "CATCH", StringComparison.OrdinalIgnoreCase);

    private static bool IsTry(TSqlParserToken? token)
        => token is not null && token.TokenType == TSqlTokenType.Identifier && string.Equals(token.Text, "TRY", StringComparison.OrdinalIgnoreCase);

    private static bool IsEnd(TSqlParserToken? token)
        => token is not null && token.TokenType == TSqlTokenType.End;

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5050",
            IssueType.Formatting,
            "Missing empty line after END block",
            "Missing empty line after END block.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
