using System.Runtime.InteropServices;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

[StructLayout(LayoutKind.Auto)]
public record struct CodeRegion(
    int StartLineNumber,
    int StartColumnNumber,
    int EndLineNumber,
    int EndColumnNumber
)
{
    public static CodeRegion Create(int startLineNumber, int startColumnNumber, int endLineNumber, int endColumnNumber)
        => new(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);

    public static CodeRegion From(SqlCodeObject codeObject)
        => new(codeObject.StartLocation.LineNumber, codeObject.StartLocation.ColumnNumber, codeObject.EndLocation.LineNumber, codeObject.EndLocation.ColumnNumber);

    public static CodeRegion From(Token token)
        => new(token.StartLocation.LineNumber, token.StartLocation.ColumnNumber, token.EndLocation.LineNumber, token.EndLocation.ColumnNumber);

    public static CodeRegion From(Location start, Location end)
        => new(start.LineNumber, start.ColumnNumber, end.LineNumber, end.ColumnNumber);

    public override string ToString() => $"({StartLineNumber},{StartColumnNumber})-({EndLineNumber},{EndColumnNumber})";
}
