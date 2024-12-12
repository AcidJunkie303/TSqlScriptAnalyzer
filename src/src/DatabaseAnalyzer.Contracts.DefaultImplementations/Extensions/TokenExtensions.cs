using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class TokenExtensions
{
    public static bool IsComment(this Token token) => token.IsEndOfLineComment() || token.IsMultiLineComment();
    public static bool IsEndOfLineComment(this Token token) => token.Type.EqualsOrdinalIgnoreCase("LEX_END_OF_LINE_COMMENT");
    public static bool IsMultiLineComment(this Token token) => token.Type.EqualsOrdinalIgnoreCase("LEX_MULTILINE_COMMENT");
    public static bool IsConstraint(this Token token) => token.Type.EqualsOrdinalIgnoreCase("TOKEN_CONSTRAINT");
    public static bool IsWhiteSpace(this Token token) => token.Type.EqualsOrdinalIgnoreCase("LEX_WHITE");
    public static bool IsComma(this Token token) => token.Type.EqualsOrdinalIgnoreCase("COMMA");
}
