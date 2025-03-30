using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

internal static class BlankSpaceComplianceEvaluators
{
    private static readonly FrozenSet<TSqlTokenType> OperationTokens = new[]
    {
        TSqlTokenType.EqualsSign,
        TSqlTokenType.Minus,
        TSqlTokenType.Add,
        TSqlTokenType.Star,
        TSqlTokenType.Divide,
        TSqlTokenType.PercentSign,
        TSqlTokenType.GreaterThan,
        TSqlTokenType.LessThan,
        TSqlTokenType.AddEquals,
        TSqlTokenType.ConcatEquals,
        TSqlTokenType.DivideEquals,
        TSqlTokenType.ModEquals,
        TSqlTokenType.MultiplyEquals,
        TSqlTokenType.SubtractEquals,
        TSqlTokenType.BitwiseAndEquals,
        TSqlTokenType.BitwiseOrEquals,
        TSqlTokenType.BitwiseXorEquals,
        TSqlTokenType.Bang
    }.ToFrozenSet();

    internal static class Before
    {
        public static bool General(IList<TSqlParserToken> tokens, int currentTokenIndex)
        {
            var previousToken = tokens.GetPreviousToken(currentTokenIndex);
            if (previousToken is null)
            {
                return false;
            }

            if (previousToken.TokenType == TSqlTokenType.WhiteSpace)
            {
                return true;
            }

            return IsPreviousNonWhiteSpaceTokenAnyKindOfOperationToken(tokens, currentTokenIndex);
        }

        public static bool Star(IList<TSqlParserToken> tokens, int currentTokenIndex)
        {
            var previousToken = tokens.GetPreviousToken(currentTokenIndex);
            if (previousToken is null)
            {
                return false;
            }

            if (previousToken.TokenType is TSqlTokenType.WhiteSpace or TSqlTokenType.LeftParenthesis) // COUNT(*)
            {
                return true;
            }

            return IsPreviousNonWhiteSpaceTokenAnyKindOfOperationToken(tokens, currentTokenIndex);
        }
    }

    internal static class After
    {
        public static bool EqualSign(IList<TSqlParserToken> tokens, int currentTokenIndex)
        {
            var nextToken = tokens.GetNextToken(currentTokenIndex);
            if (nextToken is null)
            {
                return false;
            }

            return nextToken.TokenType == TSqlTokenType.WhiteSpace;
        }

        public static bool General(IList<TSqlParserToken> tokens, int currentTokenIndex)
        {
            var nextToken = tokens.GetNextToken(currentTokenIndex);
            if (nextToken is null)
            {
                return false;
            }

            if (nextToken.TokenType == TSqlTokenType.WhiteSpace)
            {
                return true;
            }

            return IsNextTokenAnyKindOfOperationToken(tokens, currentTokenIndex);
        }

        public static bool PlusOrMinus(IList<TSqlParserToken> tokens, int currentTokenIndex)
        {
            var nextToken = tokens.GetNextToken(currentTokenIndex);
            if (nextToken is null)
            {
                return false;
            }

            if (nextToken.TokenType == TSqlTokenType.WhiteSpace)
            {
                return true;
            }

            return IsPreviousNonWhiteSpaceTokenAnyKindOfOperationToken(tokens, currentTokenIndex);
        }

        public static bool Star(IList<TSqlParserToken> tokens, int currentTokenIndex)
        {
            var nextToken = tokens.GetNextToken(currentTokenIndex);
            if (nextToken is null)
            {
                return false;
            }

            if (nextToken.TokenType is TSqlTokenType.WhiteSpace or TSqlTokenType.LeftParenthesis) // COUNT(*)
            {
                return true;
            }

            return IsNextNonWhiteSpaceTokenAnyKindOfOperationToken(tokens, currentTokenIndex);
        }

        private static bool IsNextTokenAnyKindOfOperationToken(IList<TSqlParserToken> tokens, int currentTokenIndex)
        {
            var nextToken = tokens
                .GetNextToken(currentTokenIndex);

            return nextToken is not null && OperationTokens.Contains(nextToken.TokenType);
        }

        private static bool IsNextNonWhiteSpaceTokenAnyKindOfOperationToken(IList<TSqlParserToken> tokens, int currentTokenIndex)
        {
            var firstNextNonBlankSpaceToken = tokens
                .GetPreviousTokensReversed(currentTokenIndex)
                .SkipWhiteSpaceTokens()
                .FirstOrDefault();

            return firstNextNonBlankSpaceToken is not null && OperationTokens.Contains(firstNextNonBlankSpaceToken.TokenType);
        }
    }

    private static bool IsPreviousNonWhiteSpaceTokenAnyKindOfOperationToken(IList<TSqlParserToken> tokens, int currentTokenIndex)
    {
        var firstPreviousNonBlankSpaceToken = tokens
            .GetPreviousTokensReversed(currentTokenIndex)
            .SkipWhiteSpaceTokens()
            .FirstOrDefault();

        return firstPreviousNonBlankSpaceToken is not null && OperationTokens.Contains(firstPreviousNonBlankSpaceToken.TokenType);
    }
}
