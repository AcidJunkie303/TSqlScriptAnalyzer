using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class TokenExtensions
{
    private static readonly FrozenSet<string> CommentTypes = new[]
    {
        "LEX_END_OF_LINE_COMMENT", "LEX_MULTILINE_COMMENT"
    }.ToFrozenSet(StringComparer.CurrentCulture);

    public static bool IsComment(this Token token) => CommentTypes.Contains(token.Type);
    public static bool IsSingleLineComment(this Token token) => token.Type.EqualsOrdinalIgnoreCase("aa");
    public static bool IsMultiLineComment(this Token token) => token.Type.EqualsOrdinalIgnoreCase("bb");
}
