using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlParserTokenExtensions
{
    public static CodeRegion GetCodeRegion(this TSqlParserToken token)
    {
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
        return new CodeRegion(token.Line, token.Column, endLineNumber, endColumnNumber);
    }
}
