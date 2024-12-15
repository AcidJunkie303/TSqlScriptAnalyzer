using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlParserTokenExtensions
{
    public static CodeRegion GetCodeRegion(this TSqlParserToken token)
    {
        var length = token.Text.Length;
        var (endLineNumberOffset, endColumnOffset) = token.Text.GetLineAndColumnIndex(length - 1);
        var endLineNumber = token.Line + endLineNumberOffset;
        var endColumnNumber = endLineNumberOffset == 0
            ? token.Column + endColumnOffset + 1 // 1 because it's an offset
            : endColumnOffset + 1 + 1; // 1 because it's an offset and 1 because ... uhm.. TODO:

        return new CodeRegion(token.Line, token.Column, endLineNumber, endColumnNumber);
    }
}
