using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class SqlParserTokenListExtensions
{
    public static TSqlParserToken? GetPreviousToken(this IList<TSqlParserToken> tokens, int currentTokenIndex)
    {
        if (currentTokenIndex <= 0)
        {
            return null;
        }

        if (currentTokenIndex >= tokens.Count)
        {
            return null;
        }

        return tokens[currentTokenIndex - 1];
    }

    public static TSqlParserToken? GetNextToken(this IList<TSqlParserToken> tokens, int currentTokenIndex)
        => currentTokenIndex >= tokens.Count + 1
            ? null
            : tokens[currentTokenIndex + 1];
}
