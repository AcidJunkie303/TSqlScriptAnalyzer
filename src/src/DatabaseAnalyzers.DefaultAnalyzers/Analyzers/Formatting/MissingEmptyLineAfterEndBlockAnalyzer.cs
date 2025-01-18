using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingEmptyLineAfterEndBlockAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        for (var i = 0; i < script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.End)
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
                var result = a.TokenType == TSqlTokenType.WhiteSpace || a.TokenType == TSqlTokenType.SingleLineComment || a.TokenType == TSqlTokenType.MultilineComment;
                if (result)
                {
                    tokenCount++;
                }

                return result;
            })
            .FirstOrDefault();

        return immediateCatchTokenAfter is not null && predicate(immediateCatchTokenAfter)
            ? tokenIndex + tokenCount + 1
            : -1;
    }

    private static void AnalyzeEndToken(IAnalysisContext context, IScriptModel script, TSqlParserToken endToken, int tokenIndex)
    {
        if (IsNextOrNextAfterNextTokenEndOfFileToken())
        {
            return;
        }

        var missing = IsMissingEmptyLineAfter();
        if (!missing)
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

        bool IsNextOrNextAfterNextTokenEndOfFileToken()
        {
            var tokensAfterEndToken = script.ParsedScript.ScriptTokenStream.Skip(tokenIndex + 1).Take(2).ToList();
            if (tokensAfterEndToken.Count == 1)
            {
                if (tokensAfterEndToken[0].TokenType == TSqlTokenType.EndOfFile)
                {
                    return true;
                }
            }

            if (tokensAfterEndToken.Count == 2)
            {
                if (tokensAfterEndToken[0].TokenType != TSqlTokenType.WhiteSpace)
                {
                    return false;
                }

                if (tokensAfterEndToken[1].TokenType == TSqlTokenType.EndOfFile)
                {
                    return true;
                }
            }

            return false;
        }

        int GetNewLineCountAfterToken()
            => script.ParsedScript.ScriptTokenStream
                .Skip(tokenIndex + 1)
                .TakeWhile(t => t.TokenType == TSqlTokenType.WhiteSpace)
                .Sum(a => a.Text.Count(c => c == '\n'));
    }

    private static bool IsCatch(TSqlParserToken? token)
        => token is not null && token.TokenType == TSqlTokenType.Identifier && string.Equals(token.Text, "CATCH", StringComparison.OrdinalIgnoreCase);

    private static bool IsTry(TSqlParserToken? token)
        => token is not null && token.TokenType == TSqlTokenType.Identifier && string.Equals(token.Text, "TRY", StringComparison.OrdinalIgnoreCase);

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5050",
            IssueType.Formatting,
            "Missing empty line after END block",
            "Missing empty line after END block",
            [],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
