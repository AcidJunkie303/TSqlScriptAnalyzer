using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingEmptyLineAfterEndBlockAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public MissingEmptyLineAfterEndBlockAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        for (var i = 0; i < _script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = _script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.End)
            {
                continue;
            }

            if (IsWithinDmlStatement(token))
            {
                continue;
            }

            // in case the next non-comment and non-whitespace token is another END token, we skip it
            var nextEndTokenIndex = FindNextTokenIndexWithCommentSkip(_script.ParsedScript.ScriptTokenStream, i, IsEnd);
            if (nextEndTokenIndex >= 0)
            {
                continue;
            }

            // in case the next non-comment and non-whitespace token is a try (of 'END TRY') we skip it because we don't want to enforce an extra line after END TRY
            var tryTokenIndex = FindNextTokenIndexWithCommentSkip(_script.ParsedScript.ScriptTokenStream, i, IsTry);
            if (tryTokenIndex >= 0)
            {
                continue;
            }

            // in case the next non-comment and non-whitespace token is catch, we use this instead of the END (END CATCH)
            var catchTokenIndex = FindNextTokenIndexWithCommentSkip(_script.ParsedScript.ScriptTokenStream, i, IsCatch);
            if (catchTokenIndex < 0)
            {
                AnalyzeEndToken(token, i);
            }
            else
            {
                var alternativeToken = _script.ParsedScript.ScriptTokenStream[catchTokenIndex];
                AnalyzeEndToken(alternativeToken, catchTokenIndex);
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

    private void AnalyzeEndToken(TSqlParserToken endToken, int tokenIndex)
    {
        if (IsNextTokenOfAnyType(TSqlTokenType.Else, TSqlTokenType.RightParenthesis, TSqlTokenType.Go, TSqlTokenType.EndOfFile))
        {
            return;
        }

        var nextEndTokenIndex = FindNextTokenIndexWithCommentSkip(_script.ParsedScript.ScriptTokenStream, tokenIndex, IsEnd);
        if (nextEndTokenIndex >= 0)
        {
            return;
        }

        var missing = IsMissingEmptyLineAfter();
        if (!missing)
        {
            return;
        }

        var statement = _script.ParsedScript.TryGetSqlFragmentAtPosition(endToken);
        if (IsIgnoredStatement(statement))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(endToken.Line, endToken.Column) ?? DatabaseNames.Unknown;
        var codeRegion = endToken.GetCodeRegion();
        var fullObjectName = _script.ParsedScript
            .TryGetSqlFragmentAtPosition(endToken)
            ?.TryGetFirstClassObjectName(_context, _script);

        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, codeRegion);

        bool IsMissingEmptyLineAfter() => GetNewLineCountAfterToken() < 2;

        bool IsNextTokenOfAnyType(params TSqlTokenType[] types)
        {
            var tokenAfterSkipTokens = _script.ParsedScript.ScriptTokenStream
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
            => _script.ParsedScript.ScriptTokenStream
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

    private bool IsWithinDmlStatement(TSqlParserToken token)
    {
        var fragment = _script.ParsedScript.TryGetSqlFragmentAtPosition(token);
        if (fragment is null)
        {
            return false;
        }

        return fragment
            .GetParents(_script.ParentFragmentProvider)
            .Any(a => a is SelectStatement or UpdateStatement or InsertStatement or DeleteStatement or MergeStatement);
    }

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
