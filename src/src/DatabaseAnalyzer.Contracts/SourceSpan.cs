using System.Runtime.InteropServices;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

[StructLayout(LayoutKind.Auto)]
public record struct SourceSpan(
    int StartLineNumber,
    int StartColumnNumber,
    int EndLineNumber,
    int EndColumnNumber
)
{
    public static SourceSpan Create(int startLineNumber, int startColumnNumber, int endLineNumber, int endColumnNumber)
        => new(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);

    public static SourceSpan From(SqlCodeObject codeObject)
        => new(codeObject.StartLocation.LineNumber, codeObject.StartLocation.ColumnNumber, codeObject.EndLocation.LineNumber, codeObject.EndLocation.ColumnNumber);

    public static SourceSpan From(Token token)
        => new(token.StartLocation.LineNumber, token.StartLocation.ColumnNumber, token.EndLocation.LineNumber, token.EndLocation.ColumnNumber);
}
