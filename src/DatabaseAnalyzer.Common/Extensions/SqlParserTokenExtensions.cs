using DatabaseAnalyzer.Common.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class SqlParserTokenExtensions
{
    public static CodeLocation GetCodeLocation(this TSqlParserToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        return CodeLocation.Create(token.Line, token.Column);
    }

    public static CodeRegion GetCodeRegion(this TSqlParserToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.Text is null)
        {
            return CodeRegion.Create(token.Line, token.Column, token.Line, token.Column);
        }

        var length = token.Text.Length;
        var (endLineNumberOffset, endColumnOffset) = token.Text.GetLineAndColumnIndex(length - 1);
        var endLineNumber = token.Line + endLineNumberOffset;
        var endColumnNumber = endLineNumberOffset == 0
            ? token.Column + endColumnOffset
            : endColumnOffset;

        endColumnNumber++; // because it's an offset
        return CodeRegion.Create(token.Line, token.Column, endLineNumber, endColumnNumber);
    }
}
